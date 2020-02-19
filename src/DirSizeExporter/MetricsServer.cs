using DirSizeExporter.Configuration;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;
using Prometheus;
using System.Threading;
using System.Threading.Tasks;

namespace DirSizeExporter
{
    class MetricsServer : IHostedService
    {
        private readonly MetricServer _server;

        public MetricsServer(IOptions<ExporterConfiguration> options)
        {
            var config = options.Value;
            _server = new MetricServer(config.Address, config.Port);
        }

        public Task StartAsync(CancellationToken cancellationToken)
        {
            new Thread(Main).Start();
            return Task.CompletedTask;
        }

        private void Main()
        {
            _server.Start();
        }

        public async Task StopAsync(CancellationToken cancellationToken)
        {
            await _server.StopAsync();
        }
    }
}
