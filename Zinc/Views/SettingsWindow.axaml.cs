using FluentAvalonia.UI.Controls;
using FluentAvalonia.UI.Media.Animation;
using FluentAvalonia.UI.Windowing;
using System;
using Zinc.Services;

namespace Zinc.Views;

public partial class SettingsWindow : FAAppWindow
{
    public SettingsWindow()
    {
        InitializeComponent();
        TitleBar.Height = 48;
        SplashScreen = new ZincSplashScreen();
    }

    private void FANavigationView_SelectionChanged(object? sender, FluentAvalonia.UI.Controls.FANavigationViewSelectionChangedEventArgs e)
    {
        var pgSourse = $"Zinc.Views.SettingsPages.SP{(e.SelectedItem as FANavigationViewItem).Tag}View";
        var pg = Type.GetType(pgSourse);
        ContentFrame.Navigate(pg, null, new FASlideNavigationTransitionInfo
        {
            Effect = FASlideNavigationTransitionEffect.FromBottom
        });
    }
}