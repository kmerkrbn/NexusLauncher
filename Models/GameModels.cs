using System;
using System.Collections.Generic;
using System.IO;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace MClauncher.Models
{
    public enum ModLoader
    {
        Vanilla,
        Fabric,
        Forge,
        Quilt,
        NeoForge
    }

    public class ModProfile
    {
        public string Id { get; set; } = Guid.NewGuid().ToString();
        public string Name { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public string GameVersion { get; set; } = "1.21";
        public ModLoader Loader { get; set; } = ModLoader.Vanilla;
        public List<InstalledModEntry> InstalledMods { get; set; } = new();
        public DateTime CreatedDate { get; set; } = DateTime.Now;
        public DateTime LastPlayed { get; set; }
        public int PlayCount { get; set; }
        public bool IsActive { get; set; }
        public string Icon { get; set; } = "🎮"; // emoji icon
        public bool IsModpackProfile { get; set; }
        public string ModpackProjectId { get; set; } = string.Empty;
        public string ModpackVersionId { get; set; } = string.Empty;
        public string ModpackFilePath { get; set; } = string.Empty;
    }

    public class InstalledModEntry
    {
        public string ModId { get; set; } = string.Empty;
        public string Title { get; set; } = string.Empty;
        public string FileName { get; set; } = string.Empty;
        public string Version { get; set; } = string.Empty;
        public DateTime InstalledDate { get; set; } = DateTime.Now;
        public bool IsEnabled { get; set; } = true;
    }

    public static class ProfileManager
    {
        private static readonly string ProfilesDirectory = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
            ".mclauncher", "profiles"
        );
        private static readonly string ProfilesPath = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
            ".mclauncher", "profiles.json"
        );
        private static readonly JsonSerializerOptions JsonOptions = new() { WriteIndented = true };

        private static List<ModProfile>? _profiles;

        public static List<ModProfile> GetProfiles()
        {
            if (_profiles != null) return _profiles;
            try
            {
                if (File.Exists(ProfilesPath))
                {
                    var json = File.ReadAllText(ProfilesPath);
                    _profiles = JsonSerializer.Deserialize<List<ModProfile>>(json, JsonOptions) ?? new();
                }
                else
                {
                    _profiles = new();
                }
            }
            catch
            {
                _profiles = new();
            }
            return _profiles;
        }

        public static void SaveProfiles()
        {
            try
            {
                if (!Directory.Exists(Path.GetDirectoryName(ProfilesPath)))
                    Directory.CreateDirectory(Path.GetDirectoryName(ProfilesPath)!);
                File.WriteAllText(ProfilesPath, JsonSerializer.Serialize(_profiles ?? new(), JsonOptions));
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Failed to save profiles: {ex.Message}");
            }
        }

        public static ModProfile CreateProfile(string name, string gameVersion, ModLoader loader, string description = "")
        {
            var profile = new ModProfile
            {
                Name = name,
                GameVersion = gameVersion,
                Loader = loader,
                Description = description,
                CreatedDate = DateTime.Now
            };
            GetProfiles().Add(profile);
            SaveProfiles();
            return profile;
        }

        public static void DeleteProfile(string id)
        {
            _profiles?.RemoveAll(p => p.Id == id);
            SaveProfiles();
        }

        public static ModProfile RegisterDownloadedModpackProfile(
            string projectId,
            string versionId,
            string title,
            string gameVersion,
            string filePath,
            ModLoader loader)
        {
            var profiles = GetProfiles();
            var existing = profiles.Find(p =>
                p.IsModpackProfile &&
                string.Equals(p.ModpackProjectId, projectId, StringComparison.OrdinalIgnoreCase));

            if (existing != null)
            {
                existing.Name = title;
                existing.GameVersion = gameVersion;
                existing.Loader = loader;
                existing.ModpackVersionId = versionId;
                existing.ModpackFilePath = filePath;
                existing.LastPlayed = DateTime.MinValue;
                SaveProfiles();
                return existing;
            }

            var profile = new ModProfile
            {
                Name = title,
                Description = "Imported from Modpacks",
                GameVersion = gameVersion,
                Loader = loader,
                Icon = "📦",
                IsModpackProfile = true,
                ModpackProjectId = projectId,
                ModpackVersionId = versionId,
                ModpackFilePath = filePath,
                CreatedDate = DateTime.Now
            };

            profiles.Add(profile);
            SaveProfiles();
            return profile;
        }

        public static string GetModsFolder(ModProfile profile)
        {
            var folder = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
                ".mclauncher", "profiles", profile.Id, "mods"
            );
            if (!Directory.Exists(folder)) Directory.CreateDirectory(folder);
            return folder;
        }
    }

    public class GameLaunchConfig
    {
        public string ProfileId { get; set; } = string.Empty;
        public string GameVersion { get; set; } = "1.21";
        public string JavaPath { get; set; } = string.Empty;
        public int MaxMemory { get; set; } = 4096;
        public int MinMemory { get; set; } = 1024;
        public List<string> EnabledMods { get; set; } = new();
    }

    public class ModDependency
    {
        public string ModId { get; set; } = string.Empty;
        public string RequiredVersion { get; set; } = string.Empty;
    }
}
