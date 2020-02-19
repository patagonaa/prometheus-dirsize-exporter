using DirSizeExporter.Configuration;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System.Threading;
using System.Threading.Tasks;
using Timer = System.Timers.Timer;
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

        private static readonly Gauge _directorySize = Metrics.CreateGauge("dirsize_path_bytes", "size of all files in the directory", "dirname", "basedir", "dirshortname");
        private static readonly Gauge _directoryFileCount = Metrics.CreateGauge("dirsize_path_count", "number of files in the directory", "dirname", "basedir", "dirshortname");
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
            _timer.Elapsed += (sender, e) => Scrape();
        }


        public Task StartAsync(CancellationToken cancellationToken)
        {
            _timer.Start();
            Scrape();
            return Task.CompletedTask;
        }

        private void Scrape()
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
                        default:
                            break;
                    }
                }
            }
            _logger.LogInformation("Scrape done.");
        }

        private void ScrapeTopDirectory(DirectoryConfiguration directoryConfig)
        {
            var (size, count) = GetDirectoryInfo(directoryConfig.Path);
            _directorySize.WithLabels(GetLabels(directoryConfig.Path, DirectoryScrapeType.TopDirectory)).Set(size);
            _directoryFileCount.WithLabels(GetLabels(directoryConfig.Path, DirectoryScrapeType.TopDirectory)).Set(count);
            _logger.LogInformation("Scraped {FileCount} files in {DirectoryPath}", count, directoryConfig.Path);
        }

        private void ScrapeSubDirectories(DirectoryConfiguration directoryConfig)
        {
            foreach (var directory in Directory.EnumerateDirectories(directoryConfig.Path))
            {
                var (size, count) = GetDirectoryInfo(directory);
                _directorySize.WithLabels(GetLabels(directory, DirectoryScrapeType.SubDirectories)).Set(size);
                _directoryFileCount.WithLabels(GetLabels(directory, DirectoryScrapeType.SubDirectories)).Set(count);
                _logger.LogInformation("Scraped {FileCount} files in {DirectoryPath}", count, directory);
            }
        }

        private string[] GetLabels(string path, DirectoryScrapeType scrapeType)
        {
            var directory = new DirectoryInfo(path);
            string baseDir;
            switch (scrapeType)
            {
                case DirectoryScrapeType.TopDirectory:
                    baseDir = directory.FullName;
                    break;
                case DirectoryScrapeType.SubDirectories:
                    baseDir = directory.Parent?.FullName ?? directory.FullName;
                    break;
                default:
                    throw new ArgumentException("Invalid ScrapeType");
            }

            return new string[] { directory.FullName, baseDir, directory.Name };
        }

        private (long size, long count) GetDirectoryInfo(string directory)
        {
            long size = 0;
            long count = 0;
            foreach (var file in Directory.EnumerateFiles(directory, "*", SearchOption.AllDirectories))
            {
                try
                {
                    size += new FileInfo(file).Length;
                    count++;
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Scrape failed for file {FilePath}", file);
                }
            }

            return (size, count);
        }

        public Task StopAsync(CancellationToken cancellationToken)
        {
            _timer.Stop();
            return Task.CompletedTask;
        }
    }
}
