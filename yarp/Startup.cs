using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Yarp.ReverseProxy.Configuration;
using Yarp.ReverseProxy.Model;

using System.Text.Json;

namespace Yarp
{
    public class Startup
    {
        private const string DEBUG_HEADER = "Debug";
        private const string DEBUG_METADATA_KEY = "debug";
        private const string DEBUG_VALUE = "true";

        /// <summary>
        /// This method gets called by the runtime. Use this method to add services to the container.
        /// </summary>
        public void ConfigureServices(IServiceCollection services)
        {
            // Programatically creating route and cluster configs. This allows loading the data from an arbitrary source.
            services.AddReverseProxy()
                .LoadFromMemory(GetRoutes(), GetClusters());
        }

        /// <summary>
        /// This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
        /// </summary>
        public void Configure(IApplicationBuilder app)
        {
            app.UseRouting();
            app.UseEndpoints(endpoints =>
            {
                // Proxyの構成を更新するためのエンドポイント
                endpoints.Map("/update", context =>
                {
                    context.RequestServices.GetRequiredService<InMemoryConfigProvider>().Update(GetRoutes(), GetClusters());
                    return Task.CompletedTask;
                });

                endpoints.MapReverseProxy();

                // // We can customize the proxy pipeline and add/remove/replace steps
                // endpoints.MapReverseProxy(proxyPipeline =>
                // {
                //     // Use a custom proxy middleware, defined below
                //     proxyPipeline.Use(MyCustomProxyStep);
                //     // Don't forget to include these two middleware when you make a custom proxy pipeline (if you need them).
                //     //proxyPipeline.UseSessionAffinity();
                //     proxyPipeline.UseLoadBalancing();
                // });
            });
        }

        private RouteConfig[] GetRoutes()
        {
            return new[]
            {
                new RouteConfig()
                {
                    RouteId = "route" + Random.Shared.Next(), // Forces a new route id each time GetRoutes is called.
                    ClusterId = "cluster1",
                    Match = new RouteMatch
                    {
                        // Path or Hosts are required for each route. This catch-all pattern matches all request paths.
                        Path = "{**catch-all}",
                        Headers = new []
                        {
                            new RouteHeader()
                            {
                                Name = "CustomHeader",
                                Values = new[] { "value1" },
                                Mode = HeaderMatchMode.ExactHeader
                            }
                        }
                    }
                },
                new RouteConfig()
                {
                    RouteId = "route" + Random.Shared.Next(), // Forces a new route id each time GetRoutes is called.
                    ClusterId = "cluster2",
                    Match = new RouteMatch
                    {
                        // Path or Hosts are required for each route. This catch-all pattern matches all request paths.
                        Path = "{**catch-all}",
                        Headers = new []
                        {
                            new RouteHeader()
                            {
                                Name = "CustomHeader",
                                Values = new[] { "value2" },
                                Mode = HeaderMatchMode.ExactHeader
                            }
                        }
                    }
                }
            };
        }
        private ClusterConfig[] GetClusters()
        {
            var debugMetadata = new Dictionary<string, string>();
            debugMetadata.Add(DEBUG_METADATA_KEY, DEBUG_VALUE);

            return new[]
            {
                new ClusterConfig()
                {
                    ClusterId = "cluster1",
                //    SessionAffinity = new SessionAffinityConfig { Enabled = true, Policy = "Cookie", AffinityKeyName = ".Yarp.ReverseProxy.Affinity" },
                    Destinations = new Dictionary<string, DestinationConfig>(StringComparer.OrdinalIgnoreCase)
                    {
                        { "destination1", new DestinationConfig() { Address = "https://msue-yarp-test.azurewebsites.net/" } },
                        // { "debugdestination1", new DestinationConfig() {
                        //     Address = "https://bing.com",
                        //     Metadata = debugMetadata  }
                        // },
                    }
                },
                new ClusterConfig()
                {
                    ClusterId = "cluster2",
                //    SessionAffinity = new SessionAffinityConfig { Enabled = true, Policy = "Cookie", AffinityKeyName = ".Yarp.ReverseProxy.Affinity" },
                    Destinations = new Dictionary<string, DestinationConfig>(StringComparer.OrdinalIgnoreCase)
                    {
                        { "destination1", new DestinationConfig() { Address = "https://msue-yarp-test2.azurewebsites.net/" } }
                    }
                },
            };
        }


        /// <summary>
        /// Custom proxy step that filters destinations based on a header in the inbound request
        /// Looks at each destination metadata, and filters in/out based on their debug flag and the inbound header
        /// </summary>
        // public Task MyCustomProxyStep(HttpContext context, Func<Task> next)
        // {
        //     // Can read data from the request via the context
        //     var useDebugDestinations = context.Request.Headers.TryGetValue(DEBUG_HEADER, out var headerValues) && headerValues.Count == 1 && headerValues[0] == DEBUG_VALUE;

        //     // The context also stores a ReverseProxyFeature which holds proxy specific data such as the cluster, route and destinations
        //     var availableDestinationsFeature = context.Features.Get<IReverseProxyFeature>();
        //     var filteredDestinations = new List<DestinationState>();

        //     // Filter destinations based on criteria
        //     foreach (var d in availableDestinationsFeature.AvailableDestinations)
        //     {
        //         //Todo: Replace with a lookup of metadata - but not currently exposed correctly here
        //         if (d.DestinationId.Contains("debug") == useDebugDestinations) { filteredDestinations.Add(d); }
        //     }
        //     availableDestinationsFeature.AvailableDestinations = filteredDestinations;

        //     // Important - required to move to the next step in the proxy pipeline
        //     return next();
        // }
    }
}