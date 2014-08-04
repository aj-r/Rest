using System;
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
        public async Task<WebResult<byte[]>> Execute(string uri, WebHeaderCollection customHeaders = null)
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
                    using (var response = (HttpWebResponse)(await request.GetResponseAsync()))
                    using (var stream = response.GetResponseStream())
                    using (var memoryStream = new MemoryStream())
                    {
                        stream.CopyTo(memoryStream);
                        var buffer = memoryStream.ToArray();
                        return new WebResult<byte[]>((int)response.StatusCode, response.ContentType, buffer);
                    }
                }
                catch (WebException ex)
                {
                    var response = (HttpWebResponse)ex.Response;
                    using (var stream = response.GetResponseStream())
                    using (var memoryStream = new MemoryStream())
                    {
                        if (ex.Response == null)
                            return new WebResult<byte[]>(0, null, new byte[0]);
                        stream.CopyTo(memoryStream);
                        var buffer = memoryStream.ToArray();
                        return new WebResult<byte[]>((int)response.StatusCode, response.ContentType, buffer);
                    }
                }
            }
            catch (Exception ex)
            {
                Uri actualUri = null;
                try { actualUri = new Uri(uri); }
                catch { }
                OnRequestError(new RequestErrorEventArgs(actualUri, ex));
                return new WebResult<byte[]>(0, null, new byte[0]);
            }
        }

        /// <summary>
        /// Executes a REST request and returns the body in string format.
        /// </summary>
        /// <param name="uri">The full URI of the REST request</param>
        /// <returns>The body in string format</returns>
        public async Task<WebResult<string>> ExecuteString(string uri, WebHeaderCollection customHeaders = null)
        {
            var result = await Execute(uri, customHeaders);
            try
            {
                byte[] raw = result.Value;
                var value = Encoding.UTF8.GetString(raw);
                return new WebResult<string>(result.StatusCode, result.ContentType, value);
            }
            catch (Exception ex)
            {
                Uri actualUri = null;
                try { actualUri = new Uri(uri); }
                catch { }
                OnRequestError(new RequestErrorEventArgs(actualUri, ex));
                return new WebResult<string>(0, null, string.Empty);
            }
        }

        /// <summary>
        /// Executes a REST request and parses the body as JSON.
        /// </summary>
        /// <typeparam name="T">The type of object expected to be returned</typeparam>
        /// <param name="uri">The full URI of the REST request</param>
        /// <returns>The deserialized JSON object</returns>
        public async Task<WebResult<T>> ExecuteJson<T>(string uri, WebHeaderCollection customHeaders = null)
        {
            var result = await ExecuteString(uri, customHeaders);
            if (result.StatusCode != 200)
                return new WebResult<T>(result.StatusCode, result.ContentType, default(T));

            try
            {
                var json = result.Value;
                var value = DeserializeJson<T>(json);
                return new WebResult<T>(result.StatusCode, result.ContentType, value);
            }
            catch (Exception ex)
            {
                Uri actualUri = null;
                try { actualUri = new Uri(uri); }
                catch { }
                OnRequestError(new RequestErrorEventArgs(actualUri, ex));
                return new WebResult<T>();
            }
        }

        /// <summary>
        /// Executes a REST request and parses the body as XML.
        /// </summary>
        /// <typeparam name="T">The type of object expected to be returned</typeparam>
        /// <param name="uri">The full URI of the REST request</param>
        /// <returns>The deserialized JSON object</returns>
        public async Task<WebResult<T>> ExecuteXml<T>(string uri, WebHeaderCollection customHeaders = null)
        {
            var result = await ExecuteString(uri, customHeaders);
            if (result.StatusCode != 200)
                return new WebResult<T>(result.StatusCode, result.ContentType, default(T));

            try
            {
                var json = result.Value;
                var value = DeserializeXml<T>(json);
                return new WebResult<T>(result.StatusCode, result.ContentType, value);
            }
            catch (Exception ex)
            {
                Uri actualUri = null;
                try { actualUri = new Uri(uri); }
                catch { }
                OnRequestError(new RequestErrorEventArgs(actualUri, ex));
                return new WebResult<T>();
            }
        }

        /// <summary>
        /// Executes a REST request and decodes the body as base-64
        /// </summary>
        /// <param name="uri">The full URI of the REST request</param>
        /// <returns>The base-64 decoded bytes</returns>
        public async Task<WebResult<byte[]>> ExecuteBase64(string uri, WebHeaderCollection customHeaders = null)
        {
            var result = await ExecuteString(uri, customHeaders);
            if (result.StatusCode != 200)
                return new WebResult<byte[]>(result.StatusCode, result.ContentType, new byte[0]);

            try
            {
                var base64 = result.Value;
                var bytes = Convert.FromBase64String(base64);
                return new WebResult<byte[]>(result.StatusCode, result.ContentType, bytes);
            }
            catch (Exception ex)
            {
                Uri actualUri = null;
                try { actualUri = new Uri(uri); }
                catch { }
                OnRequestError(new RequestErrorEventArgs(actualUri, ex));
                return new WebResult<byte[]>(0, null, new byte[0]);
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
