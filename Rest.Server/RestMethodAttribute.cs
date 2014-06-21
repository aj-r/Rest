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
        public string Name { get; set; }
        public int ParamCount { get; set; }
        public string ContentType { get; set; }
    }
}
