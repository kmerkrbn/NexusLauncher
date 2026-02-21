using System.Windows;

namespace MClauncher.Views
{
    public partial class PlayPage : System.Windows.Controls.Page
    {
        public PlayPage()
        {
            InitializeComponent();
        }

        private void Page_Loaded(object sender, RoutedEventArgs e)
        {
            System.Diagnostics.Debug.WriteLine("PlayPage loaded");
        }
    }
}
