using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Rest.Server
{
    [AttributeUsage(AttributeTargets.Method)]
    public class RestMethodAttribute : Attribute
    {
        public RestMethodAttribute()
        {
            MillisecondsTimeout = -1;
        }

        /// <summary>
        /// Gets or sets the name of the method used in the request URI. Can contain slashes (/). If this is not specified, the name of the CLR method is used.
        /// </summary>
        public string Name { get; set; }
        /// <summary>
        /// Gets or sets the minimum number of arguments that must be passed to this function. If MaxParamCount is not specified, then this is also the maximum number of arguments.
        /// </summary>
        public int ParamCount { get; set; }
        /// <summary>
        /// Gets or sets the maximum number of arguments that must be passed to this function.
        /// </summary>
        public int MaxParamCount { get; set; }
        /// <summary>
        /// Gets or sets the Content-Type of the data returned by this method.
        /// </summary>
        public string ContentType { get; set; }
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
