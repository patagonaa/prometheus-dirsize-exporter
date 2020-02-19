using DirSizeExporter.Configuration;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System.Threading;
using System.Threading.Tasks;
using Timer = System.Timers.Timer;
using ElapsedEventArgs = System.Timers.ElapsedEventArgs;
using Prometheus;
using System.IO;
using System;

namespace DirSizeExporter
{
    class DirectoryScraper : IHostedService
    {
        private readonly ExporterConfiguration _config;
        private readonly ILogger<DirectoryScraper> _logger;
        private readonly Timer _timer;

        private const int _maxRecursion = 50;

        private static readonly Gauge _directorySize = Metrics.CreateGauge("dirsize_path_bytes", "Number of bytes of all files in the directory", "dirname");
        private static readonly Histogram _scrapeTime = Metrics.CreateHistogram("dirsize_scrape_time_ms", "Scrape duration", new HistogramConfiguration
        {
            Buckets = new double[] { 0.1, 1, 5, 10, 30, 60, 120, 300, 600 }
        });

        public DirectoryScraper(ILogger<DirectoryScraper> logger, IOptions<ExporterConfiguration> options)
        {
            _config = options.Value;
            _logger = logger;
            _timer = new Timer
            {
                Interval = _config.IntervalSeconds * 1000,
                AutoReset = true
            };
            _timer.Elapsed += Scrape;
        }


        public Task StartAsync(CancellationToken cancellationToken)
        {
            _timer.Start();
            return Task.CompletedTask;
        }

        private void Scrape(object sender, ElapsedEventArgs e)
        {
            _logger.LogInformation("Scraping Directories.");

            using (_scrapeTime.NewTimer())
            {
                foreach (var directoryConfig in _config.Directories)
                {
                    _logger.LogInformation("Scraping Directory {DirectoryPath}", directoryConfig.Path);

                    switch (directoryConfig.ScrapeType)
                    {
                        case DirectoryScrapeType.TopDirectory:
                            ScrapeTopDirectory(directoryConfig);
                            break;
                        case DirectoryScrapeType.SubDirectories:
                            ScrapeSubDirectories(directoryConfig);
                            break;
                        case DirectoryScrapeType.Recursive:
                            throw new NotImplementedException();
                        default:
                            break;
                    }
                }
            }
            _logger.LogInformation("Scrape done.");
        }

        private void ScrapeTopDirectory(DirectoryConfiguration directoryConfig)
        {
            var size = GetDirectorySize(directoryConfig.Path, _maxRecursion);
            _directorySize.WithLabels(directoryConfig.Path).Set(size);
        }

        private void ScrapeSubDirectories(DirectoryConfiguration directoryConfig)
        {
            foreach (var directory in Directory.EnumerateDirectories(directoryConfig.Path))
            {
                var size = GetDirectorySize(directory, _maxRecursion);
                _directorySize.WithLabels(directory).Set(size);
            }
        }

        private long GetDirectorySize(string directory, int remainingRecursion)
        {
            if(remainingRecursion <= 0)
            {
                _logger.LogWarning("Directory {DirectoryPath} is too deep", directory);
                return 0;
            }

            long size = 0;
            foreach (var file in Directory.EnumerateFiles(directory))
            {
                try
                {
                    size += new FileInfo(file).Length;
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Scrape failed for file {FilePath}", file);
                }
            }
            foreach (var subDirectory in Directory.EnumerateDirectories(directory))
            {
                try
                {
                    size += GetDirectorySize(subDirectory, remainingRecursion - 1);
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Scrape failed for directory {DirectoryPath}", subDirectory);
                }
            }

            return size;
        }

        public Task StopAsync(CancellationToken cancellationToken)
        {
            _timer.Stop();
            return Task.CompletedTask;
        }
    }
}
