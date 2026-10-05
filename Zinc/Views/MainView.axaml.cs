using Avalonia.Controls;
using Avalonia.Interactivity;
using FluentAvalonia.UI.Controls;
using Microsoft.Extensions.DependencyInjection;
using Zinc.ViewModels;

namespace Zinc.Views;

public partial class MainView : UserControl
{
    private Window? _settingsWindow;

    public MainView()
    {
        InitializeComponent();
        DataContext = App.Services.GetRequiredService<MainViewModel>();
    }

    private void TabView_AddTabButtonClick(FATabView sender, System.EventArgs args)
    {
        if (DataContext is MainViewModel vm)
        {
            vm.AddNewTab();
        }
    }

    private void TabView_TabCloseRequested(FATabView sender, FATabViewTabCloseRequestedEventArgs args)
    {
        if (DataContext is MainViewModel vm && args.Item is EditorViewModel editor)
        {
            vm.CloseTab(editor);
        }
    }

    private void Settings_Click(object? sender, RoutedEventArgs e)
    {
        if (_settingsWindow is null)
        {
            _settingsWindow = new SettingsWindow();
            _settingsWindow.Closing += SettingsWindow_Closing;
            _settingsWindow.Show(TopLevel.GetTopLevel(this) as Window);
        }
        else
        {
            _settingsWindow.Activate();
        }
    }

    private void SettingsWindow_Closing(object? sender, WindowClosingEventArgs e)
    {
        if (sender is Window window)
        {
            window.Closing -= SettingsWindow_Closing;
        }
        _settingsWindow = null;
    }
}