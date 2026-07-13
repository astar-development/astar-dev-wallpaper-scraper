namespace AStar.Dev.Wallpaper.Scraper.Configuration;

public class Serilog
    {
        public string[] Using { get; set; } = [];

        public MinimumLevel MinimumLevel { get; set; } = new();

        public WriteTo[] WriteTo { get; set; } = [];

        public string[] Enrich { get; set; } = [];

        public Properties Properties { get; set; } = new();
    }

