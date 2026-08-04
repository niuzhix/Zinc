using Avalonia.Controls;
using FluentAvalonia.Interop;
using FluentAvalonia.UI.Windowing;

namespace Zinc.Views
{
    public partial class MainWindow : FAAppWindow
    {
        public MainWindow()
        {
            InitializeComponent();
            TitleBar.ExtendsContentIntoTitleBar = true;
            TitleBar.Height = 48;
        }
    }
}