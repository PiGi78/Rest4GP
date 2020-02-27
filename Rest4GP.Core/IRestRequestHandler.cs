using System.Threading.Tasks;

namespace Rest4GP.Core
{
    /// <summary>
    /// REST request handler
    /// </summary>
    public interface IRestRequestHandler
    {

        /// <summary>
        /// HTTP of the root to handle
        /// </summary>
        string HandleRoot { get; set; }


        /// <summary>
        /// True if the handler can manage the request
        /// </summary>
        /// <param name="request">Request to check</param>
        /// <returns>True if the given request is managed by this manager, elsewhere false </returns>
        Task<bool> CanHandleAsync(RestRequest request);


        /// <summary>
        /// Handles the request and give back a response
        /// </summary>
        /// <param name="request">Request to process</param>
        /// <returns>Response</returns>
        Task<RestResponse> HandleRequestAsync(RestRequest request);

    }
}