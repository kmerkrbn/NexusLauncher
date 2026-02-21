using System.Text.Json.Serialization;

namespace MClauncher.Models
{
    public class LauncherUpdateInfo
    {
        [JsonPropertyName("version")]
        public string Version { get; set; } = "";

        [JsonPropertyName("downloadUrl")]
        public string DownloadUrl { get; set; } = "";

        [JsonPropertyName("websiteUrl")]
        public string WebsiteUrl { get; set; } = "";

        [JsonPropertyName("changelog")]
        public string Changelog { get; set; } = "";

        [JsonPropertyName("mandatory")]
        public bool Mandatory { get; set; } = false;

        [JsonPropertyName("message")]
        public string Message { get; set; } = "";
    }

    public class UpdateCheckResult
    {
        public bool Success { get; set; }
        public bool IsUpdateAvailable { get; set; }
        public string ErrorMessage { get; set; } = "";
        public LauncherUpdateInfo? UpdateInfo { get; set; }
    }
}
