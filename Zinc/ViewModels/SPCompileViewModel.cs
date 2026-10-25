using CommunityToolkit.Mvvm.ComponentModel;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Reflection;
using Zinc.Abstractions;
using Zinc.Core.Models;
using Zinc.Models;

namespace Zinc.ViewModels;

/// <summary>下拉框中的 C++ 标准选项，显示名取自枚举上的 <see cref="DisplayAttribute"/>。</summary>
public sealed class CppStandardOption
{
    public CppStandardOption(CppStandard value, string displayName)
    {
        Value = value;
        DisplayName = displayName;
    }

    public CppStandard Value { get; }

    public string DisplayName { get; }
}

public partial class SPCompileViewModel : ObservableObject
{
    private readonly ISettingsService<AppSettings> _settingsService;

    public SPCompileViewModel(ISettingsService<AppSettings> settingsService)
    {
        _settingsService = settingsService;

        StandardOptions = Enum.GetValues<CppStandard>()
            .Select(standard => new CppStandardOption(standard, GetDisplayName(standard)))
            .ToArray();
    }

    public AppSettings Settings => _settingsService.Current;

    public IReadOnlyList<CppStandardOption> StandardOptions { get; }

    public CppStandardOption? SelectedStandard
    {
        get => StandardOptions.FirstOrDefault(o => o.Value == Settings.StandardVersion);
        set
        {
            if (value is null || value.Value == Settings.StandardVersion) return;

            Settings.StandardVersion = value.Value;
            OnPropertyChanged();
        }
    }

    private static string GetDisplayName(CppStandard standard)
        => typeof(CppStandard)
               .GetField(standard.ToString())
               ?.GetCustomAttribute<DisplayAttribute>()
               ?.Name
           ?? standard.ToString();
}
