using DirSizeExporter.Configuration;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace DirSizeExporter
{
    class Program
    {
        static void Main(string[] args)
        {
            var host = new HostBuilder()
                .ConfigureAppConfiguration(ConfigureAppConfiguration)
                .ConfigureServices(ConfigureServices)
                .ConfigureLogging(ConfigureLogging)
                .Build();
            using (host)
            {
                host.Run();
            }
        }

        private static void ConfigureAppConfiguration(HostBuilderContext hostContext, IConfigurationBuilder configurationBuilder)
        {
            configurationBuilder
                .AddJsonFile("configs/appSettings.json")
                .AddEnvironmentVariables();
        }

        private static void ConfigureLogging(HostBuilderContext hostContext, ILoggingBuilder loggingBuilder)
        {
            loggingBuilder.AddConsole(c => c.IncludeScopes = false);
        }

        private static void ConfigureServices(HostBuilderContext hostContext, IServiceCollection services)
        {
            services.Configure<ExporterConfiguration>(hostContext.Configuration);

            services.AddHostedService<DirectoryScraper>();
            services.AddHostedService<MetricsServer>();
        }
    }
}
