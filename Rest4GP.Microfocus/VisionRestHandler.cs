using Microsoft.Extensions.Caching.Memory;
using Rest4GP.Core.Data;
using Vision4GP.Core.FileSystem;

namespace Rest4GP.Microfocus
{

    /// <summary>
    /// Vision rest handler
    /// </summary>
    internal class VisionRestHandler : DataRequestHandler
    {

        /// <summary>
        /// Creates a new instance of VisionRestHandler
        /// </summary>
        /// <param name="root">Root of the request to handle</param>
        /// <param name="memoryCache">Memory cache implementation</param>
        /// <param name="options">Data option</param>
        internal VisionRestHandler(string root, IMemoryCache memoryCache, DataRequestOptions options) 
            : base(root, new VisionDataContext(VisionFileSystem.GetInstance()), memoryCache, options)
        {
        }
    }


}