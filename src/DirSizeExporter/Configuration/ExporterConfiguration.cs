using System.Collections.Generic;

namespace DirSizeExporter.Configuration
{
    class ExporterConfiguration
    {
        public string Address { get; set; } = "+";
        public int Port { get; set; } = 8080;
        public int IntervalSeconds { get; set; }
        public IList<DirectoryConfiguration> Directories { get; set; }
    }
}
