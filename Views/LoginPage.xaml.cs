using System;
using System.Net.Http;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Animation;
using Wpf.Ui.Controls;
using CmlLib.Core.Auth;
using CmlLib.Core.Auth.Microsoft;
using MClauncher.Services;
using MClauncher.Models;
using MessageBox = System.Windows.MessageBox;
using MessageBoxButton = System.Windows.MessageBoxButton;
using MessageBoxImage = System.Windows.MessageBoxImage;

namespace MClauncher.Views
{
    public partial class LoginPage : Page
    {
        private readonly HttpClient _httpClient;
        private readonly ISessionService _sessionService;
        private readonly MainWindow _mainWindow;

        public LoginPage(ISessionService sessionService, MainWindow mainWindow)
        {
            InitializeComponent();
            _sessionService = sessionService ?? throw new ArgumentNullException(nameof(sessionService));
            _mainWindow = mainWindow ?? throw new ArgumentNullException(nameof(mainWindow));
            _httpClient = new HttpClient();
        }

        private async void Page_Loaded(object sender, RoutedEventArgs e)
        {
            // Start all animations — use safe 'as' casts to avoid NullReferenceException
            try
            {
                (Resources["FloatAnim1"] as Storyboard)?.Begin(this);
                (Resources["FloatAnim2"] as Storyboard)?.Begin(this);
                (Resources["FloatAnim3"] as Storyboard)?.Begin(this);
                (Resources["FloatAnim4"] as Storyboard)?.Begin(this);
                (Resources["FloatAnim5"] as Storyboard)?.Begin(this);
                (Resources["CardEntrance"] as Storyboard)?.Begin(this);
                (Resources["TitleEntrance"] as Storyboard)?.Begin(this);
                (Resources["GlowPulse"] as Storyboard)?.Begin(this);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Animation error: {ex.Message}");
            }

            chkRememberMe.IsChecked = SettingsManager.Current.RememberMe;

            if (!string.IsNullOrWhiteSpace(SettingsManager.Current.RememberedOfflineUsername))
            {
                txtUsername.Text = SettingsManager.Current.RememberedOfflineUsername;
                txtPlaceholder.Visibility = Visibility.Collapsed;
            }

            if (SettingsManager.Current.RememberMe
                && string.Equals(SettingsManager.Current.LastLoginMethod, "offline", StringComparison.OrdinalIgnoreCase)
                && !string.IsNullOrWhiteSpace(SettingsManager.Current.RememberedOfflineUsername))
            {
                lbStatus.Text = "🔐 Auto-login with remembered offline profile...";
                lbStatus.Foreground = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#62EFAD"));

                await System.Threading.Tasks.Task.Delay(250);

                var autoSession = MSession.CreateOfflineSession(SettingsManager.Current.RememberedOfflineUsername);
                CompleteLogin(autoSession);
            }
        }

        private void txtUsername_GotFocus(object sender, RoutedEventArgs e)
        {
            txtPlaceholder.Visibility = Visibility.Collapsed;
        }

        private void txtUsername_LostFocus(object sender, RoutedEventArgs e)
        {
            if (string.IsNullOrEmpty(txtUsername.Text))
                txtPlaceholder.Visibility = Visibility.Visible;
        }

        private async void btnLoginMicrosoft_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                btnLoginMicrosoft.IsEnabled = false;
                lbStatus.Text = "🔄 Opening Microsoft authentication...";
                lbStatus.Foreground = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#62EFAD"));

                var loginHandler = new JELoginHandlerBuilder()
                    .WithHttpClient(_httpClient)
                    .Build();

                var session = await loginHandler.AuthenticateInteractively();
                CompleteLogin(session);
            }
            catch (OperationCanceledException)
            {
                lbStatus.Text = "Login was cancelled.";
                lbStatus.Foreground = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#FF9800"));
            }
            catch (Exception ex)
            {
                // Show inner exception detail for NullReferenceException cases
                var msg = ex.InnerException?.Message ?? ex.Message;
                lbStatus.Text = $"❌ {msg}";
                lbStatus.Foreground = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#FF5252"));
                System.Diagnostics.Debug.WriteLine($"[Login] {ex}");
            }
            finally
            {
                btnLoginMicrosoft.IsEnabled = true;
            }
        }

        private void btnLoginOffline_Click(object sender, RoutedEventArgs e)
        {
            if (string.IsNullOrWhiteSpace(txtUsername.Text))
            {
                lbStatus.Text = "⚠️ Please enter a username to continue";
                lbStatus.Foreground = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#FF9800"));
                return;
            }

            var session = MSession.CreateOfflineSession(txtUsername.Text);
            CompleteLogin(session);
        }

        private void CompleteLogin(MSession session)
        {
            try
            {
                var rememberMe = chkRememberMe.IsChecked == true;
                var isOffline = string.Equals(session.AccessToken, "0", StringComparison.Ordinal);

                SettingsManager.Current.RememberMe = rememberMe;
                SettingsManager.Current.LastLoginUsername = session.Username ?? string.Empty;
                SettingsManager.Current.LastLoginMethod = isOffline ? "offline" : "microsoft";

                if (rememberMe && isOffline)
                    SettingsManager.Current.RememberedOfflineUsername = session.Username ?? string.Empty;
                else if (!rememberMe)
                    SettingsManager.Current.RememberedOfflineUsername = string.Empty;

                SettingsManager.Save();

                _sessionService.SetSession(session);
                _mainWindow.NavigateToMain();
            }
            catch (Exception ex)
            {
                lbStatus.Text = $"❌ Error completing login: {ex.Message}";
                lbStatus.Foreground = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#FF5252"));
            }
        }

        public void OnPageUnloaded()
        {
            _httpClient?.Dispose();
        }
    }
}
