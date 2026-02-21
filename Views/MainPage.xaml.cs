using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using MClauncher.Services;
using MessageBox = System.Windows.MessageBox;
using MessageBoxButton = System.Windows.MessageBoxButton;
using MessageBoxImage = System.Windows.MessageBoxImage;

namespace MClauncher.Views
{
    public partial class MainPage : Page
    {
        private readonly ISessionService _sessionService;
        private Button? _activeButton;

        // Map: button → (icon border name, label name, accent color)
        private readonly Dictionary<Button, (string icon, string label, Color accent)> _navItems = new();

        public MainPage(ISessionService sessionService)
        {
            InitializeComponent();
            _sessionService = sessionService ?? throw new ArgumentNullException(nameof(sessionService));
        }

        private void Page_Loaded(object sender, RoutedEventArgs e)
        {
            ContentFrame.Navigate(new HomePage(_sessionService));
            SetActiveButton(btnHome);
        }

        private void MenuButton_Click(object sender, RoutedEventArgs e)
        {
            if (sender is not Button btn) return;

            SetActiveButton(btn);

            var tag = btn.Tag as string;
            if (string.IsNullOrEmpty(tag)) return;

            try
            {
                switch (tag)
                {
                    case "Home":
                        ContentFrame.Navigate(new HomePage(_sessionService));
                        break;
                    case "Profile":
                        ContentFrame.Navigate(new ProfilePage(_sessionService));
                        break;
                    case "Logs":
                        ContentFrame.Navigate(new ConsolePage());
                        break;
                    case "Settings":
                        ContentFrame.Navigate(new SettingsPage());
                        break;
                    case "Mods":
                        ContentFrame.Navigate(new ModsPage());
                        break;
                    case "Modpacks":
                        ContentFrame.Navigate(new ModpacksPage());
                        break;
                    case "Profiles":
                        ContentFrame.Navigate(new ProfilesPage(_sessionService));
                        break;
                    case "About":
                        ShowAboutInfo();
                        break;
                    case "Worlds":
                        MessageBox.Show("Coming Soon! This feature is under development.", "Feature",
                            MessageBoxButton.OK, MessageBoxImage.Information);
                        break;
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Navigation error: {ex.Message}");
                MessageBox.Show($"Error: {ex.Message}", "Navigation Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void SetActiveButton(Button btn)
        {
            // Reset previous active button
            if (_activeButton != null)
            {
                SetButtonState(_activeButton, false);
            }

            _activeButton = btn;
            SetButtonState(btn, true);
        }

        private void SetButtonState(Button btn, bool active)
        {
            // Find the template parts
            if (btn.Template.FindName("navBorder", btn) is Border border)
            {
                if (active)
                {
                    border.Background = new LinearGradientBrush(
                        Color.FromArgb(30, 98, 239, 173),
                        Color.FromArgb(15, 0, 180, 216),
                        new Point(0, 0), new Point(1, 0));
                    border.BorderBrush = new LinearGradientBrush(
                        Color.FromArgb(60, 98, 239, 173),
                        Color.FromArgb(30, 0, 180, 216),
                        new Point(0, 0), new Point(1, 0));
                    border.BorderThickness = new Thickness(1);
                }
                else
                {
                    border.Background = Brushes.Transparent;
                    border.BorderBrush = Brushes.Transparent;
                    border.BorderThickness = new Thickness(0);
                }
            }

            // Active indicator bar
            if (btn.Template.FindName("activeBar", btn) is Border bar)
            {
                bar.Opacity = active ? 1.0 : 0.0;
            }

            // Label color
            var tag = btn.Tag as string ?? "";
            var labelName = "txt" + (tag == "Logs" ? "Console" : tag);
            if (btn.Content is StackPanel sp)
            {
                foreach (var child in sp.Children)
                {
                    if (child is TextBlock tb && (string.IsNullOrEmpty(tb.Name) == false))
                    {
                        tb.Foreground = active
                            ? new LinearGradientBrush(
                                Color.FromRgb(98, 239, 173),
                                Color.FromRgb(0, 180, 216),
                                new Point(0, 0), new Point(1, 0))
                            : new SolidColorBrush(Color.FromRgb(0xAA, 0xBB, 0xCC));
                        tb.FontWeight = active ? FontWeights.SemiBold : FontWeights.Medium;
                    }
                }
            }
        }

        public Frame GetContentFrame() => ContentFrame;

        private void ShowAboutInfo()
        {
            MessageBox.Show(
                "Minecraft Launcher - Nexus\n\n" +
                "Version: 1.0.3\n" +
                "Platform: .NET 8 WPF\n" +
                "Theme: Fluent Design System\n\n" +
                "A modern, feature-rich Minecraft launcher with premium account support.\n\n" +
                "© 2025 Nexus Launcher",
                "About Nexus Launcher",
                MessageBoxButton.OK,
                MessageBoxImage.Information
            );
        }
    }
}
