using System.Collections.Generic;

namespace Rest4GP.Core
{

    /// <summary>
    /// Rest http response
    /// </summary>
    public class RestResponse
    {
        

        /// <summary>
        /// Headers
        /// </summary>
        public Dictionary<string, string> Headers { get; } = new Dictionary<string, string>();

        /// <summary>
        /// Status code
        /// </summary>
        public int StatusCode { get; set; }


        /// <summary>
        /// Content
        /// </summary>
        public string Content { get; set; }
    }
}