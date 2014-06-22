using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

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

    public class RestMethod
    {
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
                baseUrl = null;
            }
        }
        public int ParamCount { get; set; }
        public RawRestHandler Handler { get; set; }

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

        private string baseUrl;

        public string BaseUrl
        {
            get
            {
                if (baseUrl != null)
                {
                    return baseUrl;
                }
                baseUrl = Name;
                if (string.IsNullOrEmpty(baseUrl))
                {
                    baseUrl = "/";
                }
                else if (!baseUrl.StartsWith("/", StringComparison.InvariantCulture))
                {
                    baseUrl = "/" + baseUrl;
                }
                return baseUrl;
            }
        }

        public RestMethodMatch TryProcess(string url, out byte[] response)
        {
            if (!url.StartsWith(BaseUrl))
            {
                response = null;
                return RestMethodMatch.NameMismatch;
            }
            var paramString = url.Substring(BaseUrl.Length);
            var paramValues = paramString.Split(new[] { '/' }, StringSplitOptions.RemoveEmptyEntries);
            if (paramValues.Length != ParamCount)
            {
                response = null;
                return RestMethodMatch.ParamMismatch;
            }
            response = Handler(paramValues);
            return RestMethodMatch.Success;
        }
    }
}
