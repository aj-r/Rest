using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Rest.Common
{
    public delegate void RequestErrorEventHandler(object sender, RequestErrorEventArgs e);

    public class RequestErrorEventArgs : EventArgs
    {
        public RequestErrorEventArgs()
        { }

        public RequestErrorEventArgs(Uri requestUri, Exception ex)
        {
            RequestUri = requestUri;
            Exception = ex;
        }

        public Uri RequestUri { get; private set; }
        public Exception Exception { get; private set; }
    }
}
