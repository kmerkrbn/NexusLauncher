using System;
using System.Windows;
using System.Windows.Controls;

namespace MClauncher.Views
{
    public partial class ConsolePage : Page
    {
        private static ConsolePage? _instance;
        private static readonly object _lockObject = new object();

        public ConsolePage()
        {
            InitializeComponent();
            lock (_lockObject)
            {
                _instance = this;
            }
        }

        public static void Log(string message)
        {
            lock (_lockObject)
            {
                if (_instance == null) return;

                _instance.Dispatcher.Invoke(() =>
                {
                    if (_instance.txtLogs != null)
                    {
                        string formattedMessage = $"[{DateTime.Now:HH:mm:ss}] {message}";
                        _instance.txtLogs.AppendText($"{formattedMessage}{Environment.NewLine}");
                        _instance.txtLogs.ScrollToEnd();
                    }
                });
            }
        }

        public static void ClearLogs()
        {
            lock (_lockObject)
            {
                if (_instance?.txtLogs != null)
                {
                    _instance.Dispatcher.Invoke(() =>
                    {
                        _instance.txtLogs.Clear();
                    });
                }
            }
        }

        public static string GetAllLogs()
        {
            lock (_lockObject)
            {
                if (_instance?.txtLogs != null)
                {
                    string text = "";
                    _instance.Dispatcher.Invoke(() => { text = _instance.txtLogs.Text; });
                    return text;
                }
            }
            return "";
        }

        private void btnClearLogs_Click(object sender, RoutedEventArgs e)
        {
            ClearLogs();
        }
    }
}
