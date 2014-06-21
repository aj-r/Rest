using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Reflection;
using System.Text;
using System.Threading;

namespace Rest.Server
{
    /// <summary>
    /// A simple multi-threaded REST server implementation
    /// </summary>
    public class RestServer
    {
        public RestServer(int port)
        {
            Methods = new List<RestMethod>();
            listener = new HttpListener();
        }

        /// <summary>
        /// Gets the list of methods that the server is handling
        /// </summary>
        public List<RestMethod> Methods { get; private set; }

        //private TcpListener listener;
        private HttpListener listener;
        private Thread listenThread;
        private ManualResetEvent stopEvent = new ManualResetEvent(false);

        /// <summary>
        /// Starts the server.
        /// </summary>
        public void Start()
        {
            if (!HttpListener.IsSupported)
            {
                throw new NotSupportedException("HTTP listeners are not supported on your computer. Windows XP SP2 or later is required.")
            }
            listenThread = new Thread(Listen);
            listenThread.Start();
        }

        /// <summary>
        /// Stops the server.
        /// </summary>
        public void Stop()
        {
            stopEvent.Set();
        }

        /// <summary>
        /// Registers all RestMethods found on the specified object as available REST methods on the server.
        /// </summary>
        /// <param name="obj">An object with methods marked with the RestMethodAttribute</param>
        public void RegisterMethods(object obj)
        {
            var type = obj.GetType();
            var methods = type.GetMethods(BindingFlags.Instance | BindingFlags.Public);
            foreach (var method in methods)
            {
                var attr = method.GetCustomAttribute<RestMethodAttribute>();
                if (attr == null)
                {
                    // Not a REST method.
                    continue;
                }
                var restMethod = new RestMethod
                {
                    Name = (attr.Name == null ? method.Name.ToLower() : attr.Name),
                    ParamCount = attr.ParamCount,
                    ContentType = attr.ContentType,
                    Handler = (RestHandler)method.CreateDelegate(typeof(RestHandler), obj)
                };
                Methods.Add(restMethod);
            }
        }

        private void Listen()
        {
            listener.Start();
            while (!stopEvent.WaitOne(0))
            {
                var context = listener.GetContext();
                var requestThread = new Thread(() => ProcessRequest(context));
                requestThread.Start();
            }
            stopEvent.Reset();
        }

        private void ProcessRequest(HttpListenerContext context)
        {
            var url = context.Request.Url;
            string response;
            foreach (var method in Methods)
            {
                RestMethodMatch match = method.TryProcess(url.AbsoluteUri, out response);
                switch (match)
                {
                    case RestMethodMatch.Success:
                        context.Response.StatusCode = 200;
                        context.Response.StatusDescription = "OK";
                        context.Response.ContentType = method.ContentType;
                        var responseBytes = Encoding.UTF8.GetBytes(response);
                        context.Response.ContentLength64 = responseBytes.Length;
                        context.Response.OutputStream.Write(responseBytes, 0, responseBytes.Length);
                        context.Response.OutputStream.Close();
                        return;
                    case RestMethodMatch.ParamMismatch:
                        context.Response.StatusCode = 500;
                        context.Response.StatusDescription = "Internal server error";
                        context.Response.OutputStream.Close();
                        return;
                }
            }
            // No matching method found
            context.Response.StatusCode = 404;
            context.Response.StatusDescription = "Not Found";
            context.Response.OutputStream.Close();
        }
    }
}
