using System;
using System.Linq;
using System.Net;
using System.Threading;

namespace Rest.Server
{
    public delegate string RestHandler(string[] args);
    public delegate byte[] RawRestHandler(string[] args);
    public delegate string RestPostHandler(string[] args, string body);
    public delegate byte[] RawRestPostHandler(string[] args, string body);

    public class RestMethod
    {
        public RestMethod()
        {
            Verb = "GET";
            MillisecondsTimeout = -1;
        }

        private string name;

        /// <summary>
        /// Gets or sets the name of the method used in the request URI. Can contain slashes (/). If this is not specified, the name of the CLR method is used.
        /// </summary>
        public string Name
        {
            get
            {
                return name;
            }
            set
            {
                name = value;
                baseUri = null;
            }
        }

        public string Verb { get; set; }

        /// <summary>
        /// Gets or sets the minimum number of arguments that must be passed to this function. If MaxParamCount is not specified, then this is also the maximum number of arguments.
        /// </summary>
        public int ParamCount { get; set; }
        public RawRestPostHandler Handler { get; set; }

        private string _contentType;

        /// <summary>
        /// Gets or sets the Content-Type of the data returned by this function.
        /// </summary>
        public string ContentType
        {
            get
            {
                if (string.IsNullOrWhiteSpace(_contentType))
                {
                    _contentType = "text/plain";
                }
                return _contentType;
            }
            set
            {
                _contentType = value;
            }
        }

        private string baseUri;

        public string BaseUri
        {
            get
            {
                if (baseUri != null)
                {
                    return baseUri;
                }
                baseUri = Name;
                if (string.IsNullOrEmpty(baseUri))
                {
                    baseUri = "/";
                }
                else if (!baseUri.StartsWith("/", StringComparison.InvariantCulture))
                {
                    baseUri = "/" + baseUri;
                }
                return baseUri;
            }
        }

        /// <summary>
        /// Gets or sets the maximum time in milliseconds for the server to wait for the method to process.
        /// </summary>
        /// <remarks>
        /// If the timeout expires, the RequestError event will be raised with an OperationCancelledException as the Exception for the event.
        /// A value of -1 specifies an infinite wait time.
        /// </remarks>
        public int MillisecondsTimeout { get; set; }
    }
}
