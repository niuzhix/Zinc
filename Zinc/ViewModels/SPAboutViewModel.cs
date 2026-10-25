using CommunityToolkit.Mvvm.ComponentModel;
using System;
using Zinc.Abstractions;
using Zinc.Models;

namespace Zinc.ViewModels;

public partial class SPAboutViewModel : ObservableObject
{
    private const int CopyrightStartYear = 2025;

    private readonly ISettingsService<AppSettings> _settingsService;

    public AppSettings Settings => _settingsService.Current;

    public string? AppVersion => App.AppVersion;

    /// <summary>形如 “Copyright (c) 2025-2026 niuzhix”。</summary>
    public string CopyrightNotice
    {
        get
        {
            var endYear = DateTime.Now.Year;
            var span = endYear > CopyrightStartYear
                ? $"{CopyrightStartYear}-{endYear}"
                : CopyrightStartYear.ToString();

            return $"Copyright (c) {span} niuzhix";
        }
    }

    public SPAboutViewModel(ISettingsService<AppSettings> settingsService)
    {
        _settingsService = settingsService;
    }
}
