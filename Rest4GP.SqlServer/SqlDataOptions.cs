
namespace Rest4GP.SqlServer
{

    /// <summary>
    /// Options for Sql access
    /// </summary>
    public class SqlDataOptions
    {
        
        /// <summary>
        /// Connection string
        /// </summary>
        public string ConnectionString { get; set; }

        /// <summary>
        /// Database schema
        /// </summary>
        public string Schema { get; set; } = "dbo";

    }
}