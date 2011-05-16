using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Runtime.InteropServices;

namespace WkHtmlToPdfDflat
{
    /// <summary>
    /// Managed wrapper over unmanaged HTML to PDF conversion library.
    /// </summary>
    public class Worker:
        IDisposable
    {
        const int PaperSizeBufSize = 101;
        const string BlockLocalFileAccessKey = "load.blockLocalFileAccess";
        const string OutputFormatKey = "outputFormat";
        const string PaperSizeKey = "size.paperSize";
        const string UseCompressionKey = "useCompression";
        const string UseExternalLinksKey = "useExternalLinks";

        static bool initialized_ = false;
        static Deinitializer deinitializer_ = new Deinitializer();

        public Worker()
        {
            if(!initialized_)
            {
                if(!NativeMethods.wkhtmltopdf_init(false))
                {
                    throw new Exception(
                            "Failed to initialize libwkhtmltox.");
                }

                initialized_ = true;
            }

            this.GlobalSettings =
                    NativeMethods.wkhtmltopdf_create_global_settings();

            if(this.GlobalSettings == IntPtr.Zero)
            {
                throw new Exception(
                        "Failed to allocate unmanaged global settings.");
            }

            if(!NativeMethods.wkhtmltopdf_set_global_setting(
                    this.GlobalSettings,
                    OutputFormatKey,
                    "pdf"))
            {
                throw new Exception(
                        "Failed to set output format to PDF.");
            }
        }

        ~Worker()
        {
            this.Dispose(false);
        }

        public void Convert(string html, object eventArg = null)
        {
            this.Converter = new Converter(
                    this.GlobalSettings,
                    eventArg);

            this.Converter.Error += this.Converter_Error;
            this.Converter.Finished += this.Converter_Finished;
            this.Converter.PhaseChanged += this.Converter_PhaseChanged;
            this.Converter.ProgressChanged +=
                    this.Converter_ProgressChanged;
            this.Converter.Warning += this.Converter_Warning;

            var settings = this.CreateObjectSettings();

            this.Converter.AddObject(settings, html);

            this.Converter.Convert();
        }

        protected Converter Converter { get; set; }

        protected IntPtr CreateObjectSettings()
        {
            var settings =
                    NativeMethods.wkhtmltopdf_create_object_settings();
#if !H2P_ALLOW_EXTERNAL_LINKS
            if(!NativeMethods.wkhtmltopdf_set_object_setting(
                    settings,
                    UseExternalLinksKey,
                    bool.FalseString.ToLower()))
            {
                throw new Exception(
                        "Failed to enforce no external links.");
            }
#endif

#if !H2P_ALLOW_LOCAL_FILE_ACCESS
            if(!NativeMethods.wkhtmltopdf_set_object_setting(
                    settings,
                    BlockLocalFileAccessKey,
                    bool.TrueString.ToLower()))
            {
                throw new Exception(
                        "Failed to enforce blocked local file access.");
            }
#endif

            return settings;
        }

        public bool ExtendedQt
        {
            get
            {
                return NativeMethods.wkhtmltopdf_extended_qt();
            }
        }

        protected IntPtr GlobalSettings { get; set; }

        public static string LibraryVersion
        {
            get
            {
                return NativeMethods.wkhtmltopdf_version();
            }
        }

        #region IDisposable Interface.

        bool disposed_;

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
                    if(this.Converter != null)
                        this.Converter.Dispose();
                }

                /*
                 * No unmanaged state to clean up. Need to leave
                 * wkhtmltopdf initialized for other threads to use it.
                 */

                this.Converter = null;
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

        #region Converter Event Handlers.

        protected void Converter_Error(object sender, StringEventArgs e)
        {
            if(this.Error != null)
                this.Error(sender, e);
        }

        protected void Converter_Finished(
                object sender,
                IntegerEventArgs e)
        {
            if(this.Finished != null)
                this.Finished(sender, e);
        }

        protected void Converter_PhaseChanged(
                object sender,
                VoidEventArgs e)
        {
            if(this.PhaseChanged != null)
                this.PhaseChanged(sender, e);
        }

        protected void Converter_ProgressChanged(
                object sender,
                IntegerEventArgs e)
        {
            if(this.ProgressChanged != null)
                this.ProgressChanged(sender, e);
        }

        protected void Converter_Warning(
                object sender,
                StringEventArgs e)
        {
            if(this.Warning != null)
                this.Warning(sender, e);
        }

        #endregion

        #region Global Settings Accesors.

        public string PaperSize
        {
            get
            {
                var buffer = new byte[PaperSizeBufSize];

                if(!NativeMethods.wkhtmltopdf_get_global_setting(
                        this.GlobalSettings,
                        PaperSizeKey,
                        buffer,
                        buffer.Length))
                {
                    throw new Exception(
                            "Failed to get paper size setting. It may " +
                            "not exist or an error may have occurred.");
                }

                return new UTF8Encoding().GetString(buffer);
            }

            set
            {
                if(!NativeMethods.wkhtmltopdf_set_global_setting(
                        this.GlobalSettings,
                        PaperSizeKey,
                        value))
                {
                    throw new Exception(
                            "Failed to update paper size settings.");
                }
            }
        }

        public bool UseCompression
        {
            get
            {
                var buffer = new byte[6];

                if(!NativeMethods.wkhtmltopdf_get_global_setting(
                        this.GlobalSettings,
                        UseCompressionKey,
                        buffer,
                        buffer.Length))
                {
                    throw new Exception(
                            "Failed to get compression setting. If " +
                            "may not exist or an error may have " +
                            "occurred.");
                }

                return new UTF8Encoding().GetString(buffer) == "true" ?
                        true :
                        false;
            }

            set
            {
                if(!NativeMethods.wkhtmltopdf_set_global_setting(
                        this.GlobalSettings,
                        UseCompressionKey,
                        value.ToString().ToLower()))
                {
                    throw new Exception(
                            "Failed to update compression settings.");
                }
            }
        }

        #endregion

        #region Native Methods.

        static class NativeMethods
        {
            const string LibraryName = "wkhtmltox0";

            [DllImport(LibraryName)]
            public static extern IntPtr
                    wkhtmltopdf_create_global_settings();

            [DllImport(LibraryName)]
            public static extern IntPtr
                    wkhtmltopdf_create_object_settings();

            [DllImport(LibraryName)]
            [return: MarshalAs(UnmanagedType.Bool)]
            public static extern bool wkhtmltopdf_deinit();

            [DllImport(LibraryName)]
            [return: MarshalAs(UnmanagedType.Bool)]
            public static extern bool wkhtmltopdf_extended_qt();

            [DllImport(LibraryName)]
            [return: MarshalAs(UnmanagedType.Bool)]
            public static extern bool wkhtmltopdf_get_global_setting(
                    IntPtr settings,
                    [MarshalAs(UnmanagedType.LPStr)]
                    string name,
                    [MarshalAs(UnmanagedType.LPArray)]
                    byte[] value,
                    [MarshalAs(UnmanagedType.SysInt)]
                    int vs);

            [DllImport(LibraryName)]
            [return: MarshalAs(UnmanagedType.Bool)]
            public static extern bool wkhtmltopdf_get_object_setting(
                    IntPtr settings,
                    [MarshalAs(UnmanagedType.LPStr)]
                    string name,
                    [MarshalAs(UnmanagedType.LPArray)]
                    byte[] value,
                    [MarshalAs(UnmanagedType.SysInt)]
                    int vs);

            [DllImport(LibraryName)]
            [return: MarshalAs(UnmanagedType.Bool)]
            public static extern bool wkhtmltopdf_init(
                    [MarshalAs(UnmanagedType.Bool)]
                    bool use_graphics);

            [DllImport(LibraryName)]
            [return: MarshalAs(UnmanagedType.Bool)]
            public static extern bool wkhtmltopdf_set_global_setting(
                    IntPtr settings,
                    [MarshalAs(UnmanagedType.LPStr)]
                    string name,
                    [MarshalAs(UnmanagedType.LPStr)]
                    string value);

            [DllImport(LibraryName)]
            [return: MarshalAs(UnmanagedType.Bool)]
            public static extern bool wkhtmltopdf_set_object_setting(
                    IntPtr settings,
                    [MarshalAs(UnmanagedType.LPStr)]
                    string name,
                    [MarshalAs(UnmanagedType.LPStr)]
                    string value);

            [DllImport(LibraryName)]
            [return: MarshalAs(UnmanagedType.LPStr)]
            public static extern string wkhtmltopdf_version();
        }

        #endregion

        class Deinitializer
        {
            ~Deinitializer()
            {
                NativeMethods.wkhtmltopdf_deinit();
            }
        }
    }
}
