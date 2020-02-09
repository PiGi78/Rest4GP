using System.Diagnostics;
using System.Dynamic;
using System.Data;
using System.Linq;
using System;
using System.Collections.Generic;
using System.Data.SqlClient;
using System.Text;
using System.Threading.Tasks;
using Rest4GP.Core.Data;
using Rest4GP.Core.Data.Entities;
using Rest4GP.Core.Parameters;
using System.ComponentModel.DataAnnotations;

namespace Rest4GP.SqlServer
{

    /// <summary>
    /// Entity manager for Sql Server Views
    /// </summary>
    public class SqlViewManager : IEntityManager
    {


        /// <summary>
        /// Creates a new instance of SqlViewManager
        /// </summary>
        /// <param name="metadata">View metadata</param>
        /// <param name="options">Sql data options</param>
        public SqlViewManager (EntityMetadata metadata, SqlDataOptions options)
        {
            EntityMetadata = metadata ?? throw new ArgumentNullException(nameof(options));
            Options = options ?? throw new ArgumentNullException(nameof(options));
        }

        /// <summary>
        /// Metadata
        /// </summary>
        public EntityMetadata EntityMetadata { get; }

        /// <summary>
        /// Options
        /// </summary>
        protected SqlDataOptions Options { get; }


        /// <summary>
        /// Fetch all entities that match the given parameters
        /// </summary>
        /// <param name="parameters">Parameters to filter data</param>
        /// <returns>Entities that match the given parameters</returns>
        public async Task<FetchEntitiesResponse> FetchEntitiesAsync(RestParameters parameters)
        {   
            // Base query
            var sql = $"SELECT {string.Join(", ", GetColumnNames())} FROM {GetDbOjectName()}";
            
            // Query for count
            string countSql = null;

            // Sql parameters
            var sqlParameters = new List<SqlParameter>();

            if (parameters != null)
            {
                // Apply filters
                var filteredSql = ApplyFilters(sql, parameters, out sqlParameters);

                // Apply sort
                var sortedSql = ApplySort(filteredSql, parameters);

                // Pagination
                sql = ApplyPagination(sortedSql, parameters);

                // Check if count is required (if so, the query for count is the filtered one)
                countSql = parameters.WithCount ? filteredSql : null;
            }
            // Execute the query
            var result = await ExecuteQueryAsync(sql, sqlParameters, countSql);

            // Return response
            return result;
        }


        /// <summary>
        /// Executes the query for extract data
        /// </summary>
        /// <param name="sql">Query to execute for extract data</param>
        /// <param name="parameters">Parameters to set</param>
        /// <param name="countSql">Query to execute for count (null if no count required)</param>
        /// <returns>Result of the query</returns>
        private async Task<FetchEntitiesResponse> ExecuteQueryAsync(string sql, List<SqlParameter> parameters, string countSql = null)
        {
            if (string.IsNullOrEmpty(sql)) throw new ArgumentNullException(nameof(sql));
            if (parameters == null) throw new ArgumentNullException(nameof(parameters));
            
            var result = new FetchEntitiesResponse();

            // Start connection
            using (var connection = new SqlConnection(Options.ConnectionString))
            {
                await connection.OpenAsync();

                // Create command for data
                using (var command = connection.CreateCommand())
                {
                    command.CommandText = sql;
                    foreach (var param in parameters)
                    {
                        command.Parameters.Add(param);
                    }

                    // Query execution
                    using (var reader = await command.ExecuteReaderAsync(CommandBehavior.SequentialAccess))
                    {
                        if (reader.HasRows)
                        {
                            while (await reader.ReadAsync())
                            {
                                var obj = new ExpandoObject() as IDictionary<string, object>;
                                for (int i = 0; i < reader.FieldCount; i++)
                                {
                                    if (!await reader.IsDBNullAsync(i))
                                    {
                                        obj.Add(reader.GetName(i), reader.GetValue(i));
                                    }
                                }
                                result.Entities.Add(obj);
                            }
                        }
                    }

                    // Count (use the same command so I haven't to set parameters once more)
                    if (!string.IsNullOrEmpty(countSql))
                    {
                    
                        command.CommandText = $"WITH sel as ({countSql}) SELECT COUNT(*) FROM sel";;
                        // Query execution
                        result.TotalCount = (int) await command.ExecuteScalarAsync();   
                    }
                }
                // Close connection
                connection.Close();
            }

            // Result
            return result;
        }

        /// <summary>
        /// Gets the list of the name of the columns
        /// </summary>
        /// <returns>List of the column names</returns>
        protected List<string> GetColumnNames()
        {
            return EntityMetadata.Fields.Select(x => $"[{x.Name}]").ToList();
        }


        /// <summary>
        /// Gets the name of the view
        /// </summary>
        /// <returns>View name</returns>
        protected string GetDbOjectName()
        {
            return $"[{Options.Schema}].[{EntityMetadata.Name}]";
        }

        
        #region Query


        /// <summary>
        /// Applies sort to the query
        /// </summary>
        /// <param name="parameters">Parameters to use</param>
        /// <param name="sql">Query where apply the sort</param>
        /// <returns>Query with sort</returns>
        protected string ApplySort(string sql, RestParameters parameters)
        {
            if (string.IsNullOrEmpty(sql)) throw new ArgumentNullException(nameof(sql));
            if ((parameters?.Sort?.Fields?.Any()).GetValueOrDefault(false) == false) 
            {
                return $"{sql} ORDER BY 1";
            }

            var result = new StringBuilder();
            // Order by
            result.Append($"{sql} ORDER BY");
            bool isFirst = true;
            foreach (var clause in parameters.Sort.Fields)
            {
                if (!isFirst)
                { 
                    result.Append(",");
                }
                isFirst = false;
                var direction = clause.Direction == SortDirections.Ascending ? "ASC" : "DESC";
                result.Append($" [{clause.Field}] {direction}");
            }
            // if no fields, order by first column
            if (isFirst) result.Append(" 1");
            return result.ToString();
        }



        /// <summary>
        /// Applies pagination to the query
        /// </summary>
        /// <param name="parameters">Parameters to use</param>
        /// <param name="sql">Query where apply the pagination</param>
        /// <returns>Query with pagination</returns>
        protected string ApplyPagination(string sql, RestParameters parameters)
        {
            if (string.IsNullOrEmpty(sql)) throw new ArgumentNullException(nameof(sql));
            if (parameters == null ||
                (parameters.Take == 0 &&
                 parameters.Skip == 0)) return sql;

            var skipSql = $"OFFSET {parameters.Skip} ROWS";
            var takeSql = parameters.Take > 0 ? $"FETCH NEXT {parameters.Take} ROWS ONLY" : string.Empty;

            return $"{sql} {skipSql} {takeSql}";
        }



        /// <summary>
        /// Applies filters to a sql select
        /// </summary>
        /// <param name="parameters">Parameters where take filters</param>
        /// <param name="sql">Select where to apply filters</param>
        /// <param name="sqlParameters">Added parameter for filter</param>
        /// <returns>Query with filters</returns>
        protected string ApplyFilters(string sql, RestParameters parameters, out List<SqlParameter> sqlParameters)
        {
            if (string.IsNullOrEmpty(sql)) throw new ArgumentNullException(nameof(sql));
            if (parameters == null) throw new ArgumentNullException(nameof(parameters));
            
            sqlParameters = new List<SqlParameter>();

            // Filters to apply
            var filterToApply = ComposeFilter(parameters);
            if (filterToApply == null) return sql;

            // Get the where clause
            var whereClause = GetWhereClauseForFilter(filterToApply, sqlParameters);
            if (!string.IsNullOrEmpty(whereClause))
            {
                sql += $" WHERE ({whereClause})";
            }
            return sql;
        }

        /// <summary>
        /// Compose the filter to apply (using filter and smart filter)
        /// </summary>
        /// <param name="parameters">Request's parameter</param>
        /// <returns>Filter to apply, if any</returns>
        private RestFilter ComposeFilter(RestParameters parameters) 
        {
            if (parameters == null) throw new ArgumentNullException(nameof(parameters));

            // If no filters, null result
            if (parameters.Filter == null && 
                parameters.SmartFilter == null)
            {
                return null;
            }

            // Smart filter as filter
            var smartFilter = parameters.SmartFilter?.ComposeFilter(EntityMetadata.Fields);

            // If there is only a filter, that is the result
            if (smartFilter == null) return parameters.Filter;
            if (parameters.Filter == null) return smartFilter;

            // Compose the filter with both data
            return new RestFilter
            {
                Logic = FilterLogics.And,
                Filters = new List<RestFilter>() {
                    parameters.Filter,
                    smartFilter
                }
            };
        }

        /// <summary>
        /// Gets the where clause for the given filter
        /// </summary>
        /// <param name="filter">Filter to parse</param>
        /// <param name="sqlParameters">Added parameter for filter</param>
        /// <returns>Where clause, empty string if there are no filter</returns>
        private string GetWhereClauseForFilter(RestFilter filter, List<SqlParameter> sqlParameters)
        {
            if (sqlParameters == null) throw new ArgumentNullException(nameof(sqlParameters));
            if (filter == null) return string.Empty;

            var query = new StringBuilder();

            AppendWhereClauseForFilter(filter, query, sqlParameters);

            return query.ToString();
        }



        /// <summary>
        /// Composes the where clause for a simple filter
        /// </summary>
        /// <param name="filter">Filter</param>
        /// <param name="query">String builder where to add the where clause</param>
        /// <param name="sqlParameters">Added parameter for filter</param>
        private void AppendWhereClauseForFilter(RestFilter filter, StringBuilder query, List<SqlParameter> sqlParameters)
        {
            if (filter == null) return;
            if (query == null) throw new ArgumentNullException(nameof(query));


            // filtro semplice
            if (!string.IsNullOrEmpty(filter.Field))
            {
                AppendClauseForSimpleFilter(filter, query, sqlParameters);
            } else if (filter.Filters.Count > 0)
            {
                bool isFirst = true;
                var filterCount = filter.Filters.Count;
                for (int i = 0; i < filterCount; i++)
                {
                    if (isFirst)
                    {
                        query.Append("(");
                    }
                    else
                    {
                        var andOr = filter.Logic.ToLowerInvariant() == FilterLogics.And ? "AND" : "OR";
                        query.Append($" {andOr} (");
                    }
                    isFirst = false;
                    var filterToElab = filter.Filters[i];
                    AppendWhereClauseForFilter(filterToElab, query, sqlParameters);
                    query.Append(")");
                }
            }

        }


        /// <summary>
        /// Composes the where clause for a simple filter
        /// </summary>
        /// <param name="filter">Filter</param>
        /// <param name="query">String builder where to add the where clause</param>
        /// <param name="sqlParameters">Added parameter for filter</param>
        private void AppendClauseForSimpleFilter(RestFilter filter, StringBuilder query, List<SqlParameter> sqlParameters)
        {
            if (filter == null) return;
            if (query == null) throw new ArgumentNullException(nameof(query));

            if (string.IsNullOrEmpty(filter.Field)) throw new ArgumentException("Field value is mandatory", nameof(filter.Field));
            if (string.IsNullOrEmpty(filter.Operator)) throw new ArgumentException("Operato value is mandatory", nameof(filter.Operator));


            var parName = $"@{sqlParameters.Count}";
            // find the metadata
            var metadata = EntityMetadata.Fields
                                         .Where(x => string.Equals(x.Name, filter.Field, StringComparison.InvariantCultureIgnoreCase))
                                         .SingleOrDefault();
            switch (filter.Operator.ToLowerInvariant())
            {
                // Equal
                case FilterOperators.IsEqual:
                    AppendFilter(query, "=", filter, parName, metadata);
                    sqlParameters.Add(new SqlParameter(parName, filter.Value));
                    break;
                // Not equal
                case FilterOperators.IsNotEqual:
                    AppendFilter(query, "<>", filter, parName, metadata);
                    sqlParameters.Add(new SqlParameter(parName, filter.Value));
                    break;
                // Is null
                case FilterOperators.IsNull:
                    query.Append($" [{filter.Field}] IS NULL ");
                    break;
                // Is not null
                case FilterOperators.IsNotNull:
                    query.Append($" [{filter.Field}] IS NOT NULL ");
                    break;
                // Less than
                case FilterOperators.IsLessThan:
                    AppendFilter(query, "<", filter, parName, metadata);
                    sqlParameters.Add(new SqlParameter(parName, filter.Value));
                    break;
                // Less than or equal
                case FilterOperators.IsLessThanOrEqual:
                    AppendFilter(query, "<=", filter, parName, metadata);
                    sqlParameters.Add(new SqlParameter(parName, filter.Value));
                    break;
                // Greater than
                case FilterOperators.IsGreaterThan:
                    AppendFilter(query, ">", filter, parName, metadata);
                    sqlParameters.Add(new SqlParameter(parName, filter.Value));
                    break;
                // Greater than or equal
                case FilterOperators.IsGreatherThanOrEqual:
                    AppendFilter(query, ">=", filter, parName, metadata);
                    sqlParameters.Add(new SqlParameter(parName, filter.Value));
                    break;
                // Start with
                case FilterOperators.StartsWith:
                    AppendFilter(query, "LIKE", filter, parName, metadata);
                    var startValue = $"{filter.Value}%";
                    sqlParameters.Add(new SqlParameter(parName, startValue));
                    break;
                // End with
                case FilterOperators.EndsWith:
                    AppendFilter(query, "LIKE", filter, parName, metadata);
                    var endValue = $"%{filter.Value}";
                    sqlParameters.Add(new SqlParameter(parName, endValue));
                    break;
                // Contain
                case FilterOperators.Contains:
                    AppendFilter(query, "LIKE", filter, parName, metadata);
                    var contValue = $"%{filter.Value}%";
                    sqlParameters.Add(new SqlParameter(parName, contValue));
                    break;
                // Do not contain
                case FilterOperators.DoesNotContain:
                    AppendFilter(query, "NOT LIKE", filter, parName, metadata);
                    var notContValue = $"%{filter.Value}%";
                    sqlParameters.Add(new SqlParameter(parName, notContValue));
                    break;
                // Empty
                case FilterOperators.IsEmpty:
                    query.Append($" [{filter.Field}] = '' ");
                    break;
                // Not empty
                case FilterOperators.IsNotEmpty:
                    query.Append($" [{filter.Field}] <> '' ");
                    break;
                default:
                    throw new NotSupportedException($"Operator of type '{filter.Operator}' is not supported");
            }
        }


        /// <summary>
        ///  Append filter to a query checking for ignore case
        /// </summary>
        /// <param name="query">Where to add the filter</param>
        /// <param name="op">Operation</param>
        /// <param name="filter">Filter</param>
        /// <param name="parName">Name of the parameter</param>
        /// <param name="metadata">Field metadata</param>
        private void AppendFilter(StringBuilder query, string op, RestFilter filter, string parName, FieldMetadata metadata)
        {
            if (query == null) throw new ArgumentNullException(nameof(query));
            if (string.IsNullOrEmpty(op)) throw new ArgumentNullException(nameof(query));
            if (filter == null) throw new ArgumentNullException(nameof(filter));
            if (string.IsNullOrEmpty(parName)) throw new ArgumentNullException(nameof(parName));

            if (metadata?.Type == FieldDataTypes.String &&
                filter.IgnoreCase)
            {
                query.Append($" LOWER([{filter.Field}]) {op} LOWER({parName}) ");
            }
            else
            {
                query.Append($" [{filter.Field}] {op} {parName} ");
            }
        }



        #endregion



        #region Not implemented method

        /// <summary>
        /// Insert a new entity in the database
        /// </summary>
        /// <param name="fields">List of the properties of the entity</param>
        /// <returns>Properties of the key of the inserted item</returns>
        public virtual Task<IDictionary<string, object>> InsertEntityAsync(IDictionary<string, object> fields)
        {
            return null;
        }


        /// <summary>
        /// Update an entity in the database
        /// </summary>
        /// <param name="fields">List of the properties of the entity</param>
        /// <returns>Result of the update</returns>
        public virtual Task<IList<ValidationResult>> UpdateEntityAsync(IDictionary<string, object> fields)
        {
            return null;
        }


        /// <summary>
        /// Delete an entity from the database
        /// </summary>
        /// <param name="fields">List of the properties of the key of the entity</param>
        /// <returns>Result of the delete</returns>
        public virtual Task<IList<ValidationResult>> DeleteEntityAsync(IDictionary<string, object> fields)
        {
            return null;
        }

        #endregion
    }
}