using System;
using System.Linq;
using System.Text.Json.Serialization;

namespace Amuse.App
{
    public sealed record AppUpdate
    {
        private readonly AppVersion _version;
        private readonly AppAsset _assetInstaller;
        private readonly AppAsset _assetStandalone;

        public AppUpdate(AppVersion version)
        {
            _version = version;
            _assetInstaller = version.Assets?.FirstOrDefault(x => x.DownloadLink.EndsWith(".exe"));
            _assetStandalone = version.Assets?.FirstOrDefault(x => x.DownloadLink.EndsWith(".zip"));
        }

        public string Link => _version.Link;
        public string Version => _version.Version;
        public AppAsset AssetInstaller => _assetInstaller;
        public AppAsset AssetStandalone => _assetStandalone;
    }

    public sealed record AppVersion
    {
        [JsonPropertyName("tag_name")]
        public string Version { get; set; }

        [JsonPropertyName("html_url")]
        public string Link { get; set; }

        [JsonPropertyName("assets")]
        public AppAsset[] Assets { get; set; }
    }

    public sealed record AppAsset
    {
        [JsonPropertyName("name")]
        public string Name { get; set; }

        [JsonPropertyName("size")]
        public long Size { get; set; }

        public double DownloadSize => Size == 0 ? 0 : Size / Math.Pow(1024, 3);

        [JsonPropertyName("created_at")]
        public DateTime Created { get; set; }

        [JsonPropertyName("browser_download_url")]
        public string DownloadLink { get; set; }
    }
}
