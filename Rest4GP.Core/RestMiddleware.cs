using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Rest4GP.Core.Parameters;

namespace Rest4GP.Core
{

    /// <summary>
    /// Middleware for rest api
    /// </summary>
    public class RestMiddleware
    {
        

        /// <summary>
        /// Creates a new instance of <see cref="RestMiddleware"/>
        /// </summary>
        /// <param name="next"><see cref="RequestDelegate"/> that follows the middleware in the pipeline</param>
        /// <param name="logger">Logger</param>
        public RestMiddleware(RequestDelegate next, ILogger<RestMiddleware> logger)
        {
            Next = next ?? throw new ArgumentNullException(nameof(next));
            Logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        /// <summary>
        /// Request delegate that follow this on the pipeline
        /// </summary>
        protected RequestDelegate Next { get; }


        /// <summary>
        /// Logger
        /// </summary>
        private ILogger<RestMiddleware> Logger { get; }


        /// <summary>
        /// Invoke the current middleware
        /// </summary>
        /// <param name="context">Context that invokes the middleware</param>
        /// <param name="requestHandlers">Registered rest handlers</param>
        /// <returns>Results</returns>
        public async Task InvokeAsync(HttpContext context, IEnumerable<IRestRequestHandler> requestHandlers)
        {
            if (context == null) throw new ArgumentNullException(nameof(context));

            // Wrap request into RestRequest
            var request = new RestRequest(context.Request);

            // Iterate thru all registered handlers
            foreach (var handler in requestHandlers)
            {
                var typeOfHandler = handler.GetType();
                var requestPath = request.Path;
                if (await handler.CanHandleAsync(request))
                {
                    Logger.LogDebug($"Handler of type {typeOfHandler} can handle the request with Path {requestPath}");
                    // Execute the operations
                    var handledResult = await handler.HandleRequestAsync(request);
                    // If there is a result, it will back to the caller
                    if (handledResult != null)
                    {
                        Logger.LogDebug($"Request with path {requestPath} is handled by {typeOfHandler}");
                        var response = context.Response;
                        // Add headers
                        AddHeaders(handledResult, response);
                        // Status code
                        context.Response.StatusCode = handledResult.StatusCode;
                        // Content
                        if (!string.IsNullOrEmpty(handledResult.Content))
                        {
                            await context.Response.WriteAsync(handledResult.Content);
                        }
                        return;
                    }
                    Logger.LogDebug($"No response from {typeOfHandler} when handling request path {requestPath}. Moving to next handler");
                }
                
            }
            await Next(context);
        }


        /// <summary>
        /// Adds the handled request headers to the resposne
        /// </summary>
        /// <param name="from">Response with headers to add</param>
        /// <param name="to">Response where to add the headers</param>
        private void AddHeaders(RestResponse from, HttpResponse to)
        {
            if (from == null) throw new ArgumentNullException(nameof(from));
            if (to == null) throw new ArgumentNullException(nameof(to));
            // add any single header
            foreach (var key in from.Headers.Keys)
            {
                var value = from.Headers[key];
                // if the key already exists, remove it
                if (to.Headers.ContainsKey(key))
                {
                    to.Headers.Remove(key);
                }
                // Add the value only if not empty
                if (!string.IsNullOrEmpty(value))
                {
                    to.Headers.Add(key, value);
                }
            }
        }

    }
}