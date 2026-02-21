namespace MClauncher.Models
{
    public class ModrinthMod
    {
        public string Id { get; set; } = string.Empty;
        public string Slug { get; set; } = string.Empty;
        public string Title { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public string? IconUrl { get; set; }
        public string Author { get; set; } = string.Empty;
        public int Downloads { get; set; }
        public double Rating { get; set; }
        public List<string> Categories { get; set; } = new();
        public List<string> GameVersions { get; set; } = new();
        public string LatestVersion { get; set; } = string.Empty;
        public DateTime CreatedAt { get; set; }
    }

    public class ModVersion
    {
        public string Id { get; set; } = string.Empty;
        public string VersionNumber { get; set; } = string.Empty;
        public string GameVersion { get; set; } = string.Empty;
        public List<ModFile> Files { get; set; } = new();
        public DateTime DatePublished { get; set; }
    }

    public class ModFile
    {
        public string Filename { get; set; } = string.Empty;
        public string Url { get; set; } = string.Empty;
        public long Size { get; set; }
        public bool Primary { get; set; }
    }

    public class InstalledMod
    {
        public string Id { get; set; } = string.Empty;
        public string Title { get; set; } = string.Empty;
        public string? Version { get; set; }
        public string GameVersion { get; set; } = string.Empty;
        public string FilePath { get; set; } = string.Empty;
        public long FileSize { get; set; }
        public DateTime InstalledDate { get; set; }
        public bool IsEnabled { get; set; } = true;
    }
}
