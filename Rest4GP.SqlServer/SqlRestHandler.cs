using Microsoft.Extensions.Caching.Memory;
using Rest4GP.Core.Data;

namespace Rest4GP.SqlServer
{

    /// <summary>
    /// Sql Server rest handler
    /// </summary>
    public class SqlRestHandler : DataRequestHandler
    {

        /// <summary>
        /// Creates a new instance of SqlRestHandler
        /// </summary>
        /// <param name="sqlOptions">Sql options</param>
        /// <param name="memoryCache">Memory cache implementation</param>
        /// <param name="options">Data option</param>
        public SqlRestHandler(SqlDataOptions sqlOptions, IMemoryCache memoryCache, DataRequestOptions options) 
            : base(new SqlDataContext(sqlOptions), memoryCache, options)
        {
        }
    }
}