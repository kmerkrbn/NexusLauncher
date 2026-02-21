using System.Windows;
using System.Windows.Controls;
using Wpf.Ui.Controls;
using MClauncher.Models;
using MessageBox = System.Windows.MessageBox;
using MessageBoxButton = System.Windows.MessageBoxButton;
using MessageBoxImage = System.Windows.MessageBoxImage;
using MessageBoxResult = System.Windows.MessageBoxResult;

namespace MClauncher.Views
{
    public partial class SettingsPage : Page
    {
        private bool _isInitializing = true;

        public SettingsPage()
        {
            InitializeComponent();
        }

        private void Page_Loaded(object sender, RoutedEventArgs e)
        {
            _isInitializing = true;
            LoadSettings();
            _isInitializing = false;
        }

        private void LoadSettings()
        {
            tsSnapshots.IsChecked = SettingsManager.Current.ShowSnapshots;
            nbRam.Value = SettingsManager.Current.RamAllocation;
        }

        private void tsSnapshots_Changed(object sender, RoutedEventArgs e)
        {
            if (_isInitializing) return;

            SettingsManager.Current.ShowSnapshots = tsSnapshots.IsChecked ?? false;
            SettingsManager.Save();
        }

        private void nbRam_SelectionChanged(object sender, RoutedEventArgs e)
        {
            if (_isInitializing) return;

            var value = (int?)nbRam.Value ?? 2048;
            if (value < 512) value = 512;
            if (value > 32768) value = 32768;

            SettingsManager.Current.RamAllocation = value;
            SettingsManager.Save();
        }

        private void btnResetSettings_Click(object sender, RoutedEventArgs e)
        {
            var result = MessageBox.Show(
                "Are you sure you want to reset all settings to defaults?",
                "Reset Settings",
                MessageBoxButton.YesNo,
                MessageBoxImage.Question
            );

            if (result == MessageBoxResult.Yes)
            {
                _isInitializing = true;
                SettingsManager.ResetToDefaults();
                LoadSettings();
                _isInitializing = false;
                MessageBox.Show("Settings have been reset to defaults.", "Success", MessageBoxButton.OK, MessageBoxImage.Information);
            }
        }
    }
}
