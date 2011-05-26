using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;

namespace WkHtmlToPdfDflat
{
    public class Converter:
        IDisposable
    {
        public Converter(IntPtr globalSettings, object eventArg = null)
        {
            var converter = NativeMethods.wkhtmltopdf_create_converter(
                    globalSettings);

            if(converter == IntPtr.Zero)
            {
                throw new Exception(
                        "Failed to create an unmanaged converter.");
            }

            this.ConverterPtr = converter;
            this.EventArg = eventArg;

            this.RegisterCallbacks();
        }

        ~Converter()
        {
            this.Dispose(false);
        }

        public void AddObject(IntPtr settings, string html)
        {
            NativeMethods.wkhtmltopdf_add_object(
                    this.ConverterPtr,
                    settings,
                    new UnicodeEncoding().GetBytes(html));
        }

        public void Convert()
        {
            if(!NativeMethods.wkhtmltopdf_convert(this.ConverterPtr))
                throw new Exception("Failed to convert HTML to PDF.");
        }

        public IntPtr ConverterPtr { get; protected set; }

        public int CurrentPhase
        {
            get
            {
                return NativeMethods.wkhtmltopdf_current_phase(
                        this.ConverterPtr);
            }
        }

        public string CurrentPhaseDescription
        {
            get
            {
                return this.GetPhaseDescription(this.CurrentPhase);
            }
        }

        public object EventArg { get; set; }

        public byte[] GetOutput()
        {
            this.CheckDisposed();

            IntPtr data = IntPtr.Zero;

            var size = NativeMethods.wkhtmltopdf_get_output(
                    this.ConverterPtr,
                    out data);

            var buffer = new byte[size];

            Marshal.Copy(data, buffer, 0, (int)size);

            return buffer;
        }

        public int HttpErrorCode
        {
            get
            {
                return NativeMethods.wkhtmltopdf_http_error_code(
                        this.ConverterPtr);
            }
        }

        public int PhaseCount
        {
            get
            {
                return NativeMethods.wkhtmltopdf_phase_count(
                        this.ConverterPtr);
            }
        }

        public string GetPhaseDescription(int phase)
        {
            //return NativeMethods.wkhtmltopdf_phase_description(
                    //this.ConverterPtr,
                    //phase);
            return "(Phase)";
        }

        public string ProgressString
        {
            get
            {
                //return NativeMethods.wkhtmltopdf_progress_string(
                    //this.ConverterPtr);
                return "(Progress)";
            }
        }

        protected void RegisterCallbacks()
        {
            this.errorCallback_ =
                    new NativeMethods.wkhtmltopdf_str_callback(
                    this.ErrorCallback);

            this.finishedCallback_ =
                    new NativeMethods.wkhtmltopdf_int_callback(
                    this.FinishedCallback);

            this.phaseChangedCallback_ =
                    new NativeMethods.wkhtmltopdf_void_callback(
                    this.PhaseChangedCallback);

            this.progressChangedCallback_ =
                    new NativeMethods.wkhtmltopdf_int_callback(
                    this.ProgressChangedCallback);

            this.warningCallback_ =
                    new NativeMethods.wkhtmltopdf_str_callback(
                    this.WarningCallback);

            NativeMethods.wkhtmltopdf_set_error_callback(
                    this.ConverterPtr,
                    this.errorCallback_);

            NativeMethods.wkhtmltopdf_set_finished_callback(
                    this.ConverterPtr,
                    this.finishedCallback_);

            NativeMethods.wkhtmltopdf_set_phase_changed_callback(
                    this.ConverterPtr,
                    this.phaseChangedCallback_);

            NativeMethods.wkhtmltopdf_set_progress_changed_callback(
                    this.ConverterPtr,
                    this.progressChangedCallback_);

            NativeMethods.wkhtmltopdf_set_warning_callback(
                    this.ConverterPtr,
                    this.warningCallback_);
        }

        #region IDisposable Interface.

        bool disposed_;

        protected void CheckDisposed()
        {
            if(this.disposed_)
            {
                throw new Exception(
                        "The converter has already been disposed.");
            }
        }

        public void Dispose()
        {
            this.Dispose(true);
            GC.SuppressFinalize(this);
        }

        protected virtual void Dispose(bool disposing)
        {
            if(!this.disposed_)
            {
                if(disposing)
                {
                    // Nothing managed to dispose.
                }

                if(this.ConverterPtr != IntPtr.Zero)
                {
                    NativeMethods.wkhtmltopdf_destroy_converter(
                            this.ConverterPtr);
                    this.ConverterPtr = IntPtr.Zero;
                }

                this.disposed_ = true;
            }
        }

        #endregion

        #region Events.

        public event EventHandler<StringEventArgs> Error;

        public event EventHandler<IntegerEventArgs> Finished;

        public event EventHandler<VoidEventArgs> PhaseChanged;

        public event EventHandler<IntegerEventArgs> ProgressChanged;

        public event EventHandler<StringEventArgs> Warning;

        #endregion

        #region Event Triggers.

        protected void OnError(StringEventArgs e)
        {
            if(this.Error != null)
                this.Error(this, e);
        }

        protected void OnFinished(IntegerEventArgs e)
        {
            if(this.Finished != null)
                this.Finished(this, e);
        }

        protected void OnPhaseChanged(VoidEventArgs e)
        {
            if(this.PhaseChanged != null)
                this.PhaseChanged(this, e);
        }

        protected void OnProgressChanged(IntegerEventArgs e)
        {
            if(this.ProgressChanged != null)
                this.ProgressChanged(this, e);
        }

        protected void OnWarning(StringEventArgs e)
        {
            if(this.Warning != null)
                this.Warning(this, e);
        }

        #endregion

        #region Native Event Callbacks.

        protected void ErrorCallback(IntPtr converter, byte[] utf8str)
        {
            this.OnError(new StringEventArgs(
                    this,
                    new UTF8Encoding().GetString(utf8str),
                    this.EventArg));
        }

        protected void FinishedCallback(IntPtr converter, int val)
        {
            this.OnFinished(new IntegerEventArgs(
                    this,
                    val,
                    this.EventArg));
        }

        protected void PhaseChangedCallback(IntPtr converter)
        {
            this.OnPhaseChanged(new VoidEventArgs(
                    this,
                    this.EventArg));
        }

        protected void ProgressChangedCallback(IntPtr converter, int val)
        {
            this.OnProgressChanged(new IntegerEventArgs(
                    this,
                    val,
                    this.EventArg));
        }

        protected void WarningCallback(IntPtr converter, byte[] utf8str)
        {
            this.OnWarning(new StringEventArgs(
                    this,
                    new UTF8Encoding().GetString(utf8str),
                    this.EventArg));
        }

        #endregion

        #region Managed Callback Instances.
        /*
         * The callbacks need to be stored in managed code or the garbage
         * collector won't realize they are still in use and might clean
         * them up. :-X Learned this lesson the hard way with zbar library
         * and forgot all about it. ;D
         */
        NativeMethods.wkhtmltopdf_str_callback errorCallback_;
        NativeMethods.wkhtmltopdf_int_callback finishedCallback_;
        NativeMethods.wkhtmltopdf_void_callback phaseChangedCallback_;
        NativeMethods.wkhtmltopdf_int_callback progressChangedCallback_;
        NativeMethods.wkhtmltopdf_str_callback warningCallback_;

        #endregion

        #region Native Methods.

        class NativeMethods
        {
            const string LibraryName = "wkhtmltox0";

            [DllImport(LibraryName)]
            public static extern void wkhtmltopdf_add_object(
                    IntPtr converter,
                    IntPtr settings,
                    [MarshalAs(UnmanagedType.LPArray)]
                    byte[] data);

            [DllImport(LibraryName)]
            [return: MarshalAs(UnmanagedType.Bool)]
            public static extern bool wkhtmltopdf_convert(
                    IntPtr converter);

            [DllImport(LibraryName)]
            public static extern IntPtr wkhtmltopdf_create_converter(
                    IntPtr settings);

            [DllImport(LibraryName)]
            [return: MarshalAs(UnmanagedType.I4)]
            public static extern int wkhtmltopdf_current_phase(
                    IntPtr converter);

            [DllImport(LibraryName)]
            public static extern void wkhtmltopdf_destroy_converter(
                    IntPtr converter);

            [DllImport(LibraryName)]
            [return: MarshalAs(UnmanagedType.I4)]
            public static extern int wkhtmltopdf_get_output(
                    IntPtr converter,
                    out IntPtr data);

            [DllImport(LibraryName)]
            [return: MarshalAs(UnmanagedType.I4)]
            public static extern int wkhtmltopdf_http_error_code(
                    IntPtr converter);

            [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
            public delegate void wkhtmltopdf_int_callback(
                    IntPtr converter,
                    [MarshalAs(UnmanagedType.I4)]
                    int val);

            [DllImport(LibraryName)]
            [return: MarshalAs(UnmanagedType.I4)]
            public static extern int wkhtmltopdf_phase_count(
                    IntPtr converter);

            [DllImport(LibraryName)]
            [return: MarshalAs(UnmanagedType.LPStr)]
            public static extern string wkhtmltopdf_phase_description(
                    IntPtr converter,
                    [MarshalAs(UnmanagedType.I4)]
                    int phase);

            [DllImport(LibraryName)]
            [return: MarshalAs(UnmanagedType.LPStr)]
            public static extern string wkhtmltopdf_progress_string(
                    IntPtr converter);

            [DllImport(LibraryName)]
            public static extern void wkhtmltopdf_set_error_callback(
                    IntPtr converter,
                    wkhtmltopdf_str_callback cb);

            [DllImport(LibraryName)]
            public static extern void wkhtmltopdf_set_finished_callback(
                    IntPtr converter,
                    wkhtmltopdf_int_callback cb);

            [DllImport(LibraryName)]
            public static extern void wkhtmltopdf_set_phase_changed_callback(
                    IntPtr converter,
                    wkhtmltopdf_void_callback cb);

            [DllImport(LibraryName)]
            public static extern void wkhtmltopdf_set_progress_changed_callback(
                    IntPtr converter,
                    wkhtmltopdf_int_callback cb);

            [DllImport(LibraryName)]
            public static extern void wkhtmltopdf_set_warning_callback(
                    IntPtr converter,
                    wkhtmltopdf_str_callback cb);

            [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
            public delegate void wkhtmltopdf_str_callback(
                    IntPtr converter,
                    [MarshalAs(UnmanagedType.LPArray)]
                    byte[] utf8str);

            [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
            public delegate void wkhtmltopdf_void_callback(
                    IntPtr converter);
        }

        #endregion
    }
}
