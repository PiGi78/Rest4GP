using System;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;
using Rest4GP.Core.Data;

namespace Rest4GP.Core
{

    /// <summary>
    /// Extesions for Rest4GP
    /// </summary>
    public static class Extensions
    {

        /// <summary>
        /// Adds a RestHandler
        /// </summary>
        /// <param name="app"><see cref="IApplicationBuilder"/> where to append the REST handler</param>
        /// <returns><see cref="IApplicationBuilder"/> with the given handler</returns>
        public static IApplicationBuilder UseRestHandler(this IApplicationBuilder app)
        {
            // Check params
            if (app == null) throw new ArgumentNullException(nameof(app));

            // Register the middleware
            return app.UseMiddleware<RestMiddleware>();
        }


        /// <summary>
        /// Adds a rest data handler
        /// </summary>
        /// <param name="services">Services collection where to add the handler</param>
        /// <param name="options">Option of the handler</param>
        /// <returns>Services with the configured handler</returns>
        public static IServiceCollection AddRestDataHandler(this IServiceCollection services, Action<DataRequestOptions> options = null)
        {
            var opt = new DataRequestOptions();
            if (options != null) options.Invoke(opt);
            services.AddMemoryCache();
            services.AddScoped<DataRequestOptions>(x => opt);
            return services;
        }
    }
}
