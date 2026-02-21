using System;
using System.Collections.Generic;
using System.IO;
using System.Windows;
using System.Windows.Controls;
using MClauncher.Models;
using MClauncher.Services;
using CmlLib.Core;
using CmlLib.Core.ProcessBuilder;
using MessageBox = System.Windows.MessageBox;
using MessageBoxButton = System.Windows.MessageBoxButton;
using MessageBoxImage = System.Windows.MessageBoxImage;

namespace MClauncher.Views
{
    public partial class ProfilesPage : Page
    {
        private ModProfile? _selectedProfile;
        private readonly ISessionService _sessionService;
        private MinecraftLauncher? _launcher;

        public ProfilesPage(ISessionService sessionService)
        {
            InitializeComponent();
            _sessionService = sessionService;
        }

        private void Page_Loaded(object sender, RoutedEventArgs e)
        {
            RefreshProfileList();
        }

        private void RefreshProfileList()
        {
            var profiles = ProfileManager.GetProfiles();
            profileListContainer.Children.Clear();

            lbNoProfiles.Visibility = profiles.Count == 0 ? Visibility.Visible : Visibility.Collapsed;
            if (profiles.Count == 0)
            {
                profileListContainer.Children.Add(lbNoProfiles);
                return;
            }

            foreach (var profile in profiles)
            {
                var card = CreateProfileListCard(profile);
                profileListContainer.Children.Add(card);
            }
        }

        private Border CreateProfileListCard(ModProfile profile)
        {
            var border = new Border
            {
                Background = _selectedProfile?.Id == profile.Id
                    ? (System.Windows.Media.Brush)Application.Current.FindResource("AccentFillColorTertiaryBrush")
                    : (System.Windows.Media.Brush)Application.Current.FindResource("CardBackgroundFillColorDefaultBrush"),
                BorderBrush = (System.Windows.Media.Brush)Application.Current.FindResource("CardStrokeColorDefaultBrush"),
                BorderThickness = new Thickness(1),
                CornerRadius = new CornerRadius(8),
                Padding = new Thickness(14, 12, 14, 12),
                Margin = new Thickness(0, 0, 0, 8),
                Cursor = System.Windows.Input.Cursors.Hand
            };

            var grid = new Grid();
            grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(44) });
            grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });

            var iconBg = new Border
            {
                Width = 36, Height = 36,
                Background = (System.Windows.Media.Brush)Application.Current.FindResource("AccentFillColorDefaultBrush"),
                CornerRadius = new CornerRadius(8),
                Margin = new Thickness(0, 0, 10, 0)
            };
            iconBg.Child = new TextBlock { Text = profile.Icon, FontSize = 18, HorizontalAlignment = HorizontalAlignment.Center, VerticalAlignment = VerticalAlignment.Center };
            Grid.SetColumn(iconBg, 0);

            var info = new StackPanel();
            info.Children.Add(new TextBlock
            {
                Text = profile.Name,
                FontSize = 13,
                FontWeight = FontWeights.SemiBold,
                Foreground = (System.Windows.Media.Brush)Application.Current.FindResource("TextFillColorPrimaryBrush")
            });
            var subText = profile.IsModpackProfile
                ? $"{profile.GameVersion} • {profile.Loader} • Modpack"
                : $"{profile.GameVersion} • {profile.Loader} • {profile.InstalledMods.Count} mods";

            info.Children.Add(new TextBlock
            {
                Text = subText,
                FontSize = 10,
                Foreground = (System.Windows.Media.Brush)Application.Current.FindResource("TextFillColorSecondaryBrush"),
                Margin = new Thickness(0, 2, 0, 0)
            });
            Grid.SetColumn(info, 1);

            grid.Children.Add(iconBg);
            grid.Children.Add(info);
            border.Child = grid;

            border.MouseLeftButtonUp += (s, e) => SelectProfile(profile);
            return border;
        }

        private void SelectProfile(ModProfile profile)
        {
            _selectedProfile = profile;
            RefreshProfileList();
            ShowProfileDetails(profile);
        }

        private void ShowProfileDetails(ModProfile profile)
        {
            noSelectionPanel.Visibility = Visibility.Collapsed;
            detailScrollViewer.Visibility = Visibility.Visible;

            lbProfileIcon.Text = profile.Icon;
            lbProfileName.Text = profile.Name;
            lbProfileMeta.Text = $"Created: {profile.CreatedDate:MMM dd, yyyy}"
                + (string.IsNullOrEmpty(profile.Description) ? "" : $" • {profile.Description}")
                + (profile.IsModpackProfile ? " • Source: Modpacks" : "");

            lbDetailVersion.Text = profile.GameVersion;
            lbDetailLoader.Text = profile.Loader.ToString();
            lbDetailModCount.Text = profile.InstalledMods.Count.ToString();

            // Installed mods list
            installedModsList.Children.Clear();
            if (profile.InstalledMods.Count == 0)
            {
                installedModsList.Children.Add(lbNoMods);
            }
            else
            {
                foreach (var mod in profile.InstalledMods)
                {
                    var modRow = CreateInstalledModRow(mod, profile);
                    installedModsList.Children.Add(modRow);
                }
            }
        }

        private UIElement CreateInstalledModRow(InstalledModEntry mod, ModProfile profile)
        {
            var border = new Border
            {
                Background = (System.Windows.Media.Brush)Application.Current.FindResource("LayerFillColorAltBrush"),
                CornerRadius = new CornerRadius(6),
                Padding = new Thickness(10, 8, 10, 8),
                Margin = new Thickness(0, 0, 0, 6)
            };

            var grid = new Grid();
            grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
            grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(80) });

            var info = new StackPanel();
            info.Children.Add(new TextBlock
            {
                Text = mod.Title,
                FontSize = 12,
                FontWeight = FontWeights.SemiBold,
                Foreground = (System.Windows.Media.Brush)Application.Current.FindResource("TextFillColorPrimaryBrush")
            });
            info.Children.Add(new TextBlock
            {
                Text = $"v{mod.Version}  •  {mod.InstalledDate:MMM dd, yyyy}",
                FontSize = 10,
                Foreground = (System.Windows.Media.Brush)Application.Current.FindResource("TextFillColorSecondaryBrush")
            });
            Grid.SetColumn(info, 0);

            var removeBtn = new Wpf.Ui.Controls.Button
            {
                Content = "🗑️",
                Appearance = Wpf.Ui.Controls.ControlAppearance.Secondary,
                Padding = new Thickness(8, 4, 8, 4),
                FontSize = 11,
                VerticalAlignment = VerticalAlignment.Center
            };
            removeBtn.Click += (s, e) =>
            {
                profile.InstalledMods.Remove(mod);
                ProfileManager.SaveProfiles();
                ShowProfileDetails(profile);
                RefreshProfileList();
            };
            Grid.SetColumn(removeBtn, 1);

            grid.Children.Add(info);
            grid.Children.Add(removeBtn);
            border.Child = grid;
            return border;
        }

        // ---- New Profile Dialog ----
        private void btnNewProfile_Click(object sender, RoutedEventArgs e)
        {
            tbProfileName.Text = "";
            tbProfileDesc.Text = "";
            cbProfileVersion.SelectedIndex = 0;
            cbProfileLoader.SelectedIndex = 0;
            createProfileOverlay.Visibility = Visibility.Visible;
        }

        private void btnCancelCreate_Click(object sender, RoutedEventArgs e)
        {
            createProfileOverlay.Visibility = Visibility.Collapsed;
        }

        private void btnConfirmCreate_Click(object sender, RoutedEventArgs e)
        {
            var name = tbProfileName.Text.Trim();
            if (string.IsNullOrEmpty(name))
            {
                MessageBox.Show("Please enter a profile name.", "Validation", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            var version = (cbProfileVersion.SelectedItem as ComboBoxItem)?.Content?.ToString() ?? "1.21";
            var loaderTag = (cbProfileLoader.SelectedItem as ComboBoxItem)?.Tag?.ToString() ?? "Vanilla";
            var loader = Enum.TryParse<ModLoader>(loaderTag, out var l) ? l : ModLoader.Vanilla;
            var desc = tbProfileDesc.Text.Trim();

            string icon = loader switch
            {
                ModLoader.Fabric => "🧵",
                ModLoader.Forge => "⚒️",
                ModLoader.Quilt => "🪡",
                ModLoader.NeoForge => "🔥",
                _ => "🎮"
            };

            var profile = ProfileManager.CreateProfile(name, version, loader, desc);
            profile.Icon = icon;
            ProfileManager.SaveProfiles();

            createProfileOverlay.Visibility = Visibility.Collapsed;
            RefreshProfileList();
            SelectProfile(profile);
        }

        // ---- Delete Profile ----
        private void btnDeleteProfile_Click(object sender, RoutedEventArgs e)
        {
            if (_selectedProfile == null) return;
            var result = MessageBox.Show(
                $"Are you sure you want to delete profile '{_selectedProfile.Name}'?\nThis will NOT delete mod files.",
                "Delete Profile",
                MessageBoxButton.YesNo,
                MessageBoxImage.Warning);

            if (result == MessageBoxResult.Yes)
            {
                ProfileManager.DeleteProfile(_selectedProfile.Id);
                _selectedProfile = null;
                noSelectionPanel.Visibility = Visibility.Visible;
                detailScrollViewer.Visibility = Visibility.Collapsed;
                RefreshProfileList();
            }
        }

        // ---- Launch with Profile ----
        private async void btnLaunchProfile_Click(object sender, RoutedEventArgs e)
        {
            if (_selectedProfile == null) return;

            var session = _sessionService.GetSession();
            if (session == null)
            {
                MessageBox.Show("Please log in first.", "Authentication", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            btnLaunchProfile.IsEnabled = false;
            try
            {
                if (_selectedProfile.IsModpackProfile && !string.IsNullOrWhiteSpace(_selectedProfile.ModpackFilePath))
                {
                    if (!File.Exists(_selectedProfile.ModpackFilePath))
                    {
                        MessageBox.Show(
                            "This modpack profile points to a missing .mrpack file. Re-download the modpack from Modpacks page.",
                            "Modpack file missing",
                            MessageBoxButton.OK,
                            MessageBoxImage.Warning);
                        return;
                    }
                }

                var minecraftPath = new MinecraftPath();
                _launcher = new MinecraftLauncher(minecraftPath);

                var versionName = _selectedProfile.GameVersion;
                await _launcher.InstallAsync(versionName);

                var modsDir = ProfileManager.GetModsFolder(_selectedProfile);

                var process = await _launcher.CreateProcessAsync(versionName, new MLaunchOption
                {
                    MaximumRamMb = Models.SettingsManager.Current.RamAllocation,
                    Session = session
                });

                process.StartInfo.UseShellExecute = false;
                process.StartInfo.RedirectStandardOutput = true;
                process.StartInfo.RedirectStandardError = true;
                process.Start();
                process.BeginOutputReadLine();
                process.BeginErrorReadLine();

                _selectedProfile.LastPlayed = DateTime.Now;
                _selectedProfile.PlayCount++;
                ProfileManager.SaveProfiles();

                Window.GetWindow(this)!.WindowState = WindowState.Minimized;
                var profileType = _selectedProfile.IsModpackProfile ? "ModpackProfile" : "Profile";
                ConsolePage.Log($"[{profileType}: {_selectedProfile.Name}] Game launched - {versionName}");
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Failed to launch:\n{ex.Message}", "Launch Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
            finally
            {
                btnLaunchProfile.IsEnabled = true;
            }
        }

        // ---- Open Mods Page for Profile ----
        private void btnOpenModsForProfile_Click(object sender, RoutedEventArgs e)
        {
            if (_selectedProfile == null) return;
            // Navigate the parent frame to ModsPage with profile context
            var frame = FindParentFrame();
            if (frame != null)
            {
                frame.Navigate(new ModsPage(_selectedProfile));
            }
            else
            {
                MessageBox.Show(
                    "Go to the Mods tab to search and install mods.\nMods will be installed to the global mods folder.", 
                    "Add Mods", MessageBoxButton.OK, MessageBoxImage.Information);
            }
        }

        // Walk up visual tree to find the hosting Frame
        private Frame? FindParentFrame()
        {
            DependencyObject? current = this;
            while (current != null)
            {
                if (current is Frame frame) return frame;
                current = System.Windows.Media.VisualTreeHelper.GetParent(current);
            }
            return null;
        }
    }
}
