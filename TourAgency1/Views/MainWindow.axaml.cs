using Avalonia.Controls;
using Avalonia.Markup.Xaml;
using TourAgency.ViewModels;

namespace TourAgency.Views
{
    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            InitializeComponent();
            DataContext = new MainViewModel();
        }

        private void InitializeComponent()
        {
            AvaloniaXamlLoader.Load(this);
        }
    }
}
