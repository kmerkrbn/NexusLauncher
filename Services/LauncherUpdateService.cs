using System;
using System.Net.Http;
using System.Reflection;
using System.Text.Json;
using System.Threading.Tasks;
using MClauncher.Models;

namespace MClauncher.Services
{
    public static class LauncherUpdateService
    {
        private static readonly HttpClient _httpClient = new()
        {
            Timeout = TimeSpan.FromSeconds(12)
        };

        public static async Task<UpdateCheckResult> CheckForUpdatesAsync(string updateFeedUrl)
        {
            if (string.IsNullOrWhiteSpace(updateFeedUrl))
            {
                return new UpdateCheckResult
                {
                    Success = false,
                    ErrorMessage = "Update feed URL is empty."
                };
            }

            try
            {
                using var req = new HttpRequestMessage(HttpMethod.Get, updateFeedUrl);
                req.Headers.UserAgent.ParseAdd("NexusLauncher/1.0");

                using var response = await _httpClient.SendAsync(req);
                if (!response.IsSuccessStatusCode)
                {
                    return new UpdateCheckResult
                    {
                        Success = false,
                        ErrorMessage = $"Update server returned {(int)response.StatusCode}."
                    };
                }

                var json = await response.Content.ReadAsStringAsync();
                var updateInfo = JsonSerializer.Deserialize<LauncherUpdateInfo>(json, new JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true
                });

                if (updateInfo == null || string.IsNullOrWhiteSpace(updateInfo.Version))
                {
                    return new UpdateCheckResult
                    {
                        Success = false,
                        ErrorMessage = "Update feed is invalid."
                    };
                }

                var currentVersion = GetCurrentLauncherVersion();
                var isUpdateAvailable = IsRemoteVersionNewer(currentVersion, updateInfo.Version);

                return new UpdateCheckResult
                {
                    Success = true,
                    IsUpdateAvailable = isUpdateAvailable,
                    UpdateInfo = updateInfo
                };
            }
            catch (Exception ex)
            {
                return new UpdateCheckResult
                {
                    Success = false,
                    ErrorMessage = ex.Message
                };
            }
        }

        public static string GetCurrentLauncherVersion()
        {
            try
            {
                var asmVersion = Assembly.GetExecutingAssembly().GetName().Version;
                if (asmVersion == null || asmVersion.Major < 0 || asmVersion.Minor < 0)
                    return "1.0.3";

                var build = asmVersion.Build >= 0 ? asmVersion.Build : 0;
                return $"{asmVersion.Major}.{asmVersion.Minor}.{build}";
            }
            catch
            {
                return "1.0.3";
            }
        }

        private static bool IsRemoteVersionNewer(string current, string remote)
        {
            var currentVersion = NormalizeVersion(current);
            var remoteVersion = NormalizeVersion(remote);
            return remoteVersion > currentVersion;
        }

        private static Version NormalizeVersion(string value)
        {
            if (string.IsNullOrWhiteSpace(value))
                return new Version(0, 0, 0);

            var v = value.Trim();
            var cut = v.IndexOfAny(new[] { '-', '+', ' ' });
            if (cut > 0)
                v = v[..cut];

            if (Version.TryParse(v, out var parsed))
                return parsed;

            var parts = v.Split('.', StringSplitOptions.RemoveEmptyEntries);
            var major = parts.Length > 0 && int.TryParse(parts[0], out var ma) ? ma : 0;
            var minor = parts.Length > 1 && int.TryParse(parts[1], out var mi) ? mi : 0;
            var build = parts.Length > 2 && int.TryParse(parts[2], out var bu) ? bu : 0;
            return new Version(major, minor, build);
        }
    }
}
