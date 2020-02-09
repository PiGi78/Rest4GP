using System.Collections.Immutable;
using System.Text;
using System.Security.Cryptography.X509Certificates;
using System;
using System.Collections.Generic;
using System.Data.SqlClient;
using System.Linq;
using System.Threading.Tasks;
using Rest4GP.Core.Data.Entities;
using System.ComponentModel.DataAnnotations;

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
        { }



        /// <summary>
        /// Insert a new entity in the database
        /// </summary>
        /// <param name="fields">List of the properties of the entity</param>
        /// <returns>Properties of the key of the inserted item</returns>
        public override async Task<IDictionary<string, object>> InsertEntityAsync(IDictionary<string, object> fields)
        {
            if (fields == null) throw new ArgumentNullException(nameof(fields));

            // Empty results (default)
            IDictionary<string, object> result = null;

            // Converts to parameter values
            var pValues = ConvertToParameterValues(fields);

            // Take off the readonly properties (can't be written)
            var writableValues = pValues.Where(x => x.IsReadOnly == false);

            // Check for a readonly PK (identity/autoincrement)
            // Only one property is managed
            var pkReadonlyValue = EntityMetadata.Fields.SingleOrDefault(x => x.IsReadOnly && x.IsPrimaryKey);

            var pkReadonlyQuery = pkReadonlyValue == null ? string.Empty : $"OUTPUT INSERTED.[{pkReadonlyValue.Name}]";

            // Compose the query
            var sql = $"INSERT INTO {GetDbOjectName()} ({string.Join(", ", writableValues.Select(x => x.DbColumnName))}) " +
                      $" {pkReadonlyQuery} " +
                      $"VALUES ({string.Join(", ", writableValues.Select(x => x.DbParameterName))})";
            
            // Start connection
            using (var connection = new SqlConnection(Options.ConnectionString))
            {
                await connection.OpenAsync();

                // Create command for insert
                using (var command = connection.CreateCommand())
                {
                    command.CommandText = sql;

                    // Add parameters
                    foreach (var paramValue in writableValues)
                    {
                        command.Parameters.AddWithValue(paramValue.DbParameterName, paramValue.Value ?? DBNull.Value);
                    }

                    // Query execution
                    var queryResult = await command.ExecuteScalarAsync();

                    // Response
                    result = new Dictionary<string, object>();
                    foreach (var pkProperty in writableValues.Where(x => x.IsPrimaryKey))
                    {
                        if (pkProperty.IsReadOnly)
                        {
                            result.Add(pkProperty.Name, queryResult);
                        }
                        else
                        {
                            result.Add(pkProperty.Name, pkProperty.Value);
                        }
                    }
                    if (pkReadonlyValue != null &&
                        !result.Keys.Contains(pkReadonlyValue.Name))
                    {
                        result.Add(pkReadonlyValue.Name, queryResult);
                    }
                }
                // Close connection
                connection.Close();
            }

            return result;
        }



        /// <summary>
        /// Update an entity in the database
        /// </summary>
        /// <param name="fields">List of the properties of the entity</param>
        /// <returns>List of validation errors or null if anything is ok</returns>
        public override async Task<IList<ValidationResult>> UpdateEntityAsync(IDictionary<string, object> fields)
        {
            if (fields == null) throw new ArgumentNullException(nameof(fields));

            // Converts to parameter values
            var pValues = ConvertToParameterValues(fields);

            // Take off the readonly properties (can't be written)
            var writableValues = pValues.Where(x => x.IsReadOnly == false && x.IsPrimaryKey == false);

            // Primary key values
            var pkValues = pValues.Where(x => x.IsPrimaryKey == true);

            // Check that all primary keys are specified
            if (pkValues.Count() != EntityMetadata.Fields.Count(x => x.IsPrimaryKey))
            {
                var result = new List<ValidationResult>();
                result.Add(new ValidationResult("All key columns has to be set"));
                return result;
            }

            // Compose the query
            var queryBuilder = new StringBuilder();
            queryBuilder.Append($"UPDATE {GetDbOjectName()}");

            // Appends all fields
            queryBuilder.Append(" SET ");
            var separator = string.Empty;
            foreach (var writableValue in writableValues)
            {
                queryBuilder.Append($"{separator}{writableValue.DbColumnName} = {writableValue.DbParameterName} ");
                separator = ", ";
            }

            // Appends key values
            queryBuilder.Append(" WHERE ");
            separator = string.Empty;
            foreach (var pkValue in pkValues)
            {
                queryBuilder.Append($"{separator}{pkValue.DbColumnName} = {pkValue.DbParameterName} ");
                separator = ", ";
            }
            
            // Complete query
            var sql = queryBuilder.ToString();

            // Start connection
            using (var connection = new SqlConnection(Options.ConnectionString))
            {
                await connection.OpenAsync();

                // Create command for insert
                using (var command = connection.CreateCommand())
                {
                    command.CommandText = sql;

                    // Add parameters
                    foreach (var paramValue in writableValues)
                    {
                        command.Parameters.AddWithValue(paramValue.DbParameterName, paramValue.Value ?? DBNull.Value);
                    }
                    foreach (var paramValue in pkValues)
                    {
                        command.Parameters.AddWithValue(paramValue.DbParameterName, paramValue.Value ?? DBNull.Value);
                    }

                    // Query execution
                    var queryResult = await command.ExecuteNonQueryAsync();

                    // If no row update, element not found
                    if (queryResult == 0)
                    {
                        var result = new List<ValidationResult>();
                        result.Add(new ValidationResult("No record found"));
                        return result;
                    }
                }
                // Close connection
                connection.Close();
            }

            // Anything ok
            return new List<ValidationResult>();
        }



        /// <summary>
        /// Delete an entity from the database
        /// </summary>
        /// <param name="fields">List of the properties of the entity</param>
        /// <returns>List of validation errors or null if anything is ok</returns>
        public override async Task<IList<ValidationResult>> DeleteEntityAsync(IDictionary<string, object> fields)
        {
            if (fields == null) throw new ArgumentNullException(nameof(fields));

            // Converts to parameter values
            var pValues = ConvertToParameterValues(fields);

            // Primary key values
            var pkValues = pValues.Where(x => x.IsPrimaryKey == true);

            // Check that all primary keys are specified
            if (pkValues.Count() != EntityMetadata.Fields.Count(x => x.IsPrimaryKey))
            {
                var result = new List<ValidationResult>();
                result.Add(new ValidationResult("All key columns has to be set"));
                return result;
            }

            // Compose the query
            var queryBuilder = new StringBuilder();
            queryBuilder.Append($"DELETE FROM {GetDbOjectName()}");

            // Appends key values
            queryBuilder.Append(" WHERE ");
            var separator = string.Empty;
            foreach (var pkValue in pkValues)
            {
                queryBuilder.Append($"{separator}{pkValue.DbColumnName} = {pkValue.DbParameterName} ");
                separator = ", ";
            }
            
            // Complete query
            var sql = queryBuilder.ToString();

            // Start connection
            using (var connection = new SqlConnection(Options.ConnectionString))
            {
                await connection.OpenAsync();

                // Create command for insert
                using (var command = connection.CreateCommand())
                {
                    command.CommandText = sql;

                    // Add parameters
                    foreach (var paramValue in pkValues)
                    {
                        command.Parameters.AddWithValue(paramValue.DbParameterName, paramValue.Value ?? DBNull.Value);
                    }

                    // Query execution
                    var queryResult = await command.ExecuteNonQueryAsync();

                    // If no row update, element not found
                    if (queryResult == 0)
                    {
                        var result = new List<ValidationResult>();
                        result.Add(new ValidationResult("No record found"));
                        return result;
                    }
                }
                // Close connection
                connection.Close();
            }

            // Anything ok
            return new List<ValidationResult>();
        }


        /// <summary>
        /// Converts the list of fields in a list of parameter values
        /// </summary>
        /// <param name="fields">Fields to be converted</param>
        /// <returns>List of parameter values</returns>
        private IList<ParameterValue> ConvertToParameterValues(IDictionary<string, object> fields) 
        {
            if (fields == null) throw new ArgumentNullException(nameof(fields));

            var result = new List<ParameterValue>();

            foreach (var metadata in EntityMetadata.Fields)
            {
                var fieldKey = fields.Keys.SingleOrDefault(x => x.Equals(metadata.Name, StringComparison.InvariantCultureIgnoreCase));
                if (fieldKey != null)
                {
                    var pValue = new ParameterValue {
                        Name = metadata.Name,
                        Value = fields[fieldKey],
                        DbColumnName = $"[{metadata.Name}]",
                        IsPrimaryKey = metadata.IsPrimaryKey,
                        IsReadOnly = metadata.IsReadOnly,
                        DbParameterName = $"@{metadata.Name}"
                    };
                    result.Add(pValue);
                }
            }

            return result;
        }


        #region Param properties


        /* This class rappresents the intersection between
         * the given parameters (IDictionary) and the
         * properties of the Entity.
         *
         * It is an utility class made only for this purpose,
         * so doesn't need to be shared
         */

        /// <summary>
        /// Parameter property and values
        /// </summary>
        private class ParameterValue
        {

            /// <summary>
            /// Name of the parameter
            /// </summary>
            public string Name { get; set; }

            /// <summary>
            /// Value of the parameter
            /// </summary>
            public object Value { get; set; }

            /// <summary>
            /// True if the parameter is a primary key column in the DB
            /// </summary>
            public bool IsPrimaryKey { get; set; }

            /// <summary>
            /// True if the parameter is read only in the DB
            /// </summary>
            public bool IsReadOnly { get; set; }

            /// <summary>
            /// Name of the column in the database table (with squares)
            /// </summary>
            public string DbColumnName { get; set; }

            /// <summary>
            /// Parameter name used in the database query
            /// </summary>
            public string DbParameterName { get; set; }
        }


        #endregion

    }
}