using System.Web;
using System.Collections.Generic;
using System.Text.Json;

namespace Rest4GP.Core.Parameters.Converters
{

    /// <summary>
    /// Default converter
    /// </summary>
    /// <remarks>
    /// Takes the parameters from the query string.
    /// Simple properties must match the names, 
    /// complex properties must match the name and be in JSON format
    /// </remarks>
    public class DefaultParametersConverter : IParametersConverter
    {

        
        /// <summary>
        /// Converts a query string to rest parameters
        /// </summary>
        /// <param name="queryString">Query string to convert</param>
        /// <returns>Rest parameters that matches the query string</returns>
        public RestParameters ToRestParameters(string queryString)
        {
            if (string.IsNullOrEmpty(queryString)) return null;

            var result = new RestParameters();
            var query = HttpUtility.ParseQueryString(queryString);
            foreach (var key in query.AllKeys)
            {
                switch (key.ToUpperInvariant())
                {
                    case "$TAKE":
                        if (int.TryParse(query[key], out int take)) result.Take = take;
                        break;
                    case "$SKIP":
                        if (int.TryParse(query[key], out int skip)) result.Skip = skip;
                        break;
                    case "$WITHCOUNT":
                        if (bool.TryParse(query[key], out bool count)) result.WithCount = count;
                        break;
                    case "$SORT":
                        var jsonSort = query[key];
                        if (!string.IsNullOrEmpty(jsonSort))
                        {
                            result.Sort.Fields = JsonSerializer.Deserialize<List<RestSortField>>(jsonSort);
                        }
                        break;
                    case "$FILTER":
                        var filterValue = query[key];
                        result.Filter = JsonSerializer.Deserialize<RestFilter>(filterValue);
                        break;
                    case "$SMARTFILTER":
                        result.SmartFilter = new RestSmartFilter(query[key]);
                        break;
                    default:
                        break;
                }
            }

            return result;
        }
    }
}
