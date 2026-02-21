using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading.Tasks;
using MClauncher.Models;

namespace MClauncher.Services
{
    public interface IModrinthService
    {
        Task<List<ModrinthMod>> SearchModsAsync(string query, string gameVersion, int limit = 20, string category = "");
        Task<List<ModrinthMod>> SearchModpacksAsync(string query, string gameVersion, int limit = 20, string category = "");
        Task<ModrinthMod> GetModDetailsAsync(string modId);
        Task<List<ModVersion>> GetModVersionsAsync(string modId, string gameVersion);
        Task<bool> DownloadModAsync(string fileUrl, string savePath, IProgress<(long, long)> progress);
        Task<List<InstalledMod>> GetInstalledModsAsync();
        Task<bool> InstallModAsync(ModFile file, string gameVersion);
        Task<bool> UninstallModAsync(string modId);
    }

    public class ModrinthService : IModrinthService
    {
        private readonly HttpClient _httpClient;
        private readonly string _modsFolder;
        private const string BaseUrl = "https://api.modrinth.com/v2";

        public ModrinthService()
        {
            _httpClient = new HttpClient
            {
                Timeout = TimeSpan.FromSeconds(30)
            };
            _httpClient.DefaultRequestHeaders.Add("User-Agent", "NexusLauncher/1.0");

            _modsFolder = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), 
                "NexusLauncher", "mods");

            if (!Directory.Exists(_modsFolder))
                Directory.CreateDirectory(_modsFolder);
        }

        public async Task<List<ModrinthMod>> SearchModpacksAsync(string query, string gameVersion, int limit = 20, string category = "")
        {
            try
            {
                var facets = new List<string> { "[\"project_type:modpack\"]" };
                if (!string.IsNullOrEmpty(gameVersion))
                    facets.Add($"[\"versions:{gameVersion}\"]");
                if (!string.IsNullOrEmpty(category))
                    facets.Add($"[\"categories:{category}\"]");

                var facetStr = $"[{string.Join(",", facets)}]";
                var url = $"{BaseUrl}/search?query={Uri.EscapeDataString(query)}&limit={limit}&facets={Uri.EscapeDataString(facetStr)}&index=downloads";

                var response = await _httpClient.GetAsync(url);
                if (!response.IsSuccessStatusCode) return new List<ModrinthMod>();

                var json = await response.Content.ReadAsStringAsync();
                var doc = JsonDocument.Parse(json);
                var hits = doc.RootElement.GetProperty("hits").EnumerateArray();

                var packs = new List<ModrinthMod>();
                foreach (var hit in hits)
                {
                    try
                    {
                        var pack = new ModrinthMod
                        {
                            Id = hit.TryGetProperty("project_id", out var id) ? (id.GetString() ?? "unknown") : "unknown",
                            Slug = hit.TryGetProperty("slug", out var slug) ? (slug.GetString() ?? "unknown") : "unknown",
                            Title = hit.TryGetProperty("title", out var title) ? (title.GetString() ?? "Unknown") : "Unknown",
                            Description = hit.TryGetProperty("description", out var desc) ? (desc.GetString() ?? "") : "",
                            IconUrl = hit.TryGetProperty("icon_url", out var icon) && icon.ValueKind == JsonValueKind.String ? icon.GetString() : null,
                            Author = hit.TryGetProperty("author", out var author) ? (author.GetString() ?? "Unknown") : "Unknown",
                            Downloads = hit.TryGetProperty("downloads", out var dl) ? dl.GetInt32() : 0,
                            CreatedAt = hit.TryGetProperty("date_created", out var created) && created.GetString() != null
                                ? DateTime.Parse(created.GetString()!) : DateTime.Now
                        };
                        if (hit.TryGetProperty("categories", out var cats) && cats.ValueKind == JsonValueKind.Array)
                            foreach (var c in cats.EnumerateArray())
                                if (c.GetString() is string catStr) pack.Categories.Add(catStr);
                        if (hit.TryGetProperty("versions", out var gvs) && gvs.ValueKind == JsonValueKind.Array)
                            foreach (var v in gvs.EnumerateArray())
                                if (v.GetString() is string vStr) pack.GameVersions.Add(vStr);
                        packs.Add(pack);
                    }
                    catch { continue; }
                }
                return packs;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"SearchModpacksAsync error: {ex.Message}");
                return new List<ModrinthMod>();
            }
        }

        public async Task<List<ModrinthMod>> SearchModsAsync(string query, string gameVersion, int limit = 20, string category = "")
        {
            try
            {
                // Build Modrinth facets  [["project_type:mod"],["categories:sodium"],["versions:1.21"]]
                var facets = new List<string> { "[\"project_type:mod\"]" };
                if (!string.IsNullOrEmpty(gameVersion))
                    facets.Add($"[\"versions:{gameVersion}\"]");
                if (!string.IsNullOrEmpty(category))
                    facets.Add($"[\"categories:{category}\"]");

                // Build URL with facets
                var facetStr = $"[{string.Join(",", facets)}]";
                var url = $"{BaseUrl}/search?query={Uri.EscapeDataString(query)}&limit={limit}&facets={Uri.EscapeDataString(facetStr)}&index=relevance";

                System.Diagnostics.Debug.WriteLine($"Searching: {url}");

                var response = await _httpClient.GetAsync(url);

                if (!response.IsSuccessStatusCode)
                {
                    System.Diagnostics.Debug.WriteLine($"API Error: {response.StatusCode}");
                    return new List<ModrinthMod>();
                }

                var jsonString = await response.Content.ReadAsStringAsync();
                var jsonDocument = JsonDocument.Parse(jsonString);
                var hits = jsonDocument.RootElement.GetProperty("hits").EnumerateArray();

                var mods = new List<ModrinthMod>();
                foreach (var hit in hits)
                {
                    try
                    {
                        var mod = new ModrinthMod
                        {
                            Id = hit.TryGetProperty("project_id", out var id) ? (id.GetString() ?? "unknown") : "unknown",
                            Slug = hit.TryGetProperty("slug", out var slug) ? (slug.GetString() ?? "unknown") : "unknown",
                            Title = hit.TryGetProperty("title", out var title) ? (title.GetString() ?? "Unknown") : "Unknown",
                            Description = hit.TryGetProperty("description", out var desc) ? (desc.GetString() ?? "No description") : "No description",
                            IconUrl = hit.TryGetProperty("icon_url", out var icon) && icon.ValueKind == JsonValueKind.String ? icon.GetString() : null,
                            Author = hit.TryGetProperty("author", out var author) ? (author.GetString() ?? "Unknown") : "Unknown",
                            Downloads = hit.TryGetProperty("downloads", out var downloads) ? downloads.GetInt32() : 0,
                            Rating = 0.0,
                            CreatedAt = hit.TryGetProperty("date_created", out var created) && created.GetString() != null
                                ? DateTime.Parse(created.GetString()!) : DateTime.Now
                        };

                        // Parse categories
                        if (hit.TryGetProperty("categories", out var cats) && cats.ValueKind == JsonValueKind.Array)
                            foreach (var c in cats.EnumerateArray())
                                if (c.GetString() is string catStr) mod.Categories.Add(catStr);

                        // Parse display_categories (cleaner names)
                        if (mod.Categories.Count == 0 && hit.TryGetProperty("display_categories", out var dcats) && dcats.ValueKind == JsonValueKind.Array)
                            foreach (var c in dcats.EnumerateArray())
                                if (c.GetString() is string catStr) mod.Categories.Add(catStr);

                        // Parse game versions
                        if (hit.TryGetProperty("versions", out var gvs) && gvs.ValueKind == JsonValueKind.Array)
                            foreach (var v in gvs.EnumerateArray())
                                if (v.GetString() is string vStr) mod.GameVersions.Add(vStr);

                        // Parse latest_version
                        if (hit.TryGetProperty("latest_version", out var lv) && lv.GetString() is string lvStr)
                            mod.LatestVersion = lvStr;

                        mods.Add(mod);
                    }
                    catch (Exception itemEx)
                    {
                        System.Diagnostics.Debug.WriteLine($"Error parsing mod item: {itemEx.Message}");
                        continue;
                    }
                }

                System.Diagnostics.Debug.WriteLine($"Found {mods.Count} mods");
                return mods;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Search error: {ex.Message}\n{ex.StackTrace}");
                return new List<ModrinthMod>();
            }
        }

        public async Task<ModrinthMod> GetModDetailsAsync(string modId)
        {
            try
            {
                var url = $"{BaseUrl}/project/{modId}";
                var response = await _httpClient.GetAsync(url);
                response.EnsureSuccessStatusCode();

                var jsonString = await response.Content.ReadAsStringAsync();
                var doc = JsonDocument.Parse(jsonString);
                var root = doc.RootElement;

                return new ModrinthMod
                {
                    Id = root.TryGetProperty("id", out var id) ? (id.GetString() ?? string.Empty) : string.Empty,
                    Slug = root.TryGetProperty("slug", out var slug) ? (slug.GetString() ?? string.Empty) : string.Empty,
                    Title = root.TryGetProperty("title", out var title) ? (title.GetString() ?? "Unknown") : "Unknown",
                    Description = root.TryGetProperty("description", out var description) ? (description.GetString() ?? string.Empty) : string.Empty,
                    IconUrl = root.TryGetProperty("icon_url", out var icon) ? icon.GetString() : null,
                    Author = "Unknown",
                    Downloads = root.TryGetProperty("downloads", out var downloads) ? downloads.GetInt32() : 0,
                    Rating = root.TryGetProperty("rating", out var rating) ? rating.GetDouble() : 0
                };
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Get mod details error: {ex.Message}");
                throw;
            }
        }

        public async Task<List<ModVersion>> GetModVersionsAsync(string modId, string gameVersion)
        {
            try
            {
                var url = $"{BaseUrl}/project/{modId}/version";
                var response = await _httpClient.GetAsync(url);
                response.EnsureSuccessStatusCode();

                var jsonString = await response.Content.ReadAsStringAsync();
                using var doc = JsonDocument.Parse(jsonString);

                var requestedVersion = (gameVersion ?? string.Empty).Trim();
                var allParsed = new List<(List<string> GameVersions, ModVersion Version)>();

                foreach (var versionElement in doc.RootElement.EnumerateArray())
                {
                    try
                    {
                        var gameVersionList = new List<string>();
                        if (versionElement.TryGetProperty("game_versions", out var gv) && gv.ValueKind == JsonValueKind.Array)
                        {
                            gameVersionList = gv
                                .EnumerateArray()
                                .Select(v => v.GetString())
                                .Where(v => !string.IsNullOrWhiteSpace(v))
                                .Select(v => v!.Trim())
                                .Distinct(StringComparer.OrdinalIgnoreCase)
                                .ToList();
                        }

                        var mv = new ModVersion
                        {
                            Id = versionElement.TryGetProperty("id", out var id) ? (id.GetString() ?? string.Empty) : string.Empty,
                            VersionNumber = versionElement.TryGetProperty("version_number", out var vn) ? (vn.GetString() ?? string.Empty) : string.Empty,
                            GameVersion = gameVersionList.FirstOrDefault() ?? requestedVersion,
                            DatePublished = versionElement.TryGetProperty("date_published", out var dp) && dp.GetString() != null
                                ? DateTime.Parse(dp.GetString()!)
                                : DateTime.MinValue
                        };

                        if (versionElement.TryGetProperty("files", out var filesEl) && filesEl.ValueKind == JsonValueKind.Array)
                        {
                            foreach (var f in filesEl.EnumerateArray())
                            {
                                try
                                {
                                    var filename = f.TryGetProperty("filename", out var fn) ? (fn.GetString() ?? string.Empty) : string.Empty;
                                    var fileUrl = f.TryGetProperty("url", out var fu) ? (fu.GetString() ?? string.Empty) : string.Empty;

                                    if (string.IsNullOrWhiteSpace(filename) || string.IsNullOrWhiteSpace(fileUrl))
                                        continue;

                                    mv.Files.Add(new ModFile
                                    {
                                        Filename = filename,
                                        Url = fileUrl,
                                        Size = f.TryGetProperty("size", out var fs) ? fs.GetInt64() : 0,
                                        Primary = f.TryGetProperty("primary", out var fp) && fp.GetBoolean()
                                    });
                                }
                                catch
                                {
                                    continue;
                                }
                            }
                        }

                        if (mv.Files.Count == 0)
                            continue;

                        allParsed.Add((gameVersionList, mv));
                    }
                    catch
                    {
                        continue;
                    }
                }

                var orderedAll = allParsed
                    .Select(x => x.Version)
                    .OrderByDescending(v => v.DatePublished)
                    .ToList();

                if (string.IsNullOrWhiteSpace(requestedVersion))
                    return orderedAll;

                var exact = allParsed
                    .Where(x => x.GameVersions.Any(gv => string.Equals(gv, requestedVersion, StringComparison.OrdinalIgnoreCase)))
                    .Select(x => x.Version)
                    .OrderByDescending(v => v.DatePublished)
                    .ToList();
                if (exact.Count > 0)
                    return exact;

                var requestedMajorMinor = requestedVersion;
                var parts = requestedVersion.Split('.');
                if (parts.Length >= 2)
                    requestedMajorMinor = $"{parts[0]}.{parts[1]}";

                var majorMinorMatch = allParsed
                    .Where(x => x.GameVersions.Any(gv =>
                        string.Equals(gv, requestedMajorMinor, StringComparison.OrdinalIgnoreCase) ||
                        gv.StartsWith(requestedMajorMinor + ".", StringComparison.OrdinalIgnoreCase)))
                    .Select(x => x.Version)
                    .OrderByDescending(v => v.DatePublished)
                    .ToList();
                if (majorMinorMatch.Count > 0)
                    return majorMinorMatch;

                return orderedAll;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Get versions error: {ex.Message}");
                return new List<ModVersion>();
            }
        }

        public async Task<bool> DownloadModAsync(string fileUrl, string savePath, IProgress<(long, long)> progress)
        {
            try
            {
                using var response = await _httpClient.GetAsync(fileUrl, HttpCompletionOption.ResponseHeadersRead);
                response.EnsureSuccessStatusCode();

                var contentLength = response.Content.Headers.ContentLength ?? 0;
                using var contentStream = await response.Content.ReadAsStreamAsync();

                var directory = Path.GetDirectoryName(savePath);
                if (string.IsNullOrWhiteSpace(directory))
                    return false;

                if (!Directory.Exists(directory))
                    Directory.CreateDirectory(directory);

                using var fileStream = new FileStream(savePath, FileMode.Create, FileAccess.Write, FileShare.None, 8192, true);

                var buffer = new byte[8192];
                var totalRead = 0L;
                int bytesRead;

                while ((bytesRead = await contentStream.ReadAsync(buffer, 0, buffer.Length)) != 0)
                {
                    await fileStream.WriteAsync(buffer, 0, bytesRead);
                    totalRead += bytesRead;
                    progress?.Report((totalRead, contentLength));
                }

                return true;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Download error: {ex.Message}");
                if (File.Exists(savePath))
                    File.Delete(savePath);
                return false;
            }
        }

        public async Task<List<InstalledMod>> GetInstalledModsAsync()
        {
            try
            {
                var installedMods = new List<InstalledMod>();

                if (!Directory.Exists(_modsFolder))
                    return installedMods;

                var modFiles = Directory.GetFiles(_modsFolder, "*.jar");

                foreach (var file in modFiles)
                {
                    var fileInfo = new FileInfo(file);
                    installedMods.Add(new InstalledMod
                    {
                        Id = Path.GetFileNameWithoutExtension(file),
                        Title = Path.GetFileNameWithoutExtension(file),
                        FilePath = file,
                        FileSize = fileInfo.Length,
                        InstalledDate = fileInfo.CreationTime,
                        IsEnabled = true
                    });
                }

                return await Task.FromResult(installedMods);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Get installed mods error: {ex.Message}");
                return new List<InstalledMod>();
            }
        }

        public async Task<bool> InstallModAsync(ModFile file, string gameVersion)
        {
            try
            {
                var savePath = Path.Combine(_modsFolder, gameVersion, file.Filename);
                var versionFolder = Path.GetDirectoryName(savePath);
                if (string.IsNullOrWhiteSpace(versionFolder))
                    return false;

                if (!Directory.Exists(versionFolder))
                    Directory.CreateDirectory(versionFolder);

                var progress = new Progress<(long current, long total)>(p =>
                {
                    var percent = p.total > 0 ? (p.current * 100) / p.total : 0;
                    System.Diagnostics.Debug.WriteLine($"Download progress: {percent}%");
                });

                return await DownloadModAsync(file.Url, savePath, progress);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Install mod error: {ex.Message}");
                return false;
            }
        }

        public async Task<bool> UninstallModAsync(string modId)
        {
            try
            {
                var filePath = Path.Combine(_modsFolder, $"{modId}.jar");

                if (File.Exists(filePath))
                {
                    File.Delete(filePath);
                    return await Task.FromResult(true);
                }

                return await Task.FromResult(false);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Uninstall mod error: {ex.Message}");
                return false;
            }
        }
    }
}

