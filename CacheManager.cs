using System;
using System.Collections.Generic;
using System.Fabric;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using System.Timers;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Caching.Redis;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.ServiceFabric.Services.Communication.Runtime;
using Microsoft.ServiceFabric.Services.Runtime;

namespace CacheManager
{
    /// <summary>
    /// An instance of this class is created for each service instance by the Service Fabric runtime.
    /// </summary>
    internal sealed class CacheManager : StatelessService
    {

        #region Public Variables
        public static IConfiguration Configuration { get; set; }
        public static IApiRequestHandler ApiRequestHandler { get; set; }
        #endregion

        #region Private Variables
        private static System.Timers.Timer _timer;
        #endregion

        #region Constructors
        public CacheManager(StatelessServiceContext context)
            : base(context)
        {
            IConfigurationBuilder builder = new ConfigurationBuilder()
                .SetBasePath(Directory.GetCurrentDirectory())
                //.AddJsonFile("appsettings.json", true, true)
                //.AddServiceFabricConfig("Config")
                .AddEnvironmentVariables();
            Configuration = builder.Build();
            IDistributedCache cache = new RedisCache(new RedisCacheOptions
            {
                Configuration = Configuration.GetSection("Appsettings:ConnectionString").Value,
                InstanceName = "master"
            });
            ApiRequestHandler = new ApiRequestHandler();
            IServiceCollection services = new ServiceCollection();
            services.AddSingleton(Configuration);
            services.AddScoped<IApiRequestHandler, ApiRequestHandler>();

            services.AddSingleton(CacheHandler.Instance);
            CacheHandler.Instance.InitCacheHandler(Configuration, ApiRequestHandler, cache);

        } 
        #endregion

        /// <summary>
        /// Optional override to create listeners (e.g., TCP, HTTP) for this service replica to handle client or user requests.
        /// </summary>
        /// <returns>A collection of listeners.</returns>
        protected override IEnumerable<ServiceInstanceListener> CreateServiceInstanceListeners()
        {
            return new ServiceInstanceListener[0];
        }

        /// <summary>
        /// This is the main entry point for your service instance.
        /// </summary>
        /// <param name="cancellationToken">Canceled when Service Fabric needs to shut down this service instance.</param>
        protected override async Task RunAsync(CancellationToken cancellationToken)
        {
            await Task.Run(() => SetTimer(), cancellationToken);
        }

        private static void SetTimer()
        {
            string timerInterval = Configuration.GetSection("Appsettings:RefreshInterval").Value;

            _timer = new System.Timers.Timer(Convert.ToDouble(timerInterval));
            // Hook up the Elapsed event for the timer. 
            _timer.Elapsed += OnTimedEvent;
            _timer.Enabled = true;
        }

        private static void OnTimedEvent(object source, ElapsedEventArgs e)
        {
            Console.WriteLine("The Elapsed event was raised at {0:HH:mm:ss.fff}",
                e.SignalTime);

            CacheHandler.Instance.PrepareCacheEntry();
        }

    }
}
