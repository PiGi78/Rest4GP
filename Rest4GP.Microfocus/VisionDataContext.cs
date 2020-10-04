using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Rest4GP.Core.Data;
using Vision4GP.Core.FileSystem;

namespace Rest4GP.Microfocus
{

    /// <summary>
    /// Microfocus vision data context
    /// </summary>
    public class VisionDataContext : IDataContext
    {

        /// <summary>
        /// Creates a new instance of MicrofocusDataContext
        /// </summary>
        /// <param name="fileSystem">Vision file system to use</param>
        public VisionDataContext(IVisionFileSystem fileSystem)
        {
            VisionFileSystem = fileSystem ?? throw new ArgumentNullException(nameof(fileSystem));
        }

        /// <summary>
        /// Vision file system
        /// </summary>
        private IVisionFileSystem VisionFileSystem { get; }

        /// <summary>
        /// List of all entity managers
        /// </summary>
        /// <returns>Entity managers</returns>
        public Task<List<IEntityManager>> FetchEntityManagersAsync()
        {
            var result = new List<IEntityManager>();
            foreach (var fileDefinition in VisionFileSystem.GetFileDefinitions())
            {
                result.Add(new VisionFileEntityManager(fileDefinition, VisionFileSystem));
            }
            return Task.FromResult(result);
        }
    }

}