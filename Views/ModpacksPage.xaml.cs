using System;
using System.Collections.Generic;
using System.IO;
using System.Net.Http;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using MClauncher.Models;
using MClauncher.Services;
using MessageBox = System.Windows.MessageBox;
using MessageBoxButton = System.Windows.MessageBoxButton;
using MessageBoxImage = System.Windows.MessageBoxImage;

namespace MClauncher.Views
{
    public partial class ModpacksPage : Page
    {
        private List<ModrinthMod> _currentModpacks = new();
        private static readonly HttpClient _httpClient = new() { Timeout = TimeSpan.FromSeconds(30) };
        private readonly Dictionary<string, Image> _iconControls = new();
        private ModrinthMod? _selectedModpack;
        private bool _isDownloading = false;

        // ── Categories (label, emoji, Modrinth facet)
        private static readonly List<(string Label, string Icon, string Facet)> Categories = new()
        {
            ("All",         "🌐", ""),
            ("Adventure",   "⚔️", "adventure"),
            ("Tech",        "⚙️", "technology"),
            ("Magic",       "✨", "magic"),
            ("Kitchen Sink","🍲", "kitchen-sink"),
            ("Multiplayer", "👥", "multiplayer"),
            ("Skyblock",    "🌤️", "skyblock"),
            ("Horror",      "👻", "horror"),
            ("Optimization","⚡","optimization"),
        };

        private string _activeCategory = "";

        public ModpacksPage()
        {
            InitializeComponent();
        }

        private void Page_Loaded(object sender, RoutedEventArgs e)
        {
            BuildCategoryChips();
            SetupSearchPlaceholder();
        }

        private void SetupSearchPlaceholder()
        {
            searchBox.TextChanged += (s, ev) =>
                searchPlaceholder.Visibility = string.IsNullOrEmpty(searchBox.Text) ? Visibility.Visible : Visibility.Collapsed;
        }

        // ─── Category Chips ──────────────────────────────────────────────────────

        private void BuildCategoryChips()
        {
            categoryChipsPanel.Children.Clear();
            foreach (var cat in Categories)
            {
                var isActive = cat.Facet == _activeCategory;
                var chip = new Border
                {
                    CornerRadius = new CornerRadius(20),
                    Padding = new Thickness(14, 6, 14, 6),
                    Margin = new Thickness(0, 0, 8, 0),
                    Cursor = Cursors.Hand,
                    Background = isActive
                        ? new LinearGradientBrush(
                            Color.FromArgb(80, 124, 77, 255),
                            Color.FromArgb(60, 98, 239, 173),
                            new Point(0, 0), new Point(1, 0))
                        : new SolidColorBrush(Color.FromArgb(30, 100, 100, 180)),
                    BorderBrush = isActive
                        ? new SolidColorBrush(Color.FromArgb(100, 124, 77, 255))
                        : new SolidColorBrush(Color.FromArgb(20, 255, 255, 255)),
                    BorderThickness = new Thickness(1)
                };
                chip.Child = new TextBlock
                {
                    Text = $"{cat.Icon} {cat.Label}",
                    FontSize = 11,
                    FontWeight = isActive ? FontWeights.SemiBold : FontWeights.Normal,
                    Foreground = isActive
                        ? new SolidColorBrush(Colors.White)
                        : new SolidColorBrush(Color.FromRgb(0x88, 0x99, 0xBB))
                };
                var facet = cat.Facet;
                chip.MouseLeftButtonUp += async (s, ev) =>
                {
                    _activeCategory = facet;
                    BuildCategoryChips();
                    if (string.IsNullOrEmpty(facet)) { ShowWelcomePanel(); return; }
                    await RunSearch("", facet);
                };
                categoryChipsPanel.Children.Add(chip);
            }
        }

        // ─── Quick Search buttons ─────────────────────────────────────────────────

        private void QuickSearch_Click(object sender, MouseButtonEventArgs e)
        {
            if (sender is Border b && b.Child is TextBlock tb)
            {
                // Extract clean text (strip emoji)
                var text = tb.Text.Trim();
                // Remove leading emoji + space
                var spaceIdx = text.IndexOf(' ');
                var query = spaceIdx >= 0 ? text.Substring(spaceIdx + 1).Trim() : text;
                searchBox.Text = query;
                _ = RunSearch(query, "");
            }
        }

        // ─── Search ───────────────────────────────────────────────────────────────

        private void searchBox_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Enter) btnSearch_Click(btnSearch, new RoutedEventArgs());
        }

        private async void btnSearch_Click(object sender, RoutedEventArgs e)
        {
            _activeCategory = "";
            BuildCategoryChips();
            btnClearSearch.Visibility = Visibility.Visible;
            await RunSearch(searchBox.Text?.Trim() ?? "", "");
        }

        private void btnClearSearch_Click(object sender, RoutedEventArgs e)
        {
            searchBox.Text = "";
            btnClearSearch.Visibility = Visibility.Collapsed;
            _activeCategory = "";
            BuildCategoryChips();
            ShowWelcomePanel();
            lbStatus.Text = "Ready";
        }

        private async Task RunSearch(string query, string category)
        {
            btnSearch.IsEnabled = false;
            pbSearch.Visibility = Visibility.Visible;
            lbStatus.Text = "🔍 Searching modpacks...";
            _iconControls.Clear();

            var selectedItem = cbGameVersion.SelectedItem as ComboBoxItem;
            string version = selectedItem?.Content?.ToString() ?? "1.21.4";
            if (version == "Any") version = "";

            try
            {
                var service = new ModrinthService();
                _currentModpacks = await service.SearchModpacksAsync(query, version, 40, category);

                modpacksContainer.Children.Clear();

                if (_currentModpacks.Count == 0)
                {
                    ShowEmptyState("😴 No modpacks found", "Try a different search query or game version");
                    lbStatus.Text = "No results found";
                    return;
                }

                foreach (var pack in _currentModpacks)
                {
                    var card = CreateModpackCard(pack);
                    modpacksContainer.Children.Add(card);
                }

                lbStatus.Text = $"Found {_currentModpacks.Count} modpacks";

                // Async icon loading
                _ = Task.Run(async () =>
                {
                    foreach (var pack in _currentModpacks)
                    {
                        if (!string.IsNullOrEmpty(pack.IconUrl))
                            await LoadIconAsync(pack.Id, pack.IconUrl);
                    }
                });
            }
            catch (Exception ex)
            {
                ShowEmptyState("❌ Search failed", ex.Message);
                lbStatus.Text = "Search failed";
            }
            finally
            {
                btnSearch.IsEnabled = true;
                pbSearch.Visibility = Visibility.Collapsed;
            }
        }

        // ─── Card UI ──────────────────────────────────────────────────────────────

        private UIElement CreateModpackCard(ModrinthMod pack)
        {
            var card = new Border
            {
                CornerRadius = new CornerRadius(12),
                Margin = new Thickness(0, 0, 0, 10),
                Cursor = Cursors.Hand,
                BorderThickness = new Thickness(1),
                BorderBrush = new SolidColorBrush(Color.FromArgb(20, 255, 255, 255)),
                ClipToBounds = true
            };
            card.Background = new LinearGradientBrush(
                Color.FromArgb(200, 15, 18, 32),
                Color.FromArgb(200, 10, 12, 22),
                new Point(0, 0), new Point(1, 1));

            card.MouseEnter += (s, e) =>
                card.BorderBrush = new SolidColorBrush(Color.FromArgb(60, 124, 77, 255));
            card.MouseLeave += (s, e) =>
                card.BorderBrush = new SolidColorBrush(Color.FromArgb(20, 255, 255, 255));
            card.MouseLeftButtonUp += (s, e) => ShowModpackDetails(pack);

            var grid = new Grid();
            grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(88) });
            grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
            grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(96) });

            // Icon
            var iconBg = new Border
            {
                Width = 88, Height = 88,
                Background = new LinearGradientBrush(
                    Color.FromArgb(255, 18, 10, 40),
                    Color.FromArgb(255, 10, 18, 32),
                    new Point(0, 0), new Point(1, 1)),
                CornerRadius = new CornerRadius(12, 0, 0, 12),
                ClipToBounds = true
            };
            var iconGrid = new Grid();
            iconGrid.Children.Add(new TextBlock
            {
                Text = "📦", FontSize = 32,
                HorizontalAlignment = HorizontalAlignment.Center,
                VerticalAlignment = VerticalAlignment.Center
            });
            var img = new Image { Stretch = Stretch.UniformToFill };
            iconGrid.Children.Add(img);
            iconBg.Child = iconGrid;
            _iconControls[pack.Id] = img;
            Grid.SetColumn(iconBg, 0);
            grid.Children.Add(iconBg);

            // Info
            var info = new StackPanel { Margin = new Thickness(14, 10, 10, 10), VerticalAlignment = VerticalAlignment.Center };
            Grid.SetColumn(info, 1);

            info.Children.Add(new TextBlock
            {
                Text = pack.Title,
                FontSize = 14,
                FontWeight = FontWeights.Bold,
                Foreground = new SolidColorBrush(Colors.White),
                TextWrapping = TextWrapping.NoWrap,
                TextTrimming = TextTrimming.CharacterEllipsis
            });
            info.Children.Add(new TextBlock
            {
                Text = $"by {pack.Author}",
                FontSize = 10,
                Foreground = new SolidColorBrush(Color.FromRgb(0x44, 0x66, 0x88)),
                Margin = new Thickness(0, 2, 0, 5)
            });
            info.Children.Add(new TextBlock
            {
                Text = pack.Description,
                FontSize = 10,
                TextWrapping = TextWrapping.Wrap,
                MaxHeight = 34,
                TextTrimming = TextTrimming.CharacterEllipsis,
                Foreground = new SolidColorBrush(Color.FromRgb(0x55, 0x66, 0x88))
            });

            // Tags row
            if (pack.Categories.Count > 0)
            {
                var tags = new StackPanel { Orientation = Orientation.Horizontal, Margin = new Thickness(0, 5, 0, 0) };
                foreach (var cat in pack.Categories.Take(3))
                {
                    var tag = new Border
                    {
                        Background = new SolidColorBrush(Color.FromArgb(35, 124, 77, 255)),
                        CornerRadius = new CornerRadius(4),
                        Padding = new Thickness(5, 2, 5, 2),
                        Margin = new Thickness(0, 0, 4, 0)
                    };
                    tag.Child = new TextBlock { Text = cat, FontSize = 9, Foreground = new SolidColorBrush(Color.FromRgb(0x7C, 0x4D, 0xFF)) };
                    tags.Children.Add(tag);
                }
                info.Children.Add(tags);
            }
            grid.Children.Add(info);

            // Download button column
            var actions = new StackPanel { VerticalAlignment = VerticalAlignment.Center, Margin = new Thickness(4, 8, 12, 8) };
            Grid.SetColumn(actions, 2);

            // Downloads badge
            var dlBadge = new Border
            {
                Background = new SolidColorBrush(Color.FromArgb(30, 98, 239, 173)),
                CornerRadius = new CornerRadius(6),
                Padding = new Thickness(6, 3, 6, 3),
                Margin = new Thickness(0, 0, 0, 6),
                HorizontalAlignment = HorizontalAlignment.Right
            };
            dlBadge.Child = new TextBlock
            {
                Text = $"📥 {FormatNumber(pack.Downloads)}",
                FontSize = 9,
                Foreground = new SolidColorBrush(Color.FromRgb(0x62, 0xEF, 0xAD))
            };
            actions.Children.Add(dlBadge);

            var dlBtn = new Wpf.Ui.Controls.Button
            {
                Content = "⬇️ Download",
                Appearance = Wpf.Ui.Controls.ControlAppearance.Primary,
                Padding = new Thickness(6, 6, 6, 6),
                FontSize = 10,
                Width = 82,
                Margin = new Thickness(0, 0, 0, 4)
            };
            dlBtn.Click += async (s, e) =>
            {
                e.Handled = true;
                ShowModpackDetails(pack);
                await DownloadModpackAsync(pack, dlBtn);
            };

            var detailsBtn = new Wpf.Ui.Controls.Button
            {
                Content = "🔍 Details",
                Appearance = Wpf.Ui.Controls.ControlAppearance.Secondary,
                Padding = new Thickness(6, 4, 6, 4),
                FontSize = 10,
                Width = 82
            };
            detailsBtn.Click += (s, e) => { e.Handled = true; ShowModpackDetails(pack); };

            actions.Children.Add(dlBtn);
            actions.Children.Add(detailsBtn);
            grid.Children.Add(actions);
            card.Child = grid;
            return card;
        }

        // ─── Details Panel ────────────────────────────────────────────────────────

        private void ShowModpackDetails(ModrinthMod pack)
        {
            _selectedModpack = pack;
            detailsPanel.Children.Clear();

            // Icon
            var iconBorder = new Border
            {
                Width = 90, Height = 90,
                Background = new SolidColorBrush(Color.FromRgb(18, 10, 36)),
                CornerRadius = new CornerRadius(14),
                Margin = new Thickness(0, 0, 0, 12),
                HorizontalAlignment = HorizontalAlignment.Center,
                ClipToBounds = true
            };
            var ig = new Grid();
            ig.Children.Add(new TextBlock { Text = "📦", FontSize = 38, HorizontalAlignment = HorizontalAlignment.Center, VerticalAlignment = VerticalAlignment.Center });
            var detImg = new Image { Stretch = Stretch.UniformToFill };
            ig.Children.Add(detImg);
            iconBorder.Child = ig;
            detailsPanel.Children.Add(iconBorder);

            if (!string.IsNullOrEmpty(pack.IconUrl))
                _ = LoadIconToImageAsync(pack.IconUrl, detImg);

            detailsPanel.Children.Add(new TextBlock
            {
                Text = pack.Title, FontSize = 16, FontWeight = FontWeights.Bold,
                Foreground = new SolidColorBrush(Colors.White),
                TextWrapping = TextWrapping.Wrap, HorizontalAlignment = HorizontalAlignment.Center,
                TextAlignment = TextAlignment.Center, Margin = new Thickness(0, 0, 0, 4)
            });
            detailsPanel.Children.Add(new TextBlock
            {
                Text = $"by {pack.Author}", FontSize = 11,
                Foreground = new SolidColorBrush(Color.FromRgb(0x44, 0x66, 0x88)),
                HorizontalAlignment = HorizontalAlignment.Center, Margin = new Thickness(0, 0, 0, 14)
            });

            // Stats
            var statsCard = new Border
            {
                Background = new SolidColorBrush(Color.FromArgb(50, 60, 60, 100)),
                CornerRadius = new CornerRadius(8),
                Padding = new Thickness(10, 8, 10, 8),
                Margin = new Thickness(0, 0, 0, 14)
            };
            var sg = new Grid();
            sg.ColumnDefinitions.Add(new ColumnDefinition());
            sg.ColumnDefinitions.Add(new ColumnDefinition());

            void AddStat(int col, string label, string val, string color)
            {
                var sp = new StackPanel { HorizontalAlignment = HorizontalAlignment.Center };
                sp.Children.Add(new TextBlock { Text = val, FontSize = 14, FontWeight = FontWeights.Bold, HorizontalAlignment = HorizontalAlignment.Center, Foreground = new SolidColorBrush((Color)ColorConverter.ConvertFromString(color)) });
                sp.Children.Add(new TextBlock { Text = label, FontSize = 9, HorizontalAlignment = HorizontalAlignment.Center, Foreground = new SolidColorBrush(Color.FromRgb(0x44, 0x55, 0x66)) });
                Grid.SetColumn(sp, col);
                sg.Children.Add(sp);
            }
            AddStat(0, "Downloads", FormatNumber(pack.Downloads), "#62EFAD");
            AddStat(1, "Created", pack.CreatedAt.ToString("MMM yyyy"), "#7C4DFF");
            statsCard.Child = sg;
            detailsPanel.Children.Add(statsCard);

            // Description
            detailsPanel.Children.Add(new TextBlock
            {
                Text = pack.Description, FontSize = 11, TextWrapping = TextWrapping.Wrap,
                Foreground = new SolidColorBrush(Color.FromRgb(0x55, 0x66, 0x88)),
                Margin = new Thickness(0, 0, 0, 14)
            });

            // Categories
            if (pack.Categories.Count > 0)
            {
                var tw = new WrapPanel { Margin = new Thickness(0, 0, 0, 14) };
                foreach (var cat in pack.Categories)
                {
                    var tb = new Border
                    {
                        Background = new SolidColorBrush(Color.FromArgb(40, 124, 77, 255)),
                        CornerRadius = new CornerRadius(6),
                        Padding = new Thickness(8, 3, 8, 3),
                        Margin = new Thickness(0, 0, 4, 4)
                    };
                    tb.Child = new TextBlock { Text = cat, FontSize = 10, Foreground = new SolidColorBrush(Color.FromRgb(0x7C, 0x4D, 0xFF)) };
                    tw.Children.Add(tb);
                }
                detailsPanel.Children.Add(tw);
            }

            // Game versions
            if (pack.GameVersions.Count > 0)
            {
                detailsPanel.Children.Add(new TextBlock
                {
                    Text = "🎮 Versions",
                    FontSize = 11, FontWeight = FontWeights.SemiBold,
                    Foreground = new SolidColorBrush(Colors.White),
                    Margin = new Thickness(0, 0, 0, 4)
                });
                var vText = string.Join("  •  ", pack.GameVersions.Take(6));
                if (pack.GameVersions.Count > 6) vText += $" +{pack.GameVersions.Count - 6} more";
                detailsPanel.Children.Add(new TextBlock
                {
                    Text = vText, FontSize = 10, TextWrapping = TextWrapping.Wrap,
                    Foreground = new SolidColorBrush(Color.FromRgb(0x44, 0x55, 0x66)),
                    Margin = new Thickness(0, 0, 0, 14)
                });
            }

            detailsPanel.Children.Add(new Separator { Margin = new Thickness(0, 0, 0, 14) });

            // Download button
            var dlBtn = new Wpf.Ui.Controls.Button
            {
                Content = "⬇️ Download Modpack",
                Appearance = Wpf.Ui.Controls.ControlAppearance.Primary,
                Padding = new Thickness(16, 10, 16, 10),
                HorizontalAlignment = HorizontalAlignment.Stretch,
                FontSize = 12, Margin = new Thickness(0, 0, 0, 8),
                Tag = pack
            };
            dlBtn.Click += async (s, e) => await DownloadModpackAsync(pack, dlBtn);
            detailsPanel.Children.Add(dlBtn);

            // Modrinth link
            var linkBtn = new Wpf.Ui.Controls.Button
            {
                Content = "🔗 View on Modrinth",
                Appearance = Wpf.Ui.Controls.ControlAppearance.Secondary,
                Padding = new Thickness(12, 8, 12, 8),
                HorizontalAlignment = HorizontalAlignment.Stretch,
                FontSize = 11
            };
            linkBtn.Click += (s, e) =>
            {
                try { System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo($"https://modrinth.com/modpack/{pack.Slug}") { UseShellExecute = true }); }
                catch { }
            };
            detailsPanel.Children.Add(linkBtn);
        }

        // ─── Download ─────────────────────────────────────────────────────────────

        private async Task DownloadModpackAsync(ModrinthMod pack, Wpf.Ui.Controls.Button downloadBtn)
        {
            if (_isDownloading)
            {
                MessageBox.Show("A download is already in progress. Please wait.", "Download In Progress", MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }

            _isDownloading = true;
            downloadBtn.IsEnabled = false;
            downloadBtn.Content = "⏳ Fetching...";

            // Show download panels
            downloadPanel.Visibility = Visibility.Visible;
            globalDownloadBar.Visibility = Visibility.Visible;
            pbDownload.Value = 0;
            pbGlobalDownload.Value = 0;
            lbDownloadName.Text = $"Downloading: {pack.Title}";
            lbGlobalFile.Text = $"⬇️ {pack.Title}";
            lbDownloadPct.Text = "0%";
            lbGlobalPct.Text = "0%";

            try
            {
                var service = new ModrinthService();
                var selectedItem = cbGameVersion.SelectedItem as ComboBoxItem;
                string version = selectedItem?.Content?.ToString() ?? "1.21.4";
                if (version == "Any") version = "";

                // Get modpack versions (selected version first, then fallback to any)
                var versions = await service.GetModVersionsAsync(pack.Id, version);

                if (versions.Count == 0 && !string.IsNullOrWhiteSpace(version))
                {
                    versions = await service.GetModVersionsAsync(pack.Id, "");
                }

                if (versions.Count == 0)
                {
                    MessageBox.Show($"No downloadable version found for '{pack.Title}'.\nTry selecting a different game version.", "No Versions", MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                var latestVersion = versions[0];
                var primaryFile = latestVersion.Files.Find(f => f.Primary) ?? latestVersion.Files.FirstOrDefault();

                if (primaryFile == null || string.IsNullOrEmpty(primaryFile.Url))
                {
                    MessageBox.Show("No downloadable file found for this modpack.", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                    return;
                }

                // Save destination
                var saveFolder = Path.Combine(
                    Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
                    ".mclauncher", "modpacks", pack.Slug ?? pack.Id);
                Directory.CreateDirectory(saveFolder);

                var savePath = Path.Combine(saveFolder, primaryFile.Filename);

                if (File.Exists(savePath))
                {
                    var res = MessageBox.Show(
                        $"'{pack.Title}' is already downloaded.\nFile: {primaryFile.Filename}\n\nRedownload?",
                        "Already Downloaded", MessageBoxButton.YesNo, MessageBoxImage.Question);
                    if (res != MessageBoxResult.Yes) return;
                }

                downloadBtn.Content = "⬇️ 0%";
                lbStatus.Text = $"Downloading {pack.Title} v{latestVersion.VersionNumber}...";

                var fileSizeStr = "";
                var progress = new Progress<(long cur, long total)>(p =>
                {
                    if (p.total > 0)
                    {
                        var pct = (int)(p.cur * 100 / p.total);
                        var curMb = p.cur / 1024.0 / 1024.0;
                        var totMb = p.total / 1024.0 / 1024.0;
                        fileSizeStr = $"{curMb:F1} MB / {totMb:F1} MB";

                        Dispatcher.Invoke(() =>
                        {
                            pbDownload.Value = pct;
                            pbGlobalDownload.Value = pct;
                            lbDownloadPct.Text = $"{pct}%";
                            lbGlobalPct.Text = $"{pct}%";
                            lbDownloadSize.Text = fileSizeStr;
                            lbGlobalSize.Text = fileSizeStr;
                            downloadBtn.Content = $"⬇️ {pct}%";
                        });
                    }
                });

                var success = await service.DownloadModAsync(primaryFile.Url, savePath, progress);

                if (success)
                {
                    downloadBtn.Content = "✅ Downloaded!";
                    lbStatus.Text = $"✅ {pack.Title} v{latestVersion.VersionNumber} downloaded!";
                    lbDownloadName.Text = $"✅ {pack.Title} — Done!";
                    lbGlobalFile.Text = $"✅ {pack.Title} — Done!";

                    var inferredLoader = DetectLoaderFromFileName(primaryFile.Filename);
                    var profileGameVersion = !string.IsNullOrWhiteSpace(latestVersion.GameVersion)
                        ? latestVersion.GameVersion
                        : (string.IsNullOrWhiteSpace(version) ? "1.21.4" : version);

                    var profile = ProfileManager.RegisterDownloadedModpackProfile(
                        pack.Id,
                        latestVersion.Id,
                        pack.Title,
                        profileGameVersion,
                        savePath,
                        inferredLoader);

                    ConsolePage.Log($"[Modpacks] Downloaded: {pack.Title} v{latestVersion.VersionNumber} → {savePath}");
                    ConsolePage.Log($"[Profiles] Modpack profile registered: {profile.Name} ({profile.GameVersion}, {profile.Loader})");

                    MessageBox.Show(
                        $"✅ Modpack downloaded successfully!\n\n" +
                        $"📦 {pack.Title} v{latestVersion.VersionNumber}\n" +
                        $"📁 Saved to: {savePath}\n\n" +
                        $"A launchable profile was added in Profiles.",
                        "Download Complete", MessageBoxButton.OK, MessageBoxImage.Information);
                }
                else
                {
                    downloadBtn.Content = "❌ Failed";
                    lbStatus.Text = "Download failed";
                }
            }
            catch (Exception ex)
            {
                downloadBtn.Content = "❌ Error";
                lbStatus.Text = $"Download error: {ex.Message}";
                MessageBox.Show($"Download error:\n{ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
            finally
            {
                _isDownloading = false;
                await Task.Delay(3000);
                downloadPanel.Visibility = Visibility.Collapsed;
                globalDownloadBar.Visibility = Visibility.Collapsed;
                downloadBtn.IsEnabled = true;
                if (!downloadBtn.Content.ToString()!.StartsWith("✅") && !downloadBtn.Content.ToString()!.StartsWith("❌"))
                    downloadBtn.Content = "⬇️ Download";
                await Task.Delay(3000);
                if (downloadBtn.Content.ToString()!.StartsWith("✅") || downloadBtn.Content.ToString()!.StartsWith("❌"))
                    downloadBtn.Content = "⬇️ Download";
            }
        }

        // ─── Image Loading ─────────────────────────────────────────────────────────

        private async Task LoadIconAsync(string modpackId, string url)
        {
            try
            {
                var bytes = await _httpClient.GetByteArrayAsync(url);
                await Dispatcher.InvokeAsync(() =>
                {
                    try
                    {
                        if (!_iconControls.TryGetValue(modpackId, out var imgControl)) return;
                        using var ms = new MemoryStream(bytes);
                        var bmp = new BitmapImage();
                        bmp.BeginInit(); bmp.StreamSource = ms; bmp.CacheOption = BitmapCacheOption.OnLoad; bmp.EndInit(); bmp.Freeze();
                        imgControl.Source = bmp;
                    }
                    catch { }
                });
            }
            catch { }
        }

        private async Task LoadIconToImageAsync(string url, Image target)
        {
            try
            {
                var bytes = await _httpClient.GetByteArrayAsync(url);
                await Dispatcher.InvokeAsync(() =>
                {
                    try
                    {
                        using var ms = new MemoryStream(bytes);
                        var bmp = new BitmapImage();
                        bmp.BeginInit(); bmp.StreamSource = ms; bmp.CacheOption = BitmapCacheOption.OnLoad; bmp.EndInit(); bmp.Freeze();
                        target.Source = bmp;
                    }
                    catch { }
                });
            }
            catch { }
        }

        // ─── Helpers ──────────────────────────────────────────────────────────────

        private void ShowWelcomePanel()
        {
            modpacksContainer.Children.Clear();
            modpacksContainer.Children.Add(welcomePanel);
        }

        private void ShowEmptyState(string title, string message)
        {
            modpacksContainer.Children.Clear();
            var panel = new StackPanel { HorizontalAlignment = HorizontalAlignment.Center, Margin = new Thickness(0, 60, 0, 0) };
            panel.Children.Add(new TextBlock { Text = "😴", FontSize = 40, HorizontalAlignment = HorizontalAlignment.Center, Margin = new Thickness(0, 0, 0, 12) });
            panel.Children.Add(new TextBlock { Text = title, FontSize = 16, FontWeight = FontWeights.SemiBold, Foreground = new SolidColorBrush(Color.FromRgb(0xAA, 0xBB, 0xCC)), HorizontalAlignment = HorizontalAlignment.Center });
            panel.Children.Add(new TextBlock { Text = message, FontSize = 11, Foreground = new SolidColorBrush(Color.FromRgb(0x44, 0x55, 0x66)), HorizontalAlignment = HorizontalAlignment.Center, Margin = new Thickness(0, 6, 0, 0) });
            modpacksContainer.Children.Add(panel);
        }

        private static ModLoader DetectLoaderFromFileName(string fileName)
        {
            if (string.IsNullOrWhiteSpace(fileName))
                return ModLoader.Vanilla;

            var lower = fileName.ToLowerInvariant();
            if (lower.Contains("neoforge")) return ModLoader.NeoForge;
            if (lower.Contains("forge")) return ModLoader.Forge;
            if (lower.Contains("fabric")) return ModLoader.Fabric;
            if (lower.Contains("quilt")) return ModLoader.Quilt;
            return ModLoader.Vanilla;
        }

        private static string FormatNumber(long n)
        {
            if (n >= 1_000_000) return $"{n / 1_000_000.0:F1}M";
            if (n >= 1_000) return $"{n / 1_000.0:F0}K";
            return n.ToString();
        }
    }
}
