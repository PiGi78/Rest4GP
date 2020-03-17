using System;
using Microsoft.Extensions.DependencyInjection;
using Rest4GP.Core;
using Rest4GP.Core.Data;
using Microsoft.Extensions.Caching.Memory;

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
        /// <param name="root">Root of the request to handle</param>
        /// <param name="services">Services where to add the handler</param>
        /// <param name="options">Sql options</param>
        /// <returns>Services with the handler</returns>
        public static IServiceCollection AddSql4GP(this IServiceCollection services, string root, Action<SqlDataOptions> options)
        {
            if (string.IsNullOrEmpty(root)) throw new ArgumentNullException(nameof(root));

            // Add handler
            var opt = new SqlDataOptions();
            options.Invoke(opt);
            services.AddTransient<IRestRequestHandler>(x => {
                var mem = x.GetRequiredService<IMemoryCache>();
                var dataOpt = x.GetRequiredService<DataRequestOptions>();
                return new SqlRestHandler(root, opt, mem, dataOpt);
            });

            // Returns the service collection
            return services;
        }


    }
}