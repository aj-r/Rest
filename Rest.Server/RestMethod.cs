using System;
using System.Net;

namespace Rest.Server
{
    /// <summary>
    /// Indicates how a uri matches or does not match the method signature.
    /// </summary>
    public enum RestMethodMatch
    {
        /// <summary>
        /// The uri matches the method signature.
        /// </summary>
        Success,
        /// <summary>
        /// The uri does not the method signature because the name is incorrect.
        /// </summary>
        NameMismatch,
        /// <summary>
        /// The uri does not the method signature because the wrong number of parameters are present.
        /// </summary>
        ParamMismatch
    }

    public delegate string RestHandler(string[] args);
    public delegate byte[] RawRestHandler(string[] args);
    public delegate string RestPostHandler(string[] args, string body);
    public delegate byte[] RawRestPostHandler(string[] args, string body);

    public class RestMethod
    {
        public RestMethod()
        {
            Verb = "GET";
        }

        private string name;
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
        public int ParamCount { get; set; }
        public RawRestPostHandler Handler { get; set; }

        private string _contentType;
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

        public RestMethodMatch TryProcess(Uri uri, string body, out byte[] response)
        {
            var path = uri.PathAndQuery;
            if (!path.StartsWith(BaseUri))
            {
                response = null;
                return RestMethodMatch.NameMismatch;
            }
            var paramString = path.Substring(BaseUri.Length);
            var paramValues = paramString.Split(new[] { '/' }, StringSplitOptions.RemoveEmptyEntries);
            if (paramValues.Length != ParamCount)
            {
                response = null;
                return RestMethodMatch.ParamMismatch;
            }
            // TODO: enforce a time limit on the handler which can be overridden in the RestMethodAttribute
            response = Handler(paramValues, body);
            return RestMethodMatch.Success;
        }
    }
}
