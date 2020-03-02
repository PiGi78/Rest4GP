using System.Data.Common;
using System.Collections.Generic;
using System.Data.SqlClient;
using System.Threading.Tasks;
using Rest4GP.Core.Data;
using Rest4GP.Core.Data.Entities;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace Rest4GP.SqlServer
{

    /// <summary>
    /// Data context for SqlServer
    /// </summary>
    public class SqlDataContext : IDataContext
    {

        #region Constructors




        /// <summary>
        /// Creates a new instance of SqlDataContext
        /// </summary>
        /// <param name="options">Options</param>
        public SqlDataContext(SqlDataOptions options = null)
        {
            Options = options ?? new SqlDataOptions();
        }


        #endregion


        /// <summary>
        /// Data options
        /// </summary>
        private SqlDataOptions Options { get; }
        

        /// <summary>
        /// Name of the data context
        /// </summary>
        public string Name { get; }


        /// <summary>
        /// List of all entity managers
        /// </summary>
        /// <returns>Entity managers</returns>
        public async Task<List<IEntityManager>> FetchEntityManagersAsync()
        {
            var result = new List<IEntityManager>();
            // Metadata
            var metadatas = await FetchMetadataAsync();
            
            // Managers
            foreach (var metadata in metadatas)
            {
                if (metadata.IsReadOnly) 
                {
                    result.Add(new SqlViewManager(metadata, Options));
                }
                else
                {
                    result.Add(new SqlTableManager(metadata, Options));
                }
            }

            // Result
            return result;
        }



        /// <summary>
        /// Fetch metadata from the database
        /// </summary>
        /// <returns>List of all managed objects as entity metadata</returns>
        private async Task<List<EntityMetadata>> FetchMetadataAsync()
        {
            var result = new List<EntityMetadata>();
            using (var connection = CreateConnection())
            {
                await connection.OpenAsync();
                using (var command = connection.CreateCommand())
                {
                    command.CommandText = DESCRIBE_SCHEMA_QUERY;
                    command.Parameters.Add(new SqlParameter("@schema", Options.Schema));
                    // Removed the Async operator for performance issue
                    // (a DB with 1000 tables takes 20 sec with Async, 4 sec without async)
                    //var jsonContent = (string)(await command.ExecuteScalarAsync());
                    var jsonContent = (string)(command.ExecuteScalar());
                    var jsonOptions = new JsonSerializerOptions();
                    jsonOptions.Converters.Add(new JsonStringEnumConverter(JsonNamingPolicy.CamelCase));
                    result = JsonSerializer.Deserialize<List<EntityMetadata>>(jsonContent, jsonOptions);
                }
                connection.Close();
            }
            return result;
        }


        /// <summary>
        /// Creates a new connection
        /// </summary>
        /// <returns>Created connection</returns>
        private DbConnection CreateConnection()
        {
            return new SqlConnection(Options.ConnectionString);
        }



        /// <summary>
        /// Query used for extract schema informations
        /// </summary>
        private readonly string DESCRIBE_SCHEMA_QUERY = 
              @"DECLARE @RESULT AS NVARCHAR(MAX) = (
                   SELECT tbl.[TABLE_NAME] AS [Name] 
                        , tblProp.[value] AS [Description]
                        , CASE tblSysObj.[TYPE]
                             WHEN 'V' THEN CAST(1 as bit)
                             ELSE CAST(0 as bit)
                          END AS [IsReadOnly]

	                      --- Fields
	                    , (SELECT col.COLUMN_NAME AS [Name]
	                            , colSysObj.[value] AS [Description]
                                , CASE 
                                     WHEN UPPER(col.DATA_TYPE) = 'DATETIME' THEN 'DateTime'
                                     WHEN UPPER(col.DATA_TYPE) = 'DATETIME2' THEN 'DateTime'
                                     WHEN UPPER(col.DATA_TYPE) = 'DATE'     THEN 'Date'
                                     WHEN UPPER(col.DATA_TYPE) = 'TIME'     THEN 'Time'
                                     WHEN col.NUMERIC_PRECISION > 0  THEN 'Numeric'
                                     ELSE 'String'
                                  END AS [Type]
			                    , CASE 
			                         WHEN col.CHARACTER_MAXIMUM_LENGTH IS NULL THEN col.NUMERIC_PRECISION
				                     WHEN col.CHARACTER_MAXIMUM_LENGTH > 0     THEN col.CHARACTER_MAXIMUM_LENGTH
				                     ELSE 0
			                      END AS [Size]
			                    , CASE 
			                         WHEN col.NUMERIC_SCALE > 0 THEN col.NUMERIC_SCALE
				                     ELSE 0
			                      END AS [Scale]
			                    , CASE col.IS_NULLABLE
                                     WHEN 'YES' THEN CAST(0 as bit)
                                     ELSE CAST(1 as bit)
                                  END AS [IsRequired]
                                , CASE keyConstr.[type]
                                     WHEN 'Pk' THEN CAST(1 as bit)
                                     ELSE CAST(0 as bit)
                                  END AS [IsPrimaryKey]
                                , CASE
                                     WHEN (co.is_computed = 1 or ic.is_identity = 1) THEN CAST(1 as bit)
				                     ELSE CAST(0 as bit)
                                  END AS [IsReadOnly]
	                        FROM INFORMATION_SCHEMA.COLUMNS as col
		                         INNER JOIN  sys.columns co ON (co.object_id = tblSysObj.id AND co.name = col.COLUMN_NAME)
                                 LEFT OUTER JOIN sys.extended_properties colSysObj ON (colSysObj.major_id = tblSysObj.id AND colSysObj.minor_id = co.column_id AND colSysObj.name = 'MS_Description')
                                 LEFT OUTER JOIN sys.identity_columns ic ON (ic.object_id = co.object_id AND ic.column_id = co.column_id)
                                 LEFT OUTER JOIN sys.key_constraints keyConstr ON (keyConstr.parent_object_id = co.object_id AND keyConstr.unique_index_id = co.column_id)
		                    WHERE col.TABLE_NAME = tbl.TABLE_NAME AND col.TABLE_SCHEMA = tbl.TABLE_SCHEMA
		                    ORDER BY col.ORDINAL_POSITION
		                    FOR JSON PATH
	                        ) AS [Fields]

                   FROM INFORMATION_SCHEMA.TABLES as tbl
                        JOIN sysobjects as tblSysObj ON (tblSysObj.[name] = tbl.TABLE_NAME AND OBJECT_SCHEMA_NAME(tblSysObj.[id]) = tbl.TABLE_SCHEMA)
                        LEFT OUTER JOIN sys.extended_properties tblProp ON (tblProp.major_id = tblSysObj.id AND tblProp.minor_id = 0 AND tblProp.name = 'MS_Description')
                   WHERE tbl.TABLE_SCHEMA  = @schema AND 
                         (tblSysObj.[TYPE] = 'U' OR tblSysObj.[TYPE] = 'V')
                   FOR JSON PATH);
                SELECT @RESULT;";
    }
}