using System;
using System.Windows;
using System.Windows.Controls;
using Wpf.Ui.Controls;
using MClauncher.Services;
using MessageBox = System.Windows.MessageBox;
using MessageBoxButton = System.Windows.MessageBoxButton;
using MessageBoxImage = System.Windows.MessageBoxImage;

namespace MClauncher.Views
{
    public partial class ProfilePage : Page
    {
        private readonly ISessionService _sessionService;
        private readonly ISkinViewerService _skinViewerService;

        public ProfilePage(ISessionService sessionService)
        {
            InitializeComponent();
            _sessionService = sessionService ?? throw new ArgumentNullException(nameof(sessionService));
            _skinViewerService = new SkinViewerService();
        }

        private async void Page_Loaded(object sender, RoutedEventArgs e)
        {
            var session = _sessionService.GetSession();
            if (session == null)
            {
                HandleAuthenticationError();
                return;
            }

            // Update profile information
            lbUsername.Text = session.Username ?? "Unknown";
            lbSessionType.Text = _sessionService.IsPremium ? "🔐 Premium (Microsoft)" : "👤 Offline Mode";
            lbAccountCreated.Text = DateTime.Now.ToString("MMM yyyy");
            lbLastUpdated.Text = "Just now";

            // Initialize 3D skin viewer
            await InitializeSkinViewer(session.Username ?? "char");
        }

        private async System.Threading.Tasks.Task InitializeSkinViewer(string username)
        {
            try
            {
                // Ensure WebView2 runtime is available
                await webView.EnsureCoreWebView2Async();

                // Use a safe username (minotar.net requires valid chars)
                string safeName = string.IsNullOrWhiteSpace(username) ? "char" : username;
                // Sanitize for URL
                safeName = Uri.EscapeDataString(safeName);

                string skinUrl     = $"https://minotar.net/skin/{safeName}";
                string fallbackUrl = $"https://minotar.net/avatar/{safeName}/256";

                string html = _skinViewerService.GenerateHtml(username, skinUrl, fallbackUrl);
                webView.NavigateToString(html);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"WebView2 init failed: {ex.Message}");

                // Show a fallback HTML without 3D (pure 2D avatar)
                string safeName = Uri.EscapeDataString(
                    string.IsNullOrWhiteSpace(username) ? "char" : username);

                webView.NavigateToString(
                    "<!DOCTYPE html><html><head><meta charset='utf-8'>" +
                    "<style>" +
                    "html,body{margin:0;padding:0;width:100%;height:100%;display:flex;align-items:center;justify-content:center;" +
                    "background:linear-gradient(160deg,#0a0e1a,#0d1b2e,#0a1a10);font-family:'Segoe UI',sans-serif;}" +
                    ".box{text-align:center;}" +
                    ".box img{width:160px;height:160px;border-radius:12px;border:2px solid rgba(98,239,173,0.3);image-rendering:pixelated;}" +
                    ".name{color:#62EFAD;font-size:16px;font-weight:700;margin-top:12px;}" +
                    ".hint{color:#445566;font-size:12px;margin-top:6px;}" +
                    "</style></head><body>" +
                    $"<div class='box'>" +
                    $"<img src='https://minotar.net/armor/bust/{safeName}/240' alt='{username}' />" +
                    $"<div class='name'>{System.Security.SecurityElement.Escape(username)}</div>" +
                    $"<div class='hint'>WebView2 runtime not detected<br/>Install it for 3D preview</div>" +
                    "</div></body></html>"
                );
            }
        }

        private async void btnRefresh_Click(object sender, RoutedEventArgs e)
        {
            var session = _sessionService.GetSession();
            if (session != null)
            {
                await InitializeSkinViewer(session.Username ?? "char");
            }
        }

        private void btnCopyUsername_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                string username = lbUsername.Text;
                System.Windows.Clipboard.SetText(username);
                // Use InfoBar as feedback instead of MessageBox
                infoBarStatus.Title = "Copied!";
                infoBarStatus.Message = $"'{username}' copied to clipboard";
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Failed to copy: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void HandleAuthenticationError()
        {
            lbUsername.Text = "Not authenticated";
            MessageBox.Show("Your session has expired. Please log in again.", "Session Expired", MessageBoxButton.OK, MessageBoxImage.Warning);
        }
    }
}
