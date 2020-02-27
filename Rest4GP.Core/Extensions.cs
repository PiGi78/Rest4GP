using System;
using System.Linq;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Routing;
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
        /// Adds the routing for the Rest4GP API
        /// </summary>
        /// <param name="app"><see cref="IApplicationBuilder"/> where to append the REST handler</param>
        /// <param name="mainRoot">Main root for the Rest4GP service. If not given, only the main root of the handler will be used</param>
        /// <returns><see cref="IApplicationBuilder"/> with the given handler</returns>
        public static IApplicationBuilder UseRest4GPRouting(this IApplicationBuilder app, string mainRoot = "")
        {
            // Check params
            if (app == null) throw new ArgumentNullException(nameof(app));

            // Add a route for each handler
            var handlers = app.ApplicationServices.GetServices<IRestRequestHandler>();
            if (handlers != null &&
                handlers.Count() > 0)
            {
                app.UseRouting();
                var builder = new RouteHandler(context => {
                    var middleware = app.ApplicationServices.GetService<RestRouteMiddleware>();
                    var hs = app.ApplicationServices.GetServices<IRestRequestHandler>();
                    return middleware.InvokeAsync(context, hs);
                });
                var routeBuilder = new RouteBuilder(app, builder);
                routeBuilder.MapRoute("Rest4GP", $"{mainRoot}/{{root}}/{{entity}}/{{metadata?}}");
                app.UseRouter(routeBuilder.Build());
            }

            return app;
        }


        /// <summary>
        /// Adds a rest data handler
        /// </summary>
        /// <param name="services">Services collection where to add the handler</param>
        /// <param name="options">Option of the handler</param>
        /// <returns>Services with the configured handler</returns>
        public static IServiceCollection AddRest4GP(this IServiceCollection services, Action<DataRequestOptions> options = null)
        {
            var opt = new DataRequestOptions();
            if (options != null) options.Invoke(opt);
            services.AddMemoryCache();
            services.AddTransient<DataRequestOptions>(x => opt);
            services.AddTransient<RestRouteMiddleware>();
            return services;
        }
    }
}
