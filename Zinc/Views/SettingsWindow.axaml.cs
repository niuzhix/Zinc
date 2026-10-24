using FluentAvalonia.UI.Controls;
using FluentAvalonia.UI.Windowing;
using Microsoft.Extensions.DependencyInjection;
using Zinc.ViewModels;

namespace Zinc.Views;

public partial class SettingsWindow : FAAppWindow
{
    public SettingsWindow()
    {
        InitializeComponent();
        DataContext = App.Services.GetRequiredService<SettingsWindowViewModel>();
        TitleBar.Height = 48;
    }

    private void FANavigationView_SelectionChanged(object? sender, FANavigationViewSelectionChangedEventArgs e)
    {
        if (DataContext is not SettingsWindowViewModel vm)
            return;

        // MenuItemsSource 生成容器时，SelectedItem 可能是数据项，也可能是容器；两种都接受。
        var page = e.SelectedItem as SettingsPageItem
                   ?? (e.SelectedItem as FANavigationViewItem)?.Tag as SettingsPageItem;

        vm.NavigateTo(page);
    }
}
