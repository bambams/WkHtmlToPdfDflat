using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace WkHtmlToPdfDflat
{
    public class VoidEventArgs:
        EventArgs
    {
        public VoidEventArgs(
                Converter converter,
                object arg)
        {
            if(converter == null)
                throw new ArgumentNullException("converter");

            this.Converter = converter;
            this.Arg = arg;
        }

        public Converter Converter { get; private set; }

        public object Arg { get; private set; }
    }
}
