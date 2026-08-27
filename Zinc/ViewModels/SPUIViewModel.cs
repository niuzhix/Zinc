using CommunityToolkit.Mvvm.ComponentModel;
using System;
using Zinc.Core.Abstractions;
using Zinc.Core.Models;
using Zinc.Abstractions;
using Zinc.Models;

namespace Zinc.ViewModels;

public partial class SPUIViewModel : ObservableObject
{
    private readonly ISettingsService<AppSettings> _settingsService;

    public AppSettings Settings => _settingsService.Current;

    public SPUIViewModel(ISettingsService<AppSettings> settingsService)
    {
        
        _settingsService = settingsService;
    }
}