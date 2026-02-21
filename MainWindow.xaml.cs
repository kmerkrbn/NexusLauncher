using System.Windows;
using System.Windows.Input;
using Wpf.Ui.Controls;
using CmlLib.Core.Auth;
using MClauncher.Services;
using MClauncher.Views;

namespace MClauncher
{
    public partial class MainWindow : FluentWindow
    {
        private readonly ISessionService _sessionService;

        public MainWindow()
        {
            InitializeComponent();
            _sessionService = new SessionService();
            StateChanged += MainWindow_StateChanged;
            NavigateToLogin();
            UpdateMaximizeButtonGlyph();
        }

        private void NavigateToLogin()
        {
            RootFrame.Navigate(new LoginPage(_sessionService, this));
        }

        public ISessionService GetSessionService() => _sessionService;

        public void NavigateToMain()
        {
            RootFrame.Navigate(new MainPage(_sessionService));
        }

        public void Logout()
        {
            _sessionService.ClearSession();
            NavigateToLogin();
        }

        private void TopBar_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            if (e.ClickCount == 2)
            {
                ToggleMaximizeRestore();
                return;
            }

            try
            {
                DragMove();
            }
            catch
            {
                // Ignore drag exceptions
            }
        }

        private void BtnMinimize_Click(object sender, RoutedEventArgs e)
        {
            WindowState = WindowState.Minimized;
        }

        private void BtnMaxRestore_Click(object sender, RoutedEventArgs e)
        {
            ToggleMaximizeRestore();
        }

        private void BtnClose_Click(object sender, RoutedEventArgs e)
        {
            Close();
        }

        private void ToggleMaximizeRestore()
        {
            WindowState = WindowState == WindowState.Maximized
                ? WindowState.Normal
                : WindowState.Maximized;

            UpdateMaximizeButtonGlyph();
        }

        private void MainWindow_StateChanged(object? sender, EventArgs e)
        {
            UpdateMaximizeButtonGlyph();
        }

        private void UpdateMaximizeButtonGlyph()
        {
            if (btnMaxRestore?.Content is TextBlock tb)
            {
                tb.Text = WindowState == WindowState.Maximized ? "❐" : "▢";
            }
        }
    }
}