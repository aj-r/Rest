﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Reflection;
using System.Text;
using System.Threading;
using System.Web;
using Rest.Common;

namespace Rest.Server
{
    /// <summary>
    /// A simple multi-threaded HTTP REST server implementation
    /// </summary>
    public class RestServer
    {
        public RestServer(int port)
        {
            if (port <= 0 || port > 65535)
                throw new ArgumentException("Port must be between 1 and 65535.", "port");
            Methods = new List<RestMethod>();
            Port = port;
            RegisterMethods(this);
        }

        /// <summary>
        /// Gets the list of methods that the server is handling
        /// </summary>
        public List<RestMethod> Methods { get; private set; }

        public event Action<Exception> StartFailed;
        public event Action Started;
        public event Action Stopped;

        /// <summary>
        /// Occurs when there was an internal error processing a request.
        /// </summary>
        public event RequestErrorEventHandler RequestError;

        private HttpListener listener;
        private Thread listenThread;
        private ManualResetEvent stopEvent = new ManualResetEvent(false);

        private int port;

        public int Port
        {
            get { return port; }
            protected set
            {
                if (IsRunning)
                    throw new InvalidOperationException("Cannot change port while server is running.");
                port = value;
            }
        }
        public bool IsRunning { get; private set; }

        /// <summary>
        /// Starts the server.
        /// </summary>
        public void Start()
        {
            if (IsRunning)
                return;
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
            if (!IsRunning)
                return;
            stopEvent.Set();
            listener.Close();
        }

        protected virtual void OnStarted()
        {
            if (Started != null)
                Started();
        }

        protected virtual void OnStartFailed(Exception ex)
        {
            IsRunning = false;
            if (StartFailed != null)
                StartFailed(ex);
        }

        protected virtual void OnStopped()
        {
            IsRunning = false;
            if (Stopped != null)
                Stopped();
        }

        protected virtual void OnRequestError(RequestErrorEventArgs e)
        {
            if (RequestError != null)
                RequestError(this, e);
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
                var parameters = method.GetParameters();
                if (parameters.Length != 1 || parameters[0].ParameterType != typeof(string[]))
                {
                    throw new Exception("Invalid signature for RestHandler method '{0}': invalid argments. Must have a single argument of String Array (String[]) type.");
                }
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
                    throw new Exception("Invalid signature for RestHandler method '{0}': invalid return type. Must be String or Byte Array (Byte[]).");
                }
                Methods.Add(restMethod);
            }
        }

        private void Listen()
        {
            stopEvent.Reset();
            IsRunning = true;
            listener = new HttpListener();
            listener.Prefixes.Add(string.Format("http://*:{0}/", port));
            try
            {
                listener.Start();
            }
            catch (Exception ex)
            {
                OnStartFailed(ex);
                return;
            }
            if (Started != null)
            {
                Thread thread = new Thread(new ThreadStart(OnStarted));
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
                catch (ObjectDisposedException)
                {
                    // Failed to get context; potentially the connection was closed.
                    break;
                }
                var requestThread = new Thread(() => ProcessRequest(context));
                requestThread.Start();
            }
            OnStopped();
        }

        private void ProcessRequest(HttpListenerContext context)
        {
            Uri uri = null;
            try
            {
                uri = context.Request.Url;
                byte[] response;
                foreach (var method in Methods)
                {
                    RestMethodMatch match = method.TryProcess(uri, out response);
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
            catch (Exception ex)
            {
                try
                {
                    OnRequestError(new RequestErrorEventArgs(uri, ex));
                }
                catch { }
                try
                {
                    SendResponse(context.Response, 500);
                }
                catch { }
            }
        }

        protected virtual void SendResponse(HttpListenerResponse response, int status)
        {
            response.StatusCode = status;
            response.StatusDescription = HttpWorkerRequest.GetStatusDescription(response.StatusCode);
            response.OutputStream.Close();
        }
    }
}
