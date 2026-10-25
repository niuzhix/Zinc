using FluentAvalonia.UI.Windowing;
using Zinc.Services;

namespace Zinc.Views;

public partial class MainWindow : FAAppWindow
{
    public MainWindow()
    {
        InitializeComponent();

        TitleBar.ExtendsContentIntoTitleBar = true;
        TitleBar.Height = 48;
        SplashScreen = new ZincSplashScreen();
    }
}
