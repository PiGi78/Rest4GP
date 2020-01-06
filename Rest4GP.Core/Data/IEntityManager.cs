using System.Collections.Generic;
using System.Threading.Tasks;
using Rest4GP.Core.Data.Entities;
using Rest4GP.Core.Parameters;

namespace Rest4GP.Core.Data
{

    /// <summary>
    /// Manager of a single entity
    /// </summary>
    public interface IEntityManager
    {


        /// <summary>
        /// Metatada of the managed entity
        /// </summary>
        EntityMetadata EntityMetadata { get; }


        /// <summary>
        /// Fetch all entities that match the given parameters
        /// </summary>
        /// <param name="parameters">Parameters to filter data</param>
        /// <returns>Entities that match the given parameters</returns>
        Task<FetchEntitiesResponse> FetchEntitiesAsync(RestParameters parameters);

    } 



    /// <summary>
    /// Response for the FetchEntities method
    /// </summary>
    public class FetchEntitiesResponse 
    {

        /// <summary>
        /// Count of all entities
        /// </summary>
        /// <remarks>
        /// Can be greater than the count of Entities porperty due to pagination
        /// </remarks>
        public int TotalCount { get; set; }

        /// <summary>
        /// List of requested entities
        /// </summary>
        public List<object> Entities { get; set; } = new List<object>();
    }
}