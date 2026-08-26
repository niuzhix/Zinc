using FluentAvalonia.UI.Windowing;
using Microsoft.Extensions.DependencyInjection;
using Zinc.ViewModels;

namespace Zinc.Views;

public partial class MainWindow : FAAppWindow
{
    public MainWindow()
    {
        InitializeComponent();
        DataContext = App.Services.GetRequiredService<MainWindowViewModel>();
        TitleBar.ExtendsContentIntoTitleBar = true;
        TitleBar.Height = 48;
    }
}