using Avalonia;
using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Markup.Xaml;
using FluentAvalonia.UI.Controls;
using Zinc.ViewModels;

namespace Zinc.Views;

public partial class MainView : UserControl
{
    public MainView()
    {
        InitializeComponent();
    }

    private void TabView_TabCloseRequested(FATabView sender, FATabViewTabCloseRequestedEventArgs e)
    {
        if (DataContext is MainViewModel vm && e.Tab is FATabViewItem tab)
        {
            vm.RemoveTabCommand.Execute(tab.DataContext);
        }
    }
}