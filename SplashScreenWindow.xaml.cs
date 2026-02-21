using System.Windows;

namespace MClauncher
{
    public partial class SplashScreenWindow : Window
    {
        public SplashScreenWindow()
        {
            InitializeComponent();
        }

        public void UpdateStatus(string text)
        {
            txtStatus.Text = text;
        }
    }
}
