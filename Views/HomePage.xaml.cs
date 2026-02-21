using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Net.Http;
using System.Text.RegularExpressions;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using Wpf.Ui.Controls;
using CmlLib.Core;
using CmlLib.Core.ProcessBuilder;
using MClauncher.Models;
using MClauncher.Services;
using MessageBox = System.Windows.MessageBox;
using MessageBoxButton = System.Windows.MessageBoxButton;
using MessageBoxImage = System.Windows.MessageBoxImage;
using TextBlock = System.Windows.Controls.TextBlock;
using Border = System.Windows.Controls.Border;
using Button = System.Windows.Controls.Button;

namespace MClauncher.Views
{
    public partial class HomePage : Page
    {
        private MinecraftLauncher? _launcher;
        private MinecraftPath? _path;
        private readonly ISessionService _sessionService;
        private bool _showSnapshots = false;
        private static readonly HttpClient _newsImageHttpClient = new()
        {
            Timeout = TimeSpan.FromSeconds(20)
        };

        public HomePage(ISessionService sessionService)
        {
            InitializeComponent();
            _sessionService = sessionService ?? throw new ArgumentNullException(nameof(sessionService));
        }

        private async void UiPage_Loaded(object sender, RoutedEventArgs e)
        {
            var session = _sessionService.GetSession();
            if (session == null)
            {
                HandleAuthenticationError();
                return;
            }

            lbWelcome.Text = $"Welcome, {session.Username}!";
            lbSessionType.Text = _sessionService.IsPremium ? "🔐 Premium (Microsoft)" : "👤 Offline Mode";

            UpdateStatistics();

            // Fetch real Minecraft news asynchronously
            _ = LoadRealNewsAsync();

            await InitializeLauncher();
        }

        private void UpdateStatistics()
        {
            try
            {
                lbPlayTime.Text = "24h 30m";
                lbVersionCount.Text = "Loading...";
                lbLastPlayed.Text = "Today";

                var ram = SettingsManager.Current.RamAllocation;
                pbRAM.Value = Math.Min(100, (ram / 8192.0) * 100);
                lbRAM.Text = $"{ram} MB";

                try
                {
                    var psi = new ProcessStartInfo("java", "-version")
                    { RedirectStandardError = true, UseShellExecute = false, CreateNoWindow = true };
                    Process.Start(psi);
                    lbJavaVersion.Text = "✅ Runtime Available";
                }
                catch { lbJavaVersion.Text = "⚠️ Java Not Found"; }

                lbSystemStatus.Text = "✅ All systems ready";
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Stats error: {ex.Message}");
            }
        }

        // ─── Real News from Minecraft.net ────────────────────────────────────────

        private async System.Threading.Tasks.Task LoadRealNewsAsync()
        {
            try
            {
                NewsLoadingBar.Visibility = Visibility.Visible;
                NewsErrorText.Visibility = Visibility.Collapsed;
                NewsContainer.Items.Clear();

                var news = await MinecraftNewsService.FetchNewsAsync(6);

                NewsLoadingBar.Visibility = Visibility.Collapsed;

                if (news.Count == 0)
                {
                    ShowNewsError("No news articles found.");
                    return;
                }

                foreach (var item in news)
                {
                    var card = CreateNewsCard(item);
                    NewsContainer.Items.Add(card);
                }
            }
            catch (Exception ex)
            {
                NewsLoadingBar.Visibility = Visibility.Collapsed;
                ShowNewsError($"Could not load news: {ex.Message}");
            }
        }

        private void ShowNewsError(string msg)
        {
            NewsErrorText.Text = $"⚠️ {msg}";
            NewsErrorText.Visibility = Visibility.Visible;
        }

        private Border CreateNewsCard(MinecraftNewsItem item)
        {
            var accent = GetCategoryColor(item.Category);
            
            var outerBorder = new Border
            {
                CornerRadius = new CornerRadius(12),
                Margin = new Thickness(0, 0, 8, 8),
                Cursor = Cursors.Hand,
                BorderThickness = new Thickness(1),
                BorderBrush = new SolidColorBrush(Color.FromArgb(30, accent.R, accent.G, accent.B))
            };

            outerBorder.Background = new LinearGradientBrush(
                Color.FromArgb(180, 18, 22, 38),
                Color.FromArgb(180, 12, 16, 28),
                new Point(0, 0), new Point(1, 1));

            outerBorder.MouseLeftButtonDown += (s, e) =>
            {
                if (!string.IsNullOrEmpty(item.Url))
                    Process.Start(new ProcessStartInfo(item.Url) { UseShellExecute = true });
            };

            var card = new StackPanel { Margin = new Thickness(0) };

            // Image area (colored placeholder with emoji)
            var imgBorder = new Border
            {
                Height = 100,
                CornerRadius = new CornerRadius(12, 12, 0, 0),
                ClipToBounds = true
            };
            imgBorder.Background = new LinearGradientBrush(
                Color.FromArgb(60, accent.R, accent.G, accent.B),
                Color.FromArgb(20, accent.R, accent.G, accent.B),
                new Point(0, 0), new Point(1, 1));

            // Always show fallback emoji first, then try loading remote image asynchronously.
            imgBorder.Child = new TextBlock
            {
                Text = GetCategoryEmoji(item.Category),
                FontSize = 36,
                HorizontalAlignment = HorizontalAlignment.Center,
                VerticalAlignment = VerticalAlignment.Center
            };

            if (!string.IsNullOrWhiteSpace(item.ImageUrl))
            {
                _ = LoadNewsImageAsync(item.ImageUrl, imgBorder);
            }

            card.Children.Add(imgBorder);

            // Content area
            var contentPanel = new StackPanel { Margin = new Thickness(14, 12, 14, 14) };

            // Category + date row
            var topRow = new Grid();
            var catBorder = new Border
            {
                Background = new SolidColorBrush(Color.FromArgb(35, accent.R, accent.G, accent.B)),
                BorderBrush = new SolidColorBrush(Color.FromArgb(60, accent.R, accent.G, accent.B)),
                BorderThickness = new Thickness(1),
                CornerRadius = new CornerRadius(5),
                Padding = new Thickness(7, 3, 7, 3),
                HorizontalAlignment = HorizontalAlignment.Left
            };
            catBorder.Child = new TextBlock
            {
                Text = (item.Category ?? "News").ToUpper(),
                FontSize = 9,
                FontWeight = FontWeights.Bold,
                Foreground = new SolidColorBrush(accent)
            };
            topRow.Children.Add(catBorder);

            var dateText = new TextBlock
            {
                Text = item.Date,
                FontSize = 10,
                Foreground = new SolidColorBrush(Color.FromRgb(0x44, 0x55, 0x77)),
                HorizontalAlignment = HorizontalAlignment.Right,
                VerticalAlignment = VerticalAlignment.Center
            };
            topRow.Children.Add(dateText);
            contentPanel.Children.Add(topRow);

            // Title
            contentPanel.Children.Add(new TextBlock
            {
                Text = item.Title,
                FontSize = 13,
                FontWeight = FontWeights.SemiBold,
                Foreground = new SolidColorBrush(Colors.White),
                TextWrapping = TextWrapping.Wrap,
                Margin = new Thickness(0, 8, 0, 6),
                MaxHeight = 42
            });

            // Description
            if (!string.IsNullOrEmpty(item.Description))
            {
                contentPanel.Children.Add(new TextBlock
                {
                    Text = item.Description,
                    FontSize = 11,
                    Foreground = new SolidColorBrush(Color.FromRgb(0x55, 0x66, 0x88)),
                    TextWrapping = TextWrapping.Wrap,
                    MaxHeight = 44
                });
            }

            card.Children.Add(contentPanel);
            outerBorder.Child = card;
            return outerBorder;
        }

        private static Color GetCategoryColor(string category)
        {
            return (category?.ToLower() ?? "") switch
            {
                "update" or "java" => Color.FromRgb(0x62, 0xEF, 0xAD),
                "event" => Color.FromRgb(0xFF, 0x98, 0x00),
                "video" => Color.FromRgb(0x7C, 0x4D, 0xFF),
                "marketplace" or "store" => Color.FromRgb(0x00, 0xB4, 0xD8),
                _ => Color.FromRgb(0x62, 0xEF, 0xAD)
            };
        }

        private static string GetCategoryEmoji(string category)
        {
            return (category?.ToLower() ?? "") switch
            {
                "update" or "java" => "📦",
                "event" => "🎉",
                "video" => "🎬",
                "marketplace" or "store" => "🛒",
                _ => "📰"
            };
        }

        private async System.Threading.Tasks.Task LoadNewsImageAsync(string imageUrl, Border targetBorder)
        {
            try
            {
                var req = new HttpRequestMessage(HttpMethod.Get, imageUrl);
                req.Headers.TryAddWithoutValidation("User-Agent", "NexusLauncher/1.0");

                using var resp = await _newsImageHttpClient.SendAsync(req);
                if (!resp.IsSuccessStatusCode) return;

                var bytes = await resp.Content.ReadAsByteArrayAsync();
                await Dispatcher.InvokeAsync(() =>
                {
                    try
                    {
                        using var ms = new System.IO.MemoryStream(bytes);
                        var bmp = new BitmapImage();
                        bmp.BeginInit();
                        bmp.CacheOption = BitmapCacheOption.OnLoad;
                        bmp.CreateOptions = BitmapCreateOptions.IgnoreColorProfile;
                        bmp.StreamSource = ms;
                        bmp.DecodePixelWidth = 420;
                        bmp.EndInit();
                        bmp.Freeze();

                        targetBorder.Child = new System.Windows.Controls.Image
                        {
                            Source = bmp,
                            Stretch = Stretch.UniformToFill,
                            Opacity = 0.95
                        };
                    }
                    catch
                    {
                        // Keep emoji fallback if image decode fails
                    }
                });
            }
            catch
            {
                // Keep emoji fallback if download fails
            }
        }

        // ─── Version Filter ───────────────────────────────────────────────────────

        private void FilterRelease_Click(object sender, MouseButtonEventArgs e)
        {
            _showSnapshots = false;
            filterRelease.Background = new SolidColorBrush(Color.FromArgb(0x25, 0x62, 0xEF, 0xAD));
            filterRelease.BorderBrush = new SolidColorBrush(Color.FromArgb(0x50, 0x62, 0xEF, 0xAD));
            ((TextBlock)filterRelease.Child).Foreground = new SolidColorBrush(Color.FromRgb(0x62, 0xEF, 0xAD));
            filterSnapshot.Background = new SolidColorBrush(Color.FromArgb(0x10, 0xFF, 0xFF, 0xFF));
            filterSnapshot.BorderBrush = new SolidColorBrush(Color.FromArgb(0x18, 0xFF, 0xFF, 0xFF));
            ((TextBlock)filterSnapshot.Child).Foreground = new SolidColorBrush(Color.FromRgb(0x66, 0x77, 0x99));
            _ = InitializeLauncher();
        }

        private void FilterSnapshot_Click(object sender, MouseButtonEventArgs e)
        {
            _showSnapshots = true;
            filterSnapshot.Background = new SolidColorBrush(Color.FromArgb(0x25, 0xFF, 0x98, 0x00));
            filterSnapshot.BorderBrush = new SolidColorBrush(Color.FromArgb(0x50, 0xFF, 0x98, 0x00));
            ((TextBlock)filterSnapshot.Child).Foreground = new SolidColorBrush(Color.FromRgb(0xFF, 0x98, 0x00));
            filterRelease.Background = new SolidColorBrush(Color.FromArgb(0x10, 0xFF, 0xFF, 0xFF));
            filterRelease.BorderBrush = new SolidColorBrush(Color.FromArgb(0x18, 0xFF, 0xFF, 0xFF));
            ((TextBlock)filterRelease.Child).Foreground = new SolidColorBrush(Color.FromRgb(0x66, 0x77, 0x99));
            _ = InitializeLauncher();
        }

        // ─── Launcher ─────────────────────────────────────────────────────────────

        private async System.Threading.Tasks.Task InitializeLauncher()
        {
            try
            {
                lbStatus.Text = "Loading versions...";
                cbVersions.Items.Clear();

                _path = new MinecraftPath();
                _launcher = new MinecraftLauncher(_path);

                var versions = await _launcher.GetAllVersionsAsync();
                int count = 0;
                foreach (var v in versions)
                {
                    if (!_showSnapshots && v.Type == "snapshot") continue;
                    cbVersions.Items.Add(v.Name);
                    count++;
                }

                lbVersionCount.Text = count.ToString();
                if (cbVersions.Items.Count > 0)
                    cbVersions.SelectedIndex = cbVersions.Items.Count - 1;

                lbStatus.Text = "✅ Ready to launch";

                if (cbVersions.SelectedItem != null)
                    lbLastVersion.Text = cbVersions.SelectedItem.ToString();
            }
            catch (Exception ex)
            {
                lbStatus.Text = $"⚠️ Error: {ex.Message}";
            }
        }

        private async void btnLaunch_Click(object sender, RoutedEventArgs e)
        {
            if (_launcher == null || cbVersions.SelectedItem == null)
            { lbStatus.Text = "⚠️ Please select a version"; return; }

            var session = _sessionService.GetSession();
            if (session == null) { HandleAuthenticationError(); return; }

            btnLaunch.IsEnabled = false;
            var versionName = cbVersions.SelectedItem.ToString()!;
            try { await LaunchGame(session, versionName); }
            finally { btnLaunch.IsEnabled = true; pbProgress.Value = 0; }
        }

        private async System.Threading.Tasks.Task LaunchGame(CmlLib.Core.Auth.MSession session, string versionName)
        {
            try
            {
                LogGameStatus($"=== Starting Launch: {versionName} ===");

                _launcher!.FileProgressChanged += (s, ev) =>
                {
                    Dispatcher.Invoke(() =>
                    {
                        lbStatus.Text = $"⬇️ Downloading: {ev.Name} ({ev.ProgressedTasks}/{ev.TotalTasks})";
                        pbProgress.Maximum = ev.TotalTasks;
                        pbProgress.Value = ev.ProgressedTasks;
                        if (ev.ProgressedTasks % 10 == 0 || ev.ProgressedTasks == ev.TotalTasks)
                            LogGameStatus($"Progress: {ev.Name} ({ev.ProgressedTasks}/{ev.TotalTasks})");
                    });
                };

                lbStatus.Text = "🔍 Checking files...";
                await _launcher.InstallAsync(versionName);

                lbStatus.Text = "🚀 Launching...";
                var process = await _launcher.CreateProcessAsync(versionName, new MLaunchOption
                {
                    MaximumRamMb = SettingsManager.Current.RamAllocation,
                    Session = session
                });

                process.StartInfo.UseShellExecute = false;
                process.StartInfo.RedirectStandardOutput = true;
                process.StartInfo.RedirectStandardError = true;
                process.OutputDataReceived += (s, a) => { if (a.Data != null) LogGameStatus(a.Data); };
                process.ErrorDataReceived += (s, a) => { if (a.Data != null) LogGameStatus($"[ERR] {a.Data}"); };

                process.Start();
                process.BeginOutputReadLine();
                process.BeginErrorReadLine();

                lbStatus.Text = "🎮 Game started!";
                Window.GetWindow(this)!.WindowState = WindowState.Minimized;
            }
            catch (Exception ex)
            {
                LogGameStatus($"[ERROR] Launch failed: {ex.Message}");
                lbStatus.Text = $"❌ Launch failed: {ex.Message}";
                MessageBox.Show($"Failed to launch game:\n{ex.Message}", "Launch Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void LogGameStatus(string message) => ConsolePage.Log(message);

        // ─── Event Handlers ───────────────────────────────────────────────────────

        private void btnLogout_Click(object sender, RoutedEventArgs e)
            => (Window.GetWindow(this) as MainWindow)?.Logout();

        private async void btnRefresh_Click(object sender, RoutedEventArgs e)
        {
            btnRefresh.IsEnabled = false;
            lbStatus.Text = "🔄 Refreshing...";
            try { _launcher = null; cbVersions.Items.Clear(); await InitializeLauncher(); }
            finally { btnRefresh.IsEnabled = true; }
        }

        private void btnOpenSite_Click(object sender, MouseButtonEventArgs e)
            => Process.Start(new ProcessStartInfo("https://www.minecraft.net") { UseShellExecute = true });

        private void btnOpenFolder_Click(object sender, MouseButtonEventArgs e)
        {
            var path = System.IO.Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), ".minecraft");
            if (System.IO.Directory.Exists(path))
                Process.Start("explorer.exe", path);
            else
                MessageBox.Show("Minecraft folder not found.", "Not Found", MessageBoxButton.OK, MessageBoxImage.Information);
        }

        private void btnCopyLogs_Click(object sender, MouseButtonEventArgs e)
        {
            var logs = ConsolePage.GetAllLogs();
            if (string.IsNullOrEmpty(logs))
                MessageBox.Show("No logs available yet.", "Logs", MessageBoxButton.OK, MessageBoxImage.Information);
            else
            {
                Clipboard.SetText(logs);
                MessageBox.Show("Logs copied to clipboard!", "Success", MessageBoxButton.OK, MessageBoxImage.Information);
            }
        }

        private void btnQuickLaunch_Click(object sender, MouseButtonEventArgs e)
        {
            if (cbVersions.SelectedItem == null)
            { MessageBox.Show("No version selected.", "Quick Launch", MessageBoxButton.OK, MessageBoxImage.Warning); return; }
            btnLaunch_Click(sender, new RoutedEventArgs());
        }

        private void btnLearnMore_Click(object sender, MouseButtonEventArgs e)
            => Process.Start(new ProcessStartInfo("https://www.minecraft.net/en-us/article/minecraft-java-edition-1-21-4") { UseShellExecute = true });

        private void btnViewAllNews_Click(object sender, MouseButtonEventArgs e)
            => Process.Start(new ProcessStartInfo("https://www.minecraft.net/en-us/articles") { UseShellExecute = true });

        private void HandleAuthenticationError()
        {
            lbStatus.Text = "🔒 Authentication error.";
            MessageBox.Show("Your session has expired. Please log in again.", "Session Expired", MessageBoxButton.OK, MessageBoxImage.Warning);
            (Window.GetWindow(this) as MainWindow)?.Logout();
        }
    }
}
