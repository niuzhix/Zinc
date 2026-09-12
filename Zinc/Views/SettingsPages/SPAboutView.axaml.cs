using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Markup.Xaml;
using Avalonia.Platform;
using FluentAvalonia.UI.Controls;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Collections.ObjectModel;
using System.IO;
using Zinc.ViewModels;

namespace Zinc.Views.SettingsPages;

public partial class SPAboutView : UserControl
{
    public SPAboutViewModel ViewModel { get; } = App.Services.GetRequiredService<SPAboutViewModel>();

    public SPAboutView()
    {
        InitializeComponent();
        DataContext = App.Services.GetRequiredService<SPAboutViewModel>();
    }

    private async void SettingsExpanderItemShowOssLicense_OnClick(object? sender, RoutedEventArgs e)
    {
        var license = await new StreamReader(AssetLoader.Open(new Uri("avares://Zinc/Assets/LICENSE.txt")))
            .ReadToEndAsync();
        await new FAContentDialog()
        {
            Title = "开放源代码许可",
            Content = new TextBlock()
            {
                Text = license
            },
            PrimaryButtonText = "关闭",
            DefaultButton = FAContentDialogButton.Primary
        }.ShowAsync();
    }
}