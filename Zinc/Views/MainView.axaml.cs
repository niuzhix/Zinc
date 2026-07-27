using Avalonia;
using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Markup.Xaml;
using Avalonia.Markup.Xaml.MarkupExtensions;
using FluentAvalonia.UI.Controls;
using Zinc.ViewModels;

namespace Zinc.Views;

public partial class MainView : UserControl
{
    public MainView()
    {
        InitializeComponent();

        var newTab = new FATabViewItem();
        newTab.Header = $"New Document {TabView.TabItems.Count}";
        newTab.Content = new EditorView();
        TabView.TabItems.Add(newTab);
        TabView.SelectedItem = newTab;
    }

    private void TabView_AddTabButtonClick(FATabView sender, System.EventArgs args)
    {
        var newTab = new FATabViewItem();
        newTab.Header = $"New Document {sender.TabItems.Count}";
        newTab.Content = new EditorView();
        sender.TabItems.Add(newTab);
        sender.SelectedItem = newTab;
    }

    private void TabView_TabCloseRequested(FATabView sender, FATabViewTabCloseRequestedEventArgs args)
    {
        sender.TabItems.Remove(args.Tab);
    }
}