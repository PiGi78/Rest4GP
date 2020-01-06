using Rest4GP.Core.Data.Entities;

namespace Rest4GP.SqlServer
{

    /// <summary>
    /// Rest manager for a table
    /// </summary>
    public class SqlTableManager : SqlViewManager
    {
        
        /// <summary>
        /// Creates a new instance of SqlTableManager
        /// </summary>
        /// <param name="metadata">Table metadata</param>
        /// <param name="options">Sql data options</param>
        public SqlTableManager(EntityMetadata metadata, SqlDataOptions options) 
                : base(metadata, options)
        {
        }
    }
}