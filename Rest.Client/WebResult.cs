using System;
using System.Net;

namespace Rest.Client
{
    public struct WebResult<T>
    {
        public static WebResult<T> Empty
        {
            get { return new WebResult<T>(-1, null, default(T)); }
        }

        private readonly int statusCode;
        private readonly string contentType;
        private readonly T value;

        public WebResult(int statusCode, string contentType, T value)
        {
            this.statusCode = statusCode;
            this.contentType = contentType;
            this.value = value;
        }

        /// <summary>
        /// Gets or sets the status code returned in the web response.
        /// </summary>
        public int StatusCode { get { return statusCode; } }

        /// <summary>
        /// Gets or sets the content type of the web response.
        /// </summary>
        public string ContentType { get { return contentType; } }

        /// <summary>
        /// Gets or sets the value of the data in the web response.
        /// </summary>
        public T Value { get { return value; } }
    }
}
