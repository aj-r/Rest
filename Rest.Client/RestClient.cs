﻿using System;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Net;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using System.Web.Script.Serialization;
using System.Xml.Serialization;
using JsonSerialization;
using Rest.Common;

namespace Rest.Client
{
    public class RestClient
    {
        private static JavaScriptSerializer jsonSerializer;
        private static NameJavaScriptConverter jsonConverter;

        public event RequestErrorEventHandler RequestError;

        static RestClient()
        {
            jsonSerializer = new JavaScriptSerializer();
            jsonConverter = new NameJavaScriptConverter();
            jsonSerializer.RegisterConverters(new JavaScriptConverter[] { jsonConverter });
        }

        /// <summary>
        /// Executes a REST request and returns the body in byte array format.
        /// </summary>
        /// <param name="uri">The full URI of the REST request</param>
        /// <returns>The body in byte array format</returns>
        public async Task<byte[]> Execute(string uri, WebHeaderCollection customHeaders = null)
        {
            try
            {
                var request = WebRequest.Create(uri);
                request.Method = "GET";
                if (customHeaders != null)
                {
                    request.Headers = customHeaders;
                }
                try
                {
                    using (var response = await request.GetResponseAsync())
                    using (var stream = response.GetResponseStream())
                    using (var memoryStream = new MemoryStream())
                    {
                        stream.CopyTo(memoryStream);
                        var buffer = memoryStream.ToArray();
                        return buffer;
                    }
                }
                catch (WebException ex)
                {
                    using (var stream = ex.Response.GetResponseStream())
                    using (var memoryStream = new MemoryStream())
                    {
                        stream.CopyTo(memoryStream);
                        var buffer = memoryStream.ToArray();
                        return buffer;
                    }
                }
            }
            catch (Exception ex)
            {
                Uri actualUri = null;
                try { actualUri = new Uri(uri); }
                catch { }
                OnRequestError(new RequestErrorEventArgs(actualUri, ex));
                return null;
            }
        }

        /// <summary>
        /// Executes a REST request and returns the body in string format.
        /// </summary>
        /// <param name="uri">The full URI of the REST request</param>
        /// <returns>The body in string format</returns>
        public async Task<string> ExecuteString(string uri, WebHeaderCollection customHeaders = null)
        {
            byte[] raw = await Execute(uri, customHeaders);
            if (raw == null)
                return null;
            try
            {
                var result = Encoding.UTF8.GetString(raw);
                return result;
            }
            catch (Exception ex)
            {
                Uri actualUri = null;
                try { actualUri = new Uri(uri); }
                catch { }
                OnRequestError(new RequestErrorEventArgs(actualUri, ex));
                return null;
            }
        }

        /// <summary>
        /// Executes a REST request and parses the body as JSON.
        /// </summary>
        /// <typeparam name="T">The type of object expected to be returned</typeparam>
        /// <param name="uri">The full URI of the REST request</param>
        /// <returns>The deserialized JSON object</returns>
        public async Task<T> ExecuteJson<T>(string uri, WebHeaderCollection customHeaders = null)
        {
            var json = await ExecuteString(uri, customHeaders);
            if (json == null)
                return default(T);
            try
            {
                return DeserializeJson<T>(json);
            }
            catch (Exception ex)
            {
                Uri actualUri = null;
                try { actualUri = new Uri(uri); }
                catch { }
                OnRequestError(new RequestErrorEventArgs(actualUri, ex));
                return default(T);
            }
        }

        /// <summary>
        /// Executes a REST request and parses the body as XML.
        /// </summary>
        /// <typeparam name="T">The type of object expected to be returned</typeparam>
        /// <param name="uri">The full URI of the REST request</param>
        /// <returns>The deserialized JSON object</returns>
        public async Task<T> ExecuteXml<T>(string uri, WebHeaderCollection customHeaders = null)
        {
            var json = await ExecuteString(uri, customHeaders);
            if (json == null)
                return default(T);
            try
            {
                return DeserializeXml<T>(json);
            }
            catch (Exception ex)
            {
                Uri actualUri = null;
                try { actualUri = new Uri(uri); }
                catch { }
                OnRequestError(new RequestErrorEventArgs(actualUri, ex));
                return default(T);
            }
        }

        /// <summary>
        /// Executes a REST request and decodes the body as base-64
        /// </summary>
        /// <param name="uri">The full URI of the REST request</param>
        /// <returns>The base-64 decoded bytes</returns>
        public async Task<byte[]> ExecuteBase64(string uri, WebHeaderCollection customHeaders = null)
        {
            var base64 = await ExecuteString(uri, customHeaders);
            if (base64 == null)
                return null;
            try
            {
                var bytes = Convert.FromBase64String(base64);
                return bytes;
            }
            catch (Exception ex)
            {
                Uri actualUri = null;
                try { actualUri = new Uri(uri); }
                catch { }
                OnRequestError(new RequestErrorEventArgs(actualUri, ex));
                return null;
            }
        }
        
        /// <summary>
        /// Converts a JSON string to a CLR object
        /// </summary>
        private T DeserializeJson<T>(string json)
        {
            jsonConverter.AddSupportedType(typeof(T));
            var obj = jsonSerializer.Deserialize<T>(json);
            return obj;
        }

        /// <summary>
        /// Converts an XML string to a CLR object
        /// </summary>
        private T DeserializeXml<T>(string xml)
        {
            var serializer = new XmlSerializer(typeof(T));
            T obj;
            using (var reader = new StringReader(xml))
            {
                obj = (T)serializer.Deserialize(reader);
            }
            return obj;
        }

        protected virtual void OnRequestError(RequestErrorEventArgs e)
        {
            if (RequestError != null)
                RequestError(this, e);
        }
    }
}
