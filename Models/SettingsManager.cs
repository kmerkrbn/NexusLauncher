using System;
using System.IO;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace MClauncher.Models
{
    public class LauncherSettings
    {
        [JsonPropertyName("showSnapshots")]
        public bool ShowSnapshots { get; set; } = false;

        [JsonPropertyName("ramAllocation")]
        public int RamAllocation { get; set; } = 2048;

        [JsonPropertyName("javaArguments")]
        public string JavaArguments { get; set; } = "";

        [JsonPropertyName("rememberMe")]
        public bool RememberMe { get; set; } = false;

        [JsonPropertyName("rememberedOfflineUsername")]
        public string RememberedOfflineUsername { get; set; } = "";

        [JsonPropertyName("lastLoginUsername")]
        public string LastLoginUsername { get; set; } = "";

        [JsonPropertyName("lastLoginMethod")]
        public string LastLoginMethod { get; set; } = "";

        [JsonPropertyName("autoCheckUpdates")]
        public bool AutoCheckUpdates { get; set; } = true;

        [JsonPropertyName("updateFeedUrl")]
        public string UpdateFeedUrl { get; set; } = "https://example.com/nexus-launcher/update.json";
    }

    public static class SettingsManager
    {
        private static readonly string SettingsDirectory = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
            ".mclauncher"
        );

        private static readonly string SettingsPath = Path.Combine(SettingsDirectory, "settings.json");

        private static readonly JsonSerializerOptions JsonOptions = new JsonSerializerOptions
        {
            WriteIndented = true,
            DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
        };

        public static LauncherSettings Current { get; private set; } = new LauncherSettings();

        static SettingsManager()
        {
            Load();
        }

        public static void Load()
        {
            try
            {
                if (File.Exists(SettingsPath))
                {
                    string json = File.ReadAllText(SettingsPath);
                    var settings = JsonSerializer.Deserialize<LauncherSettings>(json, JsonOptions);
                    Current = settings ?? new LauncherSettings();
                }
                else
                {
                    Current = new LauncherSettings();
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Failed to load settings: {ex.Message}");
                Current = new LauncherSettings();
            }
        }

        public static void Save()
        {
            try
            {
                if (!Directory.Exists(SettingsDirectory))
                {
                    Directory.CreateDirectory(SettingsDirectory);
                }

                string json = JsonSerializer.Serialize(Current, JsonOptions);
                File.WriteAllText(SettingsPath, json);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Failed to save settings: {ex.Message}");
            }
        }

        public static void ResetToDefaults()
        {
            Current = new LauncherSettings();
            Save();
        }
    }
}
