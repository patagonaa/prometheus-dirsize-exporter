# prometheus-dirsize-exporter
Prometheus exporter for getting the directory size of a directory or its subdirectories.
This can be useful for directories like `/var/lib/docker/volumes` to see which volumes take up the most space.

## Configuration
The exporter is configured through the file `configs/appSettings.json` under the path of the binary (`/app/configs/appSettings.json` in the docker container).

Full configuration example:
```javascript
{
    "Address": "+" // the address to bind the web server to (default "+" -> bind to all interfaces)
    "Port": 8080 // port to bind to (default: 8080)
    "IntervalSeconds": 60, // interval in seconds in which to crawl the directories
    "Directories": [ // can be one or multiple directories
        {
            "ScrapeType": "TopDirectory", // can be "TopDirectory" to expose only one metric or "SubDirectories" to expose one metric per direct subdirectory (not recursive)
            "Path": "/home/patagona"
        },
        {
            "ScrapeType": "SubDirectories",
            "Path": "/var/lib/docker/volumes"
        }
    ]
}
```

## Usage with docker / docker-compose

This expects the appSettings.json file and the cloned git repo in the same directory as the compose file. This composefile mounts the entire `/var/lib/docker/volumes` path into the container (readonly). If you want to crawl different paths, you can add them in the compose file.

docker-compose.yml:
```
version: "3"
  dirsize:
    restart: unless-stopped
    image: prometheus-dirsize-exporter:latest
    build: ./prometheus-dirsize-exporter
    expose:
      - "8080"
    volumes:
      - "./appSettings.json:/app/configs/appSettings.json"
      - "/var/lib/docker/volumes:/mnt/docker-volumes:ro"
```

appSettings.json:
```json
{
    "IntervalSeconds": 60,
    "Directories": [
        {
            "ScrapeType": "SubDirectories",
            "Path": "/mnt/docker-volumes"
        }
    ]
}
```

## Example Output

```
# HELP dirsize_path_bytes size of all files in the directory
# TYPE dirsize_path_bytes gauge
dirsize_path_bytes{dirname="/mnt/docker-volumes/grafana_grafana-data",basedir="/mnt/docker-volumes",dirshortname="grafana_grafana-data"} 7861729
dirsize_path_bytes{dirname="/mnt/docker-volumes/nextcloud_db",basedir="/mnt/docker-volumes",dirshortname="nextcloud_db"} 236783452
dirsize_path_bytes{dirname="/mnt/docker-volumes/nextcloud_nextcloud",basedir="/mnt/docker-volumes",dirshortname="nextcloud_nextcloud"} 262569295568
dirsize_path_bytes{dirname="/mnt/docker-volumes/influxdb_influxdb-data",basedir="/mnt/docker-volumes",dirshortname="influxdb_influxdb-data"} 5472619156
# HELP dirsize_path_count number of files in the directory
# TYPE dirsize_path_count gauge
dirsize_path_count{dirname="/mnt/docker-volumes/grafana_grafana-data",basedir="/mnt/docker-volumes",dirshortname="grafana_grafana-data"} 304
dirsize_path_count{dirname="/mnt/docker-volumes/nextcloud_db",basedir="/mnt/docker-volumes",dirshortname="nextcloud_db"} 236
dirsize_path_count{dirname="/mnt/docker-volumes/nextcloud_nextcloud",basedir="/mnt/docker-volumes",dirshortname="nextcloud_nextcloud"} 32007
dirsize_path_count{dirname="/mnt/docker-volumes/influxdb_influxdb-data",basedir="/mnt/docker-volumes",dirshortname="influxdb_influxdb-data"} 219
# HELP dirsize_scrape_time_ms Scrape duration
# TYPE dirsize_scrape_time_ms histogram
dirsize_scrape_time_ms_sum 1348.1112561000027
dirsize_scrape_time_ms_count 3731
dirsize_scrape_time_ms_bucket{le="0.1"} 0
dirsize_scrape_time_ms_bucket{le="1"} 3725
dirsize_scrape_time_ms_bucket{le="5"} 3731
dirsize_scrape_time_ms_bucket{le="10"} 3731
dirsize_scrape_time_ms_bucket{le="30"} 3731
dirsize_scrape_time_ms_bucket{le="60"} 3731
dirsize_scrape_time_ms_bucket{le="120"} 3731
dirsize_scrape_time_ms_bucket{le="300"} 3731
dirsize_scrape_time_ms_bucket{le="600"} 3731
dirsize_scrape_time_ms_bucket{le="+Inf"} 3731
```