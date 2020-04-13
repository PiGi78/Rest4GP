using System.Linq;
using System.Web;
using System.Collections.Generic;

// Newtonsoft because System.Text.Json does not deserialize
// object as instance of an object but as instance of JsonElement
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using Newtonsoft.Json.Serialization;

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
            if (query?.AllKeys?.Any() == true)
            {
                foreach (var key in query.AllKeys)
                {
                    switch (key.ToUpperInvariant())
                    {
                        case "TAKE":
                            var takeValue = query[key];
                            if (!string.IsNullOrEmpty(takeValue) && int.TryParse(query[key], out int take)) result.Take = take;
                            break;
                        case "SKIP":
                            var skipValue = query[key];
                            if (!string.IsNullOrEmpty(skipValue) && int.TryParse(skipValue, out int skip)) result.Skip = skip;
                            break;
                        case "WITHCOUNT":
                            var withCountValue = query[key];
                            if (!string.IsNullOrEmpty(withCountValue) && bool.TryParse(query[key], out bool count)) result.WithCount = count;
                            break;
                        case "SORT":
                            var jsonSort = query[key];
                            if (!string.IsNullOrEmpty(jsonSort))
                            {
                                result.Sort = new RestSort();
                                result.Sort.Fields = JsonConvert.DeserializeObject<List<RestSortField>>(jsonSort, GetJsonConverterSettings());
                            }
                            break;
                        case "FILTER":
                            var filterValue = query[key];
                            result.Filter = JsonConvert.DeserializeObject<RestFilter>(filterValue, GetJsonConverterSettings());
                            break;
                        case "SMARTFILTER":
                            result.SmartFilter = new RestSmartFilter(query[key]);
                            break;
                        default:
                            break;
                    }
                }
            }

            return result;
        }


        /// <summary>
        /// Setting for JSON.NET deserialization
        /// </summary>
        /// <returns></returns>
        private JsonConverter[] GetJsonConverterSettings()
        {
            var result = new List<JsonConverter>();

            result.Add(new StringEnumConverter {
                NamingStrategy = new CamelCaseNamingStrategy()
            });

            return result.ToArray();
        }
    }
}
