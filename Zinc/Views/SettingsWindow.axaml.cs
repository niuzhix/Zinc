using FluentAvalonia.UI.Controls;
using FluentAvalonia.UI.Windowing;
using System;

namespace Zinc.Views;

public partial class SettingsWindow : FAAppWindow
{
    public SettingsWindow()
    {
        InitializeComponent();
        TitleBar.Height = 48;
    }

    private void FANavigationView_SelectionChanged(object? sender, FluentAvalonia.UI.Controls.FANavigationViewSelectionChangedEventArgs e)
    {
        var pgSourse = $"Zinc.Views.SettingsPages.SP{(e.SelectedItem as FANavigationViewItem).Tag}View";
        var pg = Activator.CreateInstance(Type.GetType(pgSourse));
        (sender as FANavigationView).Content = pg;
    }
}