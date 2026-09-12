using Avalonia.Media;
using CommunityToolkit.Mvvm.ComponentModel;
using System;
using System.Collections.ObjectModel;
using Zinc.Abstractions;
using Zinc.Core.Abstractions;
using Zinc.Core.Models;
using Zinc.Models;
using Zinc.Views;

namespace Zinc.ViewModels;

public partial class SPAboutViewModel : ObservableObject
{
    private readonly ISettingsService<AppSettings> _settingsService;

    public AppSettings Settings => _settingsService.Current;

    public string? AppVersion => App.AppVersion;

    public SPAboutViewModel(ISettingsService<AppSettings> settingsService)
    {
        
        _settingsService = settingsService;
    }
}