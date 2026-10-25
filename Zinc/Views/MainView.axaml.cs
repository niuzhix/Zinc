using Avalonia.Controls;
using FluentAvalonia.UI.Controls;
using System;
using Zinc.ViewModels;

namespace Zinc.Views;

public partial class MainView : UserControl
{
    public MainView()
    {
        InitializeComponent();
    }

    private void TabView_AddTabButtonClick(FATabView sender, EventArgs args)
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
}
