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

public partial class SPUIViewModel : ObservableObject
{
    [ObservableProperty]
    public string testText = "#include <bits/stdc++.h>\nusing namespace std;\nint main(){\n    //处理逻辑\n    return 0;\n}";

    private readonly ISettingsService<AppSettings> _settingsService;

    public AppSettings Settings => _settingsService.Current;

    public ObservableCollection<FontFamily> FontFamilies { get; } = new(FontManager.Current.SystemFonts);

    public SPUIViewModel(ISettingsService<AppSettings> settingsService)
    {
        
        _settingsService = settingsService;
    }
}