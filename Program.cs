using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using SimManager.Interfaces;
using SimManager.Models;
using System.IO;
using System.Threading.Tasks;

namespace SimManager
{
    public class Program
    {
        public static async Task<int> Main(string[] args)
        {
            var serviceCollection = new ServiceCollection();
            
            await ConfigureServices(serviceCollection).ConfigureAwait(false);
            var serviceProvider = serviceCollection.BuildServiceProvider();

            ISimManager simManager = serviceProvider.GetService<ISimManager>();

            await simManager.StartSimulation().ConfigureAwait(false);
            
            await simManager.StopSimulation().ConfigureAwait(false);


            return 0;
        }

        private static async Task ConfigureServices(IServiceCollection services)
        {
            var builder = new ConfigurationBuilder()
                .SetBasePath(Directory.GetCurrentDirectory())
                .AddJsonFile("appsettings.json", optional: false, reloadOnChange: true)
                .AddEnvironmentVariables();

            
            IConfiguration config = builder.Build();


            services.AddOptions();


            services.AddLogging(configure => configure.AddConsole());


            services.Configure<ApplicationSettings>(config.GetSection("applicationSettings"));
            services.Configure<IoTHubSettings>(config.GetSection("iotHubSettings"));


            services.AddSingleton<IDeviceManager, DeviceManager>();
            services.AddSingleton<IGridManager, GridManager>();
            services.AddSingleton<ISimManager, SimManager>();
        }
    }
}
