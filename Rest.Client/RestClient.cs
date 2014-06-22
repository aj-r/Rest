using System;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Net;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using System.Web.Script.Serialization;
using BlowfishCrypto;
using JsonSerialization;

namespace Rest.Client
{
    public class RestClient
    {
        private static JavaScriptSerializer jsonSerializer;
        private static NameJavaScriptConverter jsonConverter;

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
        public async Task<byte[]> Execute(string uri)
        {
            var request = WebRequest.Create(uri);
            request.Method = "GET";
            using (var response = await request.GetResponseAsync())
            using (var stream = response.GetResponseStream())
            {
                var buffer = new byte[response.ContentLength];
                // Read all bytes from the body
                var offset = 0;
                var bytesRead = 0;
                var totalBytesRead = 0;
                do
                {
                    bytesRead = stream.Read(buffer, offset, buffer.Length - offset);
                    offset += bytesRead;
                    totalBytesRead += bytesRead;
                } while (totalBytesRead < buffer.Length && bytesRead > 0);

                return buffer;
            }
        }

        /// <summary>
        /// Executes a REST request and returns the body in string format.
        /// </summary>
        /// <param name="uri">The full URI of the REST request</param>
        /// <returns>The body in string format</returns>
        public async Task<string> ExecuteString(string uri)
        {
            byte[] raw = await Execute(uri);
            var result = Encoding.UTF8.GetString(raw);
            return result;
        }

        /// <summary>
        /// Executes a REST request and parses the body as JSON.
        /// </summary>
        /// <typeparam name="T">The type of object expected to be returned</typeparam>
        /// <param name="uri">The full URI of the REST request</param>
        /// <returns>The deserialized JSON object</returns>
        public async Task<T> ExecuteJson<T>(string uri)
        {
            try
            {
                var json = await ExecuteString(uri);
                return DeserializeJson<T>(json);
            }
            catch
            {
                // TODO: log exception
                return default(T);
            }
        }

        /// <summary>
        /// Executes a REST request and decodes the body as base-64
        /// </summary>
        /// <param name="uri">The full URI of the REST request</param>
        /// <returns>The base-64 decoded bytes</returns>
        public async Task<byte[]> ExecuteBase64(string uri)
        {
            var base64 = await ExecuteString(uri);
            var bytes = Convert.FromBase64String(base64);
            // TODO: is the data in some kind of AMF format? Can we deserialize it?
            return bytes;
        }
        /*
        /// <summary>
        /// Executes a REST request, decodes the body as base-64, and parses the result as AMF (?).
        /// </summary>
        /// <typeparam name="T">The type of object expected to be returned</typeparam>
        /// <param name="uri">The full URI of the REST request</param>
        /// <returns>The deserialized AMF object</returns>
        public async Task<T> ExecuteBase64<T>(string uri)
        {
            var base64 = await ExecuteString(uri);
            var bytes = Convert.FromBase64String(base64);
            var amf = Encoding.UTF8.GetString(bytes);
            return DeserializeAmf<T>(amf);
        }
        */
        /// <summary>
        /// Executes a REST request, decrypts and decompresses the body, and parses the result as JSON.
        /// NOTE: this method is not currently working, nor should it be used.
        /// </summary>
        /// <typeparam name="T">The type of object expected to be returned</typeparam>
        /// <param name="uri">The full URI of the REST request</param>
        /// <param name="encryptionKey">The key used to encrypt/decrypt the data</param>
        /// <returns>The deserialized JSON object</returns>
        public async Task<T> ExecuteEncrypted<T>(string uri, string encryptionKey)
        {
            byte[] encrypted = await Execute(uri);
            // Attemp to decrypt
            var encryptionKeyBytes = Convert.FromBase64String(encryptionKey);
            var blowfish = new Blowfish(encryptionKeyBytes);
            // TODO: should there be an IV? What part of the data contains the IV? Assume it is at the start...
            var iv = new byte[8];
            var data = new byte[encrypted.Length - iv.Length];
            Array.Copy(encrypted, 0, iv, 0, iv.Length);
            Array.Copy(encrypted, iv.Length, data, 0, data.Length);
            //blowfish.IV = iv;
            byte[] decrypted = blowfish.Decrypt_ECB(encrypted);
            string s = Encoding.UTF8.GetString(decrypted);// for testing

            // Decompress
            using (var stream = new MemoryStream(decrypted))
            using (var zip = new GZipStream(stream, CompressionMode.Decompress))
            using (var reader = new StreamReader(zip))
            {
                string json = reader.ReadToEnd();
                return DeserializeJson<T>(json);
            }
        }

        /// <summary>
        /// Converts a JSON string to a CLR object
        /// </summary>
        private T DeserializeJson<T>(string json)
        {
            // Convert json string to CLR object
            jsonConverter.AddSupportedType(typeof(T));
            var obj = jsonSerializer.Deserialize<T>(json);
            return obj;
        }
    }
}
