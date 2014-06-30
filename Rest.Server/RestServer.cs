using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Reflection;
using System.Text;
using System.Threading;
using System.Web;

namespace Rest.Server
{
    /// <summary>
    /// A simple multi-threaded HTTP REST server implementation
    /// </summary>
    public class RestServer
    {
        public RestServer(int port)
        {
            Methods = new List<RestMethod>();
            listener = new HttpListener();
            listener.Prefixes.Add(string.Format("http://*:{0}/", port));
        }

        /// <summary>
        /// Gets the list of methods that the server is handling
        /// </summary>
        public List<RestMethod> Methods { get; private set; }

        public event Action<Exception> StartFailed;
        public event Action Started;

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
                throw new NotSupportedException("HTTP listeners are not supported on your computer. Windows XP SP2 or later is required.");
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
            listener.Close();
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
                };
                if (method.ReturnType == typeof(byte[]))
                {
                    restMethod.Handler = (RawRestHandler)method.CreateDelegate(typeof(RawRestHandler), obj);
                }
                else if (method.ReturnType == typeof(string))
                {
                    var handler = (RestHandler)method.CreateDelegate(typeof(RestHandler), obj);
                    restMethod.Handler = (args) => Encoding.UTF8.GetBytes(handler(args));
                }
                else
                {
                    throw new Exception("Invalid return type for RestHandler method '{0}': invalid return type. Must be String or Byte[].");
                }
                Methods.Add(restMethod);
            }
        }

        private void Listen()
        {
            try
            {
                listener.Start();
            }
            catch (Exception ex)
            {
                if (StartFailed != null)
                {
                    StartFailed(ex);
                }
            }
            if (Started != null)
            {
                Thread thread = new Thread(new ThreadStart(Started));
                thread.Start();
            }
            while (!stopEvent.WaitOne(0))
            {
                HttpListenerContext context;
                try
                {
                    context = listener.GetContext();
                }
                catch (HttpListenerException)
                {
                    // Failed to get context; potentially the connection was closed.
                    break;
                }
                var requestThread = new Thread(() => ProcessRequest(context));
                requestThread.Start();
            }
            stopEvent.Reset();
        }

        private void ProcessRequest(HttpListenerContext context)
        {
            try
            {
                var url = context.Request.Url;
                byte[] response;
                foreach (var method in Methods)
                {
                    RestMethodMatch match = method.TryProcess(url, out response);
                    if (match != RestMethodMatch.Success)
                    {
                        continue;
                    }
                    context.Response.ContentType = method.ContentType;
                    context.Response.ContentLength64 = response.Length;
                    context.Response.OutputStream.Write(response, 0, response.Length);
                    SendResponse(context.Response, 200);
                    return;
                }
                // No matching method found
                SendResponse(context.Response, 404);
            }
            catch
            {
                // TODO: log exception
                try
                {
                    SendResponse(context.Response, 500);
                }
                catch { }
            }
        }

        private void SendResponse(HttpListenerResponse response, int status)
        {
            response.StatusCode = status;
            response.StatusDescription = HttpWorkerRequest.GetStatusDescription(response.StatusCode);
            response.OutputStream.Close();
        }
    }
}
