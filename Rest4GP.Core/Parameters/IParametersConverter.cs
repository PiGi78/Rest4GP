using System;
using Microsoft.AspNetCore.Http;

namespace Rest4GP.Core.Parameters
{

    /// <summary>
    /// Parameter converter
    /// </summary>
    public interface IParametersConverter
    {

        /// <summary>
        /// Converts a query string to rest parameters
        /// </summary>
        /// <param name="queryString">Query string to convert</param>
        /// <returns>Rest parameters that matches the query string</returns>
        RestParameters ToRestParameters(string queryString);
        
    }
}
