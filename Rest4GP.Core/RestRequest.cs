using System.Linq;
using System;
using System.IO;
using System.Text;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using System.Diagnostics;

namespace Rest4GP.Core
{
    /// <summary>
    /// Http rest request
    /// </summary>
    public class RestRequest
    {
        
        /// <summary>
        /// Creates a new instance of RestRequest for the fiven http request
        /// </summary>
        /// <param name="originalRequest">Original http request</param>
        public RestRequest(HttpRequest originalRequest)
        {
            OriginalRequest = originalRequest ?? throw new ArgumentNullException(nameof(originalRequest));
        }

        /// <summary>
        /// Original http request
        /// </summary>
        public HttpRequest OriginalRequest  { get; }


        /// <summary>
        /// Method of the request
        /// </summary>
        public RestMethods Method
        {
            get
            {
                switch (OriginalRequest.Method.ToUpper())
                {
                    case "GET":
                        return RestMethods.Get;
                    case "POST":
                        return RestMethods.Post;
                    case "PUT":
                        return RestMethods.Put;
                    case "PATCH":
                        return RestMethods.Patch;
                    case "DELETE":
                        return RestMethods.Delete;
                }
                return RestMethods.Undefined;
            }
        }

#region Content


        private string _content = null;

        /// <summary>
        /// Request content
        /// </summary>
        public async Task<string> GetContentAsync()
        {
            // If already extracted, return it
            if (_content != null) return _content;

            // Enables rewind (else other method can't read the content anymore)
            OriginalRequest.EnableBuffering();
            
            // Read content
            _content = string.Empty;
            if (OriginalRequest.ContentLength > 0)
            {
                int bufferSize = (int)OriginalRequest.ContentLength.Value;
                using (var reader = new StreamReader(OriginalRequest.Body, Encoding.UTF8, false, bufferSize, leaveOpen: true))
                {
                    _content = await reader.ReadToEndAsync();
                }

                // Set the position to the beginning (for other that can read content)
                OriginalRequest.Body.Position = 0;
            }

            // Return the content
            return _content;
        }


#endregion


#region Path

        private string[] _paths = null;

        /// <summary>
        /// Path of the request
        /// </summary>
        public string Path => OriginalRequest?.Path;

        /// <summary>
        /// Path as array 
        /// </summary>
        public string[] Paths
        {
            get
            {
                if (_paths == null)
                {
                    _paths = new string[0];
                    if (OriginalRequest.Path.HasValue)
                    {
                        _paths = OriginalRequest.Path.Value.TrimStart('/').Split('/');
                    }
                }
                return _paths;
            }
        }


        /// <summary>
        /// Check if the request is for metadata
        /// </summary>
        /// <remarks>
        /// The chek is made on path: the last one has to be $metadata
        /// </remarks>
        /// <returns>True if the caller is asking for metadata only</returns>
        public bool IsMetadata() 
        {
            return Paths.Last().Equals("$metadata", StringComparison.InvariantCultureIgnoreCase);
        }


        /// <summary>
        /// Root of the request
        /// </summary>
        public string Root
        {
            get
            {
                return GetValueFromRoute("root");
            }
        }


        /// <summary>
        /// Entity of the request
        /// </summary>
        public string EntityName
        {
            get
            {
                return GetValueFromRoute("entity");
            }
        }


        /// <summary>
        /// Extract a value from the route path
        /// </summary>
        /// <param name="key">Key of the data</param>
        /// <returns>Value of the data or empty string if not found</returns>
        private string GetValueFromRoute(string key)
        {
            if (string.IsNullOrEmpty(key)) throw new ArgumentNullException(nameof(key));

            var result = string.Empty;
            var route = OriginalRequest.HttpContext.GetRouteData();
            Debug.WriteLine($"Routedata: {route?.ToString()}");
            if (route != null &&
                route.Values.TryGetValue(key, out var value) &&
                value != null)
            {
                var strValue = value.ToString();
                Debug.WriteLine($"Key: {key} - Value: {strValue}");
                // exclude metadata value
                if (strValue.Equals("$metadata", StringComparison.InvariantCultureIgnoreCase))
                {
                    strValue = string.Empty;
                }
                result = strValue;
            }
            return result;
        }

#endregion


    }
}