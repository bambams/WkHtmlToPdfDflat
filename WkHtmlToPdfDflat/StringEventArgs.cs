using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace WkHtmlToPdfDflat
{
    public class StringEventArgs:
        EventArgs
    {
        public StringEventArgs(
                Converter converter,
                string str,
                object arg)
        {
            if(converter == null)
                throw new ArgumentNullException("converter");

            if(string.IsNullOrWhiteSpace(str))
                throw new ArgumentNullException("str");

            this.Converter = converter;
            this.String = str;
            this.Arg = arg;
        }

        public Converter Converter { get; private set; }

        public object Arg { get; private set; }

        public string String { get; private set; }
    }
}
