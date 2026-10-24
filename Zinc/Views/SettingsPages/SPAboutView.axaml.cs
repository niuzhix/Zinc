using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Platform;
using FluentAvalonia.UI.Controls;
using System;
using System.IO;

namespace Zinc.Views.SettingsPages;

public partial class SPAboutView : UserControl
{
    public SPAboutView()
    {
        InitializeComponent();
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
