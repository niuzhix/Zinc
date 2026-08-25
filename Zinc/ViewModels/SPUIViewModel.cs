using CommunityToolkit.Mvvm.ComponentModel;
using FluentAvalonia.UI.Controls;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using Zinc.Core.Abstractions;
using Zinc.Core.Models;
using Zinc.Core.Services;

namespace Zinc.ViewModels;

public partial class SPUIViewModel : ObservableObject
{
    private readonly ISettingsService<AppSettings> _settingsService;

    public SPUIViewModel(ISettingsService<AppSettings> settingsService)
    {
        _settingsService = settingsService;
    }
    public AppSettings Settings => _settingsService.Current;
}