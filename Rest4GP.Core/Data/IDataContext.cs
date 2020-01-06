using System.Collections.Generic;
using System.Threading.Tasks;

namespace Rest4GP.Core.Data
{

    /// <summary>
    /// Data context
    /// </summary>
    public interface IDataContext
    {

        /// <summary>
        /// Name of the data context
        /// </summary>
        string Name { get; }


        /// <summary>
        /// List of all entity managers
        /// </summary>
        /// <returns>Entity managers</returns>
        Task<List<IEntityManager>> FetchEntityManagersAsync();

    }
}