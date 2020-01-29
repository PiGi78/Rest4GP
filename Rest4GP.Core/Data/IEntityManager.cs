using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
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


        /// <summary>
        /// Insert a new entity in the database
        /// </summary>
        /// <param name="fields">List of the properties of the entity</param>
        /// <returns>Properties of the key of the inserted item</returns>
        Task<IDictionary<string, object>> InsertEntityAsync(IDictionary<string, object> fields);


        /// <summary>
        /// Update an entity in the database
        /// </summary>
        /// <param name="fields">List of the properties of the entity</param>
        /// <returns>Result of the update</returns>
        Task<IList<ValidationResult>> UpdateEntityAsync(IDictionary<string, object> fields);


        /// <summary>
        /// Delete an entity from the database
        /// </summary>
        /// <param name="fields">List of the properties of the key of the entity</param>
        /// <returns>Result of the delete</returns>
        Task<IList<ValidationResult>> DeleteEntityAsync(IDictionary<string, object> fields);

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