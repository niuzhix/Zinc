using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.ApplicationLifetimes;
using Microsoft.Extensions.DependencyInjection;
using System;
using Zinc.Abstractions;
using Zinc.ViewModels;
using Zinc.Views;

namespace Zinc.Services;

public sealed class WindowService : IWindowService
{
    private readonly IServiceProvider _services;
    private SettingsWindow? _settingsWindow;

    public WindowService(IServiceProvider services)
    {
        _services = services;
    }

    public void ShowSettings()
    {
        if (_settingsWindow is not null)
        {
            _settingsWindow.Activate();
            return;
        }

        var window = new SettingsWindow
        {
            DataContext = _services.GetRequiredService<SettingsWindowViewModel>()
        };

        window.Closed += OnSettingsWindowClosed;
        _settingsWindow = window;

        var owner = GetOwner();
        if (owner is null)
        {
            window.Show();
        }
        else
        {
            window.Show(owner);
        }
    }

    private void OnSettingsWindowClosed(object? sender, EventArgs e)
    {
        if (sender is SettingsWindow window)
        {
            window.Closed -= OnSettingsWindowClosed;
        }

        _settingsWindow = null;
    }

    private static Window? GetOwner()
        => (Application.Current?.ApplicationLifetime as IClassicDesktopStyleApplicationLifetime)?.MainWindow;
}
