using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace MClauncher.Services
{
    public class MinecraftNewsItem
    {
        public string Title { get; set; } = "";
        public string Category { get; set; } = "";
        public string Date { get; set; } = "";
        public string Description { get; set; } = "";
        public string Url { get; set; } = "";
        public string ImageUrl { get; set; } = "";
        public string Tag { get; set; } = "";
    }

    public static class MinecraftNewsService
    {
        private static readonly HttpClient _http = new()
        {
            DefaultRequestHeaders =
            {
                { "User-Agent", "NexusLauncher/1.0 (+https://minecraft.net)" }
            },
            Timeout = TimeSpan.FromSeconds(10)
        };

        // Minecraft.net has a JSON news endpoint
        private const string NewsApiUrl = "https://launchercontent.mojang.com/news.json";

        public static async Task<List<MinecraftNewsItem>> FetchNewsAsync(int maxItems = 6)
        {
            try
            {
                var json = await _http.GetStringAsync(NewsApiUrl);
                using var doc = JsonDocument.Parse(json);

                var result = new List<MinecraftNewsItem>();
                var entries = doc.RootElement.GetProperty("entries");

                int count = 0;
                foreach (var entry in entries.EnumerateArray())
                {
                    if (count >= maxItems) break;

                    var title = SafeGetString(entry, "title");
                    var category = SafeGetString(entry, "category");
                    var date = SafeGetString(entry, "date");
                    var description = SafeGetString(entry, "text");
                    var readMoreUrl = SafeGetString(entry, "readMoreLink");
                    var tag = SafeGetString(entry, "tag");

                    // Image URL
                    string imageUrl = "";
                    if (entry.TryGetProperty("playPageImage", out var img))
                        imageUrl = SafeGetString(img, "url");
                    else if (entry.TryGetProperty("cardImage", out var card))
                        imageUrl = SafeGetString(card, "url");

                    if (!string.IsNullOrWhiteSpace(imageUrl) && !imageUrl.StartsWith("http", StringComparison.OrdinalIgnoreCase))
                        imageUrl = "https://launchercontent.mojang.com" + imageUrl;

                    result.Add(new MinecraftNewsItem
                    {
                        Title = title,
                        Category = category,
                        Date = FormatDate(date),
                        Description = TruncateText(description, 100),
                        Url = readMoreUrl.StartsWith("http", StringComparison.OrdinalIgnoreCase)
                            ? readMoreUrl
                            : "https://www.minecraft.net" + readMoreUrl,
                        ImageUrl = imageUrl,
                        Tag = tag
                    });
                    count++;
                }

                return result;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[NewsService] Error: {ex.Message}");
                return GetFallbackNews();
            }
        }

        private static string SafeGetString(JsonElement el, string prop)
        {
            if (el.TryGetProperty(prop, out var v) && v.ValueKind == JsonValueKind.String)
                return v.GetString() ?? "";
            return "";
        }

        private static string FormatDate(string raw)
        {
            if (DateTime.TryParse(raw, out var dt))
            {
                var diff = DateTime.UtcNow - dt;
                if (diff.TotalDays < 1) return "Today";
                if (diff.TotalDays < 2) return "Yesterday";
                if (diff.TotalDays < 7) return $"{(int)diff.TotalDays} days ago";
                return dt.ToString("MMM d, yyyy");
            }
            return raw;
        }

        private static string TruncateText(string text, int max)
        {
            if (string.IsNullOrEmpty(text)) return "";
            return text.Length <= max ? text : text.Substring(0, max).TrimEnd() + "...";
        }

        private static List<MinecraftNewsItem> GetFallbackNews()
        {
            return new List<MinecraftNewsItem>
            {
                new() { Title = "Minecraft 1.21.4 Update", Category = "Update", Date = "Recent",
                    Description = "The latest Minecraft update brings exciting new features and improvements.",
                    Url = "https://www.minecraft.net/en-us/article/minecraft-java-edition-1-21-4", Tag = "UPDATE" },
                new() { Title = "Minecraft Live 2024", Category = "Event", Date = "Recent",
                    Description = "Watch the biggest Minecraft event of the year!",
                    Url = "https://www.minecraft.net/en-us/live", Tag = "EVENT" },
                new() { Title = "New Mob Vote Results", Category = "News", Date = "Recent",
                    Description = "The community has voted for the next mob to be added to Minecraft!",
                    Url = "https://www.minecraft.net/en-us/article", Tag = "NEWS" },
            };
        }
    }
}
