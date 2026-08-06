using Avalonia.Controls;
using FluentAvalonia.Interop;
using FluentAvalonia.UI.Windowing;
using Zinc.ViewModels;

namespace Zinc.Views
{
    public partial class MainWindow : FAAppWindow
    {
        public MainWindow()
        {
            InitializeComponent();
            DataContext = new MainWindowViewModel();
            TitleBar.ExtendsContentIntoTitleBar = true;
            TitleBar.Height = 48;
        }
    }
}