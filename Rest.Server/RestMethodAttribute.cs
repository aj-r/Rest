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
        /// Gets or sets the Content-Type of the data returned by this function.
        /// </summary>
        public string ContentType { get; set; }
    }
}
