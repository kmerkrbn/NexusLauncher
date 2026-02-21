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
    public partial class ModsPage : Page
    {
        private List<ModrinthMod> _currentMods = new();
        private static readonly HttpClient _httpClient = new() { Timeout = TimeSpan.FromSeconds(30) };
        private readonly Dictionary<string, Image> _modImageControls = new();
        private ModProfile? _targetProfile;

        // ── Categories (label, Modrinth facet value)
        private static readonly List<(string Label, string Icon, string Facet)> Categories = new()
        {
            ("All",         "🌐", ""),
            ("Performance", "⚡", "performance"),
            ("Utility",     "🔧", "utility"),
            ("Food",        "🍖", "food"),
            ("Adventure",   "🗺️", "adventure"),
            ("Magic",       "✨", "magic"),
            ("Technology",  "⚙️", "technology"),
            ("Storage",     "📦", "storage"),
            ("Combat",      "⚔️", "combat"),
            ("Decoration",  "🏠", "decoration"),
            ("Fabric",      "🧵", "fabric"),
            ("Forge",       "⚒️", "forge"),
        };

        private static readonly List<string> QuickSearches = new()
            { "Sodium", "JEI", "Waystones", "Iron Chests", "Biomes O'Plenty",
              "Create", "Tinkers' Construct", "Pam's HarvestCraft", "Optifine" };

        private string _activeCategory = "";

        public ModsPage() : this(null) { }

        public ModsPage(ModProfile? targetProfile)
        {
            InitializeComponent();
            _targetProfile = targetProfile;
        }

        private void Page_Loaded(object sender, RoutedEventArgs e)
        {
            if (_targetProfile != null)
            {
                lbProfileContext.Text = $"🎯 Showing mods for: {_targetProfile.Name}  (v{_targetProfile.GameVersion} • {_targetProfile.Loader})";

                // Auto-select the profile's game version in the version filter
                SelectProfileVersion(_targetProfile.GameVersion);
            }
            else
            {
                lbProfileContext.Text = "Browse and install mods from Modrinth";
            }

            BuildCategoryChips();
            BuildQuickSearches();
        }

        private void SelectProfileVersion(string targetVersion)
        {
            // Try to find matching ComboBoxItem
            for (int i = 0; i < cbGameVersion.Items.Count; i++)
            {
                if (cbGameVersion.Items[i] is ComboBoxItem item &&
                    item.Content?.ToString() == targetVersion)
                {
                    cbGameVersion.SelectedIndex = i;
                    return;
                }
            }

            // Not found — add it at top and select it
            var newItem = new ComboBoxItem { Content = targetVersion };
            cbGameVersion.Items.Insert(0, newItem);
            cbGameVersion.SelectedIndex = 0;
        }

        // ─────────────────────────────────────────────
        //  CATEGORY CHIPS
        // ─────────────────────────────────────────────
        private void BuildCategoryChips()
        {
            categoryChipsPanel.Children.Clear();
            foreach (var cat in Categories)
            {
                var isActive = cat.Facet == _activeCategory;
                var chip = new Border
                {
                    Background = isActive
                        ? (Brush)Application.Current.FindResource("AccentFillColorDefaultBrush")
                        : new SolidColorBrush(Color.FromArgb(40, 120, 120, 200)),
                    CornerRadius = new CornerRadius(20),
                    Padding = new Thickness(14, 6, 14, 6),
                    Margin = new Thickness(0, 0, 8, 0),
                    Cursor = Cursors.Hand
                };
                chip.Child = new TextBlock
                {
                    Text = $"{cat.Icon} {cat.Label}",
                    FontSize = 11,
                    FontWeight = isActive ? FontWeights.Bold : FontWeights.Normal,
                    Foreground = isActive ? Brushes.White : (Brush)Application.Current.FindResource("TextFillColorPrimaryBrush")
                };
                var facet = cat.Facet;
                chip.MouseLeftButtonUp += async (s, ev) => await SearchCategory(facet);
                categoryChipsPanel.Children.Add(chip);
            }
        }

        private async Task SearchCategory(string facet)
        {
            _activeCategory = facet;
            BuildCategoryChips();

            if (string.IsNullOrEmpty(facet))
            {
                searchBox.Text = "";
                ShowWelcomePanel();
                return;
            }

            // Search by categories using Modrinth facets
            await RunSearch("", facet);
        }

        // ─────────────────────────────────────────────
        //  QUICK SEARCHES
        // ─────────────────────────────────────────────
        private void BuildQuickSearches()
        {
            quickSearchPanel.Children.Clear();
            foreach (var qs in QuickSearches)
            {
                var btn = new Border
                {
                    Background = new SolidColorBrush(Color.FromArgb(50, 100, 180, 255)),
                    BorderBrush = new SolidColorBrush(Color.FromArgb(80, 100, 180, 255)),
                    BorderThickness = new Thickness(1),
                    CornerRadius = new CornerRadius(16),
                    Padding = new Thickness(12, 5, 12, 5),
                    Margin = new Thickness(4, 0, 4, 8),
                    Cursor = Cursors.Hand
                };
                btn.Child = new TextBlock
                {
                    Text = qs,
                    FontSize = 11,
                    Foreground = (Brush)Application.Current.FindResource("TextFillColorPrimaryBrush")
                };
                var query = qs;
                btn.MouseLeftButtonUp += async (s, ev) =>
                {
                    searchBox.Text = query;
                    await RunSearch(query, "");
                };
                quickSearchPanel.Children.Add(btn);
            }
        }

        private void ShowWelcomePanel()
        {
            modsContainer.Children.Clear();
            modsContainer.Children.Add(welcomePanel);
        }

        // ─────────────────────────────────────────────
        //  SEARCH
        // ─────────────────────────────────────────────
        private void searchBox_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Enter) btnSearch_Click(btnSearch, null!);
        }

        private async void btnSearch_Click(object sender, RoutedEventArgs e)
        {
            var query = searchBox.Text?.Trim() ?? "";
            _activeCategory = "";
            BuildCategoryChips();
            btnClearSearch.Visibility = Visibility.Visible;
            await RunSearch(query, "");
        }

        private async Task RunSearch(string query, string category)
        {
            btnSearch.IsEnabled = false;
            lbStatus.Text = "🔍 Searching...";
            _modImageControls.Clear();

            var selectedItem = cbGameVersion.SelectedItem as ComboBoxItem;
            string version = selectedItem?.Content?.ToString() ?? "1.21.4";
            if (string.Equals(version, "Any", StringComparison.OrdinalIgnoreCase))
                version = "";

            try
            {
                var service = new ModrinthService();
                _currentMods = await service.SearchModsAsync(query, version, 40, category);

                modsContainer.Children.Clear();

                if (_currentMods.Count == 0)
                {
                    modsContainer.Children.Add(CreateEmptyState("😴 No mods found", "Try a different search or game version"));
                    lbStatus.Text = "No results";
                    return;
                }

                foreach (var mod in _currentMods)
                {
                    var card = CreateModCard(mod);
                    modsContainer.Children.Add(card);
                }

                lbStatus.Text = $"Found {_currentMods.Count} mods";

                // Async image loading
                _ = Task.Run(async () =>
                {
                    foreach (var mod in _currentMods)
                    {
                        if (!string.IsNullOrEmpty(mod.IconUrl))
                            await LoadImageForModAsync(mod.Id, mod.IconUrl);
                    }
                });
            }
            catch (Exception ex)
            {
                modsContainer.Children.Clear();
                modsContainer.Children.Add(CreateEmptyState("❌ Search failed", ex.Message));
                lbStatus.Text = "Search failed";
            }
            finally
            {
                btnSearch.IsEnabled = true;
            }
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

        // ─────────────────────────────────────────────
        //  ASYNC IMAGE LOADING
        // ─────────────────────────────────────────────
        private async Task LoadImageForModAsync(string modId, string url)
        {
            try
            {
                var bytes = await _httpClient.GetByteArrayAsync(url);
                await Dispatcher.InvokeAsync(() =>
                {
                    try
                    {
                        if (!_modImageControls.TryGetValue(modId, out var imgControl)) return;
                        using var ms = new MemoryStream(bytes);
                        var bmp = new BitmapImage();
                        bmp.BeginInit();
                        bmp.StreamSource = ms;
                        bmp.CacheOption = BitmapCacheOption.OnLoad;
                        bmp.EndInit();
                        bmp.Freeze();
                        imgControl.Source = bmp;
                    }
                    catch { }
                });
            }
            catch { }
        }

        // ─────────────────────────────────────────────
        //  MOD CARD
        // ─────────────────────────────────────────────
        private UIElement CreateModCard(ModrinthMod mod)
        {
            var card = new Border
            {
                Background = (Brush)Application.Current.FindResource("CardBackgroundFillColorDefaultBrush"),
                BorderBrush = (Brush)Application.Current.FindResource("CardStrokeColorDefaultBrush"),
                BorderThickness = new Thickness(1),
                CornerRadius = new CornerRadius(10),
                Padding = new Thickness(0),
                Margin = new Thickness(0, 0, 0, 10),
                Cursor = Cursors.Hand
            };

            // Hover effect
            card.MouseEnter += (s, e) =>
                card.Background = (Brush)Application.Current.FindResource("LayerFillColorAltBrush");
            card.MouseLeave += (s, e) =>
                card.Background = (Brush)Application.Current.FindResource("CardBackgroundFillColorDefaultBrush");
            card.MouseLeftButtonUp += (s, e) => ShowModDetails(mod);

            var outer = new Grid();
            outer.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(76) });
            outer.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
            outer.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(88) });

            // Icon column
            var iconBg = new Border
            {
                Width = 76, Height = 76,
                Background = new SolidColorBrush(Color.FromRgb(30, 30, 40)),
                CornerRadius = new CornerRadius(10, 0, 0, 10),
                ClipToBounds = true
            };
            var iconGrid = new Grid();
            iconGrid.Children.Add(new TextBlock { Text = "📦", FontSize = 30, HorizontalAlignment = HorizontalAlignment.Center, VerticalAlignment = VerticalAlignment.Center });
            var img = new Image { Stretch = Stretch.UniformToFill };
            iconGrid.Children.Add(img);
            iconBg.Child = iconGrid;
            _modImageControls[mod.Id] = img;
            Grid.SetColumn(iconBg, 0);
            outer.Children.Add(iconBg);

            // Content column
            var content = new StackPanel { Margin = new Thickness(14, 10, 10, 10), VerticalAlignment = VerticalAlignment.Center };
            Grid.SetColumn(content, 1);

            var titleRow = new Grid();
            titleRow.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
            titleRow.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(70) });

            var titleTb = new TextBlock
            {
                Text = mod.Title,
                FontSize = 13,
                FontWeight = FontWeights.Bold,
                Foreground = (Brush)Application.Current.FindResource("TextFillColorPrimaryBrush"),
                TextWrapping = TextWrapping.NoWrap,
                TextTrimming = TextTrimming.CharacterEllipsis
            };
            Grid.SetColumn(titleTb, 0);
            titleRow.Children.Add(titleTb);

            // Downloads badge
            var dlBadge = new Border
            {
                Background = new SolidColorBrush(Color.FromArgb(40, 0, 200, 120)),
                CornerRadius = new CornerRadius(8),
                Padding = new Thickness(6, 2, 6, 2),
                VerticalAlignment = VerticalAlignment.Center,
                HorizontalAlignment = HorizontalAlignment.Right,
                Margin = new Thickness(4, 0, 0, 0)
            };
            dlBadge.Child = new TextBlock { Text = $"📥 {FormatNumber(mod.Downloads)}", FontSize = 9, Foreground = new SolidColorBrush(Color.FromRgb(0, 220, 140)) };
            Grid.SetColumn(dlBadge, 1);
            titleRow.Children.Add(dlBadge);

            content.Children.Add(titleRow);

            content.Children.Add(new TextBlock
            {
                Text = $"by {mod.Author}",
                FontSize = 10,
                Foreground = (Brush)Application.Current.FindResource("TextFillColorSecondaryBrush"),
                Margin = new Thickness(0, 2, 0, 4)
            });

            content.Children.Add(new TextBlock
            {
                Text = mod.Description,
                FontSize = 10,
                TextWrapping = TextWrapping.Wrap,
                MaxHeight = 32,
                Foreground = (Brush)Application.Current.FindResource("TextFillColorTertiaryBrush"),
                TextTrimming = TextTrimming.CharacterEllipsis
            });

            // Category tags
            if (mod.Categories.Count > 0)
            {
                var tagsPanel = new StackPanel { Orientation = Orientation.Horizontal, Margin = new Thickness(0, 4, 0, 0) };
                foreach (var cat in mod.Categories.Take(3))
                {
                    var tagBorder = new Border
                    {
                        Background = new SolidColorBrush(Color.FromArgb(30, 120, 120, 200)),
                        CornerRadius = new CornerRadius(4),
                        Padding = new Thickness(5, 1, 5, 1),
                        Margin = new Thickness(0, 0, 4, 0)
                    };
                    tagBorder.Child = new TextBlock { Text = cat, FontSize = 9, Foreground = (Brush)Application.Current.FindResource("AccentFillColorDefaultBrush") };
                    tagsPanel.Children.Add(tagBorder);
                }
                content.Children.Add(tagsPanel);
            }

            outer.Children.Add(content);

            // Action column
            var actionPanel = new StackPanel { VerticalAlignment = VerticalAlignment.Center, Margin = new Thickness(4, 8, 12, 8) };
            Grid.SetColumn(actionPanel, 2);

            var installBtn = new Wpf.Ui.Controls.Button
            {
                Content = _targetProfile != null ? "📥 Install" : "📥 Install",
                Appearance = Wpf.Ui.Controls.ControlAppearance.Primary,
                Padding = new Thickness(6, 6, 6, 6),
                FontSize = 10,
                Width = 76,
                Margin = new Thickness(0, 0, 0, 6)
            };
            installBtn.Click += async (s, e) =>
            {
                e.Handled = true;
                ShowModDetails(mod);
                await InstallModAsync(mod, installBtn);
            };

            var viewBtn = new Wpf.Ui.Controls.Button
            {
                Content = "🔍 Details",
                Appearance = Wpf.Ui.Controls.ControlAppearance.Secondary,
                Padding = new Thickness(6, 4, 6, 4),
                FontSize = 10,
                Width = 76
            };
            viewBtn.Click += (s, e) => { e.Handled = true; ShowModDetails(mod); };

            actionPanel.Children.Add(installBtn);
            actionPanel.Children.Add(viewBtn);
            outer.Children.Add(actionPanel);

            card.Child = outer;
            return card;
        }

        // ─────────────────────────────────────────────
        //  MOD DETAILS PANEL
        // ─────────────────────────────────────────────
        private void ShowModDetails(ModrinthMod mod)
        {
            detailsPanel.Children.Clear();

            // Large icon
            var iconBorder = new Border
            {
                Width = 100, Height = 100,
                Background = new SolidColorBrush(Color.FromRgb(25, 25, 35)),
                CornerRadius = new CornerRadius(16),
                Margin = new Thickness(0, 0, 0, 14),
                HorizontalAlignment = HorizontalAlignment.Center,
                ClipToBounds = true
            };
            var detailsImg = new Image { Stretch = Stretch.UniformToFill };
            var iconGrid = new Grid();
            iconGrid.Children.Add(new TextBlock { Text = "📦", FontSize = 44, HorizontalAlignment = HorizontalAlignment.Center, VerticalAlignment = VerticalAlignment.Center });
            iconGrid.Children.Add(detailsImg);
            iconBorder.Child = iconGrid;
            detailsPanel.Children.Add(iconBorder);

            if (!string.IsNullOrEmpty(mod.IconUrl))
                _ = Task.Run(async () =>
                {
                    try
                    {
                        var bytes = await _httpClient.GetByteArrayAsync(mod.IconUrl);
                        await Dispatcher.InvokeAsync(() =>
                        {
                            using var ms = new MemoryStream(bytes);
                            var bmp = new BitmapImage();
                            bmp.BeginInit(); bmp.StreamSource = ms; bmp.CacheOption = BitmapCacheOption.OnLoad; bmp.EndInit(); bmp.Freeze();
                            detailsImg.Source = bmp;
                        });
                    }
                    catch { }
                });

            // Title
            detailsPanel.Children.Add(new TextBlock
            {
                Text = mod.Title,
                FontSize = 16,
                FontWeight = FontWeights.Bold,
                Foreground = (Brush)Application.Current.FindResource("TextFillColorPrimaryBrush"),
                TextWrapping = TextWrapping.Wrap,
                Margin = new Thickness(0, 0, 0, 4),
                HorizontalAlignment = HorizontalAlignment.Center,
                TextAlignment = TextAlignment.Center
            });

            detailsPanel.Children.Add(new TextBlock
            {
                Text = $"by {mod.Author}",
                FontSize = 11,
                Foreground = (Brush)Application.Current.FindResource("TextFillColorSecondaryBrush"),
                Margin = new Thickness(0, 0, 0, 12),
                HorizontalAlignment = HorizontalAlignment.Center
            });

            // Stats row
            var statsCard = new Border
            {
                Background = new SolidColorBrush(Color.FromArgb(40, 80, 80, 120)),
                CornerRadius = new CornerRadius(8),
                Padding = new Thickness(12, 10, 12, 10),
                Margin = new Thickness(0, 0, 0, 12)
            };
            var statsGrid = new Grid();
            statsGrid.ColumnDefinitions.Add(new ColumnDefinition());
            statsGrid.ColumnDefinitions.Add(new ColumnDefinition());
            statsGrid.ColumnDefinitions.Add(new ColumnDefinition());

            void AddStat(int col, string label, string value, string color)
            {
                var sp = new StackPanel { HorizontalAlignment = HorizontalAlignment.Center };
                sp.Children.Add(new TextBlock { Text = value, FontSize = 14, FontWeight = FontWeights.Bold, Foreground = new SolidColorBrush((Color)ColorConverter.ConvertFromString(color)), HorizontalAlignment = HorizontalAlignment.Center });
                sp.Children.Add(new TextBlock { Text = label, FontSize = 9, Foreground = (Brush)Application.Current.FindResource("TextFillColorSecondaryBrush"), HorizontalAlignment = HorizontalAlignment.Center });
                Grid.SetColumn(sp, col);
                statsGrid.Children.Add(sp);
            }
            AddStat(0, "Downloads", FormatNumber(mod.Downloads), "#4CAF50");
            AddStat(1, "Rating", $"{mod.Rating:F1}★", "#FFB300");
            AddStat(2, "Created", mod.CreatedAt.ToString("MMM yy"), "#64B5F6");
            statsCard.Child = statsGrid;
            detailsPanel.Children.Add(statsCard);

            // Description
            detailsPanel.Children.Add(new TextBlock
            {
                Text = mod.Description,
                FontSize = 10,
                TextWrapping = TextWrapping.Wrap,
                Foreground = (Brush)Application.Current.FindResource("TextFillColorSecondaryBrush"),
                Margin = new Thickness(0, 0, 0, 14)
            });

            // Categories
            if (mod.Categories.Count > 0)
            {
                var tagsWrap = new WrapPanel { Margin = new Thickness(0, 0, 0, 14) };
                foreach (var cat in mod.Categories)
                {
                    var tb = new Border
                    {
                        Background = new SolidColorBrush(Color.FromArgb(40, 100, 100, 200)),
                        CornerRadius = new CornerRadius(6),
                        Padding = new Thickness(8, 3, 8, 3),
                        Margin = new Thickness(0, 0, 4, 4)
                    };
                    tb.Child = new TextBlock { Text = cat, FontSize = 10, Foreground = (Brush)Application.Current.FindResource("AccentFillColorDefaultBrush") };
                    tagsWrap.Children.Add(tb);
                }
                detailsPanel.Children.Add(tagsWrap);
            }

            // Game versions
            if (mod.GameVersions.Count > 0)
            {
                detailsPanel.Children.Add(new TextBlock
                {
                    Text = "🎮 Supported Versions",
                    FontSize = 11, FontWeight = FontWeights.SemiBold,
                    Foreground = (Brush)Application.Current.FindResource("TextFillColorPrimaryBrush"),
                    Margin = new Thickness(0, 0, 0, 6)
                });
                var versionsText = string.Join("  •  ", mod.GameVersions.Take(8));
                if (mod.GameVersions.Count > 8) versionsText += $" +{mod.GameVersions.Count - 8} more";
                detailsPanel.Children.Add(new TextBlock
                {
                    Text = versionsText,
                    FontSize = 10,
                    Foreground = (Brush)Application.Current.FindResource("TextFillColorSecondaryBrush"),
                    TextWrapping = TextWrapping.Wrap,
                    Margin = new Thickness(0, 0, 0, 14)
                });
            }

            detailsPanel.Children.Add(new Separator { Margin = new Thickness(0, 0, 0, 14) });

            // Install button
            var installBtn = new Wpf.Ui.Controls.Button
            {
                Content = _targetProfile != null ? $"📥 Install to '{_targetProfile.Name}'" : "📥 Install Mod",
                Appearance = Wpf.Ui.Controls.ControlAppearance.Primary,
                Padding = new Thickness(16, 10, 16, 10),
                HorizontalAlignment = HorizontalAlignment.Stretch,
                FontSize = 12,
                Margin = new Thickness(0, 0, 0, 8)
            };
            installBtn.Click += async (s, e) => await InstallModAsync(mod, installBtn);
            detailsPanel.Children.Add(installBtn);

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
                try { System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo($"https://modrinth.com/mod/{mod.Slug}") { UseShellExecute = true }); }
                catch { }
            };
            detailsPanel.Children.Add(linkBtn);
        }

        // ─────────────────────────────────────────────
        //  INSTALL
        // ─────────────────────────────────────────────
        private async Task InstallModAsync(ModrinthMod mod, Wpf.Ui.Controls.Button btn)
        {
            btn.IsEnabled = false;
            btn.Content = "⏳ Checking versions...";
            pbDownload.Visibility = Visibility.Visible;
            pbDownload.IsIndeterminate = true;

            try
            {
                var service = new ModrinthService();
                var selectedItem = cbGameVersion.SelectedItem as ComboBoxItem;
                string version = selectedItem?.Content?.ToString() ?? "1.21.4";
                if (string.Equals(version, "Any", StringComparison.OrdinalIgnoreCase))
                    version = "";

                var versions = await service.GetModVersionsAsync(mod.Id, version);

                if (versions.Count == 0)
                {
                    MessageBox.Show(
                        $"Could not find any version for '{mod.Title}'.\nThis is unusual — please try again.",
                        "No Versions Found", MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                var latestVersion = versions[0];
                var primaryFile = latestVersion.Files.Find(f => f.Primary) ?? latestVersion.Files.FirstOrDefault();
                if (primaryFile == null || string.IsNullOrEmpty(primaryFile.Url))
                {
                    MessageBox.Show("No downloadable file found for this mod version.", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                    return;
                }

                // Determine folder
                string saveFolder;
                if (_targetProfile != null)
                    saveFolder = ProfileManager.GetModsFolder(_targetProfile);
                else
                {
                    saveFolder = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), ".mclauncher", "mods", version);
                    Directory.CreateDirectory(saveFolder);
                }

                var savePath = Path.Combine(saveFolder, primaryFile.Filename);

                // Show which version we're installing (not just "fetching")
                lbStatus.Text = $"Found: {mod.Title} v{latestVersion.VersionNumber} for {latestVersion.GameVersion}";

                // Handle already installed
                if (File.Exists(savePath))
                {
                    var overwrite = MessageBox.Show(
                        $"'{primaryFile.Filename}' is already installed.\nReinstall?",
                        "Already Installed", MessageBoxButton.YesNo, MessageBoxImage.Question);
                    if (overwrite != MessageBoxResult.Yes) return;
                }

                // Download
                pbDownload.IsIndeterminate = false;
                pbDownload.Maximum = 100;
                var progress = new Progress<(long cur, long total)>(p =>
                {
                    if (p.total > 0)
                    {
                        var pct = (int)(p.cur * 100 / p.total);
                        Dispatcher.Invoke(() =>
                        {
                            btn.Content = $"⬇️ {pct}%";
                            pbDownload.Value = pct;
                            lbStatus.Text = $"Downloading {primaryFile.Filename}... {pct}%";
                        });
                    }
                });

                var success = await service.DownloadModAsync(primaryFile.Url, savePath, progress);

                if (success)
                {
                    // Register in profile
                    if (_targetProfile != null)
                    {
                        _targetProfile.InstalledMods.RemoveAll(m => m.ModId == mod.Id);
                        _targetProfile.InstalledMods.Add(new InstalledModEntry
                        {
                            ModId = mod.Id, Title = mod.Title,
                            FileName = primaryFile.Filename,
                            Version = latestVersion.VersionNumber,
                            InstalledDate = DateTime.Now
                        });
                        ProfileManager.SaveProfiles();
                    }

                    btn.Content = "✅ Installed!";
                    lbStatus.Text = $"✅ {mod.Title} v{latestVersion.VersionNumber} installed successfully!";
                    ConsolePage.Log($"[Mods] Installed: {mod.Title} v{latestVersion.VersionNumber} -> {savePath}");
                }
                else
                {
                    btn.Content = "❌ Failed";
                    lbStatus.Text = "Download failed";
                }
            }
            catch (Exception ex)
            {
                btn.Content = "❌ Error";
                lbStatus.Text = $"Error: {ex.Message}";
                MessageBox.Show($"Install error:\n{ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
            finally
            {
                pbDownload.Visibility = Visibility.Collapsed;
                await Task.Delay(2500);
                btn.IsEnabled = true;
                btn.Content = _targetProfile != null ? $"📥 Install to '{_targetProfile.Name}'" : "📥 Install Mod";
            }
        }

        // ─────────────────────────────────────────────
        //  INSTALLED MODS VIEW
        // ─────────────────────────────────────────────
        private async void btnInstalled_Click(object sender, RoutedEventArgs e)
        {
            var selectedItem = cbGameVersion.SelectedItem as ComboBoxItem;
            string version = selectedItem?.Content?.ToString() ?? "1.21.4";

            modsContainer.Children.Clear();
            _modImageControls.Clear();
            lbStatus.Text = "Loading installed mods...";
            _activeCategory = "";
            BuildCategoryChips();

            var service = new ModrinthService();
            var installed = await service.GetInstalledModsAsync();

            // Also check profile mods
            List<InstalledModEntry> profileMods = new();
            if (_targetProfile != null) profileMods = _targetProfile.InstalledMods;

            if (installed.Count == 0 && profileMods.Count == 0)
            {
                modsContainer.Children.Add(CreateEmptyState("📦 No mods installed", "Install some mods first using the search above"));
                lbStatus.Text = "No installed mods";
                return;
            }

            // Show profile mods first
            if (profileMods.Count > 0)
            {
                modsContainer.Children.Add(CreateSectionHeader($"📁 Profile: {_targetProfile!.Name} ({profileMods.Count} mods)"));
                foreach (var pm in profileMods)
                {
                    modsContainer.Children.Add(CreateInstalledProfileModRow(pm));
                }
            }

            // Show global mods
            if (installed.Count > 0)
            {
                modsContainer.Children.Add(CreateSectionHeader($"🌐 Global Mods ({installed.Count} mods)"));
                foreach (var m in installed)
                {
                    modsContainer.Children.Add(CreateInstalledModRow(m));
                }
            }

            lbStatus.Text = $"{installed.Count + profileMods.Count} installed mods";
        }

        private UIElement CreateSectionHeader(string text)
        {
            return new TextBlock
            {
                Text = text,
                FontSize = 12,
                FontWeight = FontWeights.SemiBold,
                Foreground = (Brush)Application.Current.FindResource("TextFillColorPrimaryBrush"),
                Margin = new Thickness(0, 8, 0, 8)
            };
        }

        private UIElement CreateInstalledProfileModRow(InstalledModEntry mod)
        {
            var border = new Border
            {
                Background = (Brush)Application.Current.FindResource("CardBackgroundFillColorDefaultBrush"),
                BorderBrush = (Brush)Application.Current.FindResource("CardStrokeColorDefaultBrush"),
                BorderThickness = new Thickness(1),
                CornerRadius = new CornerRadius(8),
                Padding = new Thickness(12, 8, 12, 8),
                Margin = new Thickness(0, 0, 0, 6)
            };
            var grid = new Grid();
            grid.ColumnDefinitions.Add(new ColumnDefinition());
            grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(80) });

            var info = new StackPanel();
            info.Children.Add(new TextBlock { Text = $"📦 {mod.Title}", FontSize = 12, FontWeight = FontWeights.SemiBold, Foreground = (Brush)Application.Current.FindResource("TextFillColorPrimaryBrush") });
            info.Children.Add(new TextBlock { Text = $"v{mod.Version}  •  {mod.InstalledDate:MMM dd, yyyy}", FontSize = 10, Foreground = (Brush)Application.Current.FindResource("TextFillColorSecondaryBrush") });
            Grid.SetColumn(info, 0);

            var removeBtn = new Wpf.Ui.Controls.Button
            {
                Content = "🗑️",
                Appearance = Wpf.Ui.Controls.ControlAppearance.Secondary,
                Padding = new Thickness(6, 4, 6, 4),
                FontSize = 11,
                VerticalAlignment = VerticalAlignment.Center
            };
            removeBtn.Click += (s, e) =>
            {
                _targetProfile?.InstalledMods.Remove(mod);
                ProfileManager.SaveProfiles();
                btnInstalled_Click(null!, null!);
            };
            Grid.SetColumn(removeBtn, 1);

            grid.Children.Add(info);
            grid.Children.Add(removeBtn);
            border.Child = grid;
            return border;
        }

        private UIElement CreateInstalledModRow(InstalledMod mod)
        {
            var border = new Border
            {
                Background = (Brush)Application.Current.FindResource("CardBackgroundFillColorDefaultBrush"),
                BorderBrush = (Brush)Application.Current.FindResource("CardStrokeColorDefaultBrush"),
                BorderThickness = new Thickness(1),
                CornerRadius = new CornerRadius(8),
                Padding = new Thickness(12, 8, 12, 8),
                Margin = new Thickness(0, 0, 0, 6)
            };
            var grid = new Grid();
            grid.ColumnDefinitions.Add(new ColumnDefinition());
            grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(80) });

            var info = new StackPanel();
            info.Children.Add(new TextBlock { Text = $"📦 {mod.Title}", FontSize = 12, FontWeight = FontWeights.SemiBold, Foreground = (Brush)Application.Current.FindResource("TextFillColorPrimaryBrush") });
            info.Children.Add(new TextBlock { Text = $"Installed: {mod.InstalledDate:MMM dd, yyyy}  •  {mod.FileSize / 1024}KB", FontSize = 10, Foreground = (Brush)Application.Current.FindResource("TextFillColorSecondaryBrush") });
            Grid.SetColumn(info, 0);

            var removeBtn = new Wpf.Ui.Controls.Button
            {
                Content = "🗑️",
                Appearance = Wpf.Ui.Controls.ControlAppearance.Secondary,
                Padding = new Thickness(6, 4, 6, 4),
                FontSize = 11,
                VerticalAlignment = VerticalAlignment.Center
            };
            removeBtn.Click += async (s, e) =>
            {
                var svc = new ModrinthService();
                await svc.UninstallModAsync(mod.Id);
                btnInstalled_Click(null!, null!);
            };
            Grid.SetColumn(removeBtn, 1);

            grid.Children.Add(info);
            grid.Children.Add(removeBtn);
            border.Child = grid;
            return border;
        }

        private UIElement CreateEmptyState(string title, string subtitle)
        {
            var sp = new StackPanel { HorizontalAlignment = HorizontalAlignment.Center, Margin = new Thickness(0, 60, 0, 0) };
            sp.Children.Add(new TextBlock { Text = "😴", FontSize = 48, HorizontalAlignment = HorizontalAlignment.Center, Margin = new Thickness(0, 0, 0, 12) });
            sp.Children.Add(new TextBlock { Text = title, FontSize = 16, FontWeight = FontWeights.Bold, HorizontalAlignment = HorizontalAlignment.Center, Foreground = (Brush)Application.Current.FindResource("TextFillColorPrimaryBrush") });
            sp.Children.Add(new TextBlock { Text = subtitle, FontSize = 11, HorizontalAlignment = HorizontalAlignment.Center, Foreground = (Brush)Application.Current.FindResource("TextFillColorSecondaryBrush"), Margin = new Thickness(0, 4, 0, 0) });
            return sp;
        }

        private string FormatNumber(int num)
        {
            if (num >= 1000000) return (num / 1000000.0).ToString("F1") + "M";
            if (num >= 1000) return (num / 1000.0).ToString("F1") + "K";
            return num.ToString();
        }
    }
}
