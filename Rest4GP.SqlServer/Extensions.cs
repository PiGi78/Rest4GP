using System.Text;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Rest4GP.Core;
using Rest4GP.Core.Parameters;
using Newtonsoft.Json;
using System.Data.SqlClient;
using Rest4GP.Core.Data;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Options;

namespace Rest4GP.SqlServer
{

    /// <summary>
    /// Extensions
    /// </summary>
    public static class Extensions
    {

        /// <summary>
        /// Adds a Sql Server data handler
        /// </summary>
        /// <param name="services">Services where to add the handler</param>
        /// <param name="options">Sql options</param>
        /// <returns>Services with the handler</returns>
        public static IServiceCollection AddSqlDataHandler(this IServiceCollection services, Action<SqlDataOptions> options)
        {
            var opt = new SqlDataOptions();
            options.Invoke(opt);
            services.AddScoped<IRestRequestHandler>(x => {
                var mem = x.GetRequiredService<IMemoryCache>();
                var dataOpt = x.GetRequiredService<DataRequestOptions>();
                return new SqlRestHandler(opt, mem, dataOpt);
            });
            return services;
        }


    }
}