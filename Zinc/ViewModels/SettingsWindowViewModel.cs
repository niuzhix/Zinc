using CommunityToolkit.Mvvm.ComponentModel;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Collections.Generic;

namespace Zinc.ViewModels;

public partial class SettingsWindowViewModel : ObservableObject
{
    private readonly IServiceProvider _services;

    [ObservableProperty]
    private SettingsPageItem? _selectedPage;

    [ObservableProperty]
    private object? _currentViewModel;

    public SettingsWindowViewModel(IServiceProvider services)
    {
        _services = services;

        MenuPages =
        [
            new SettingsPageItem("外观", "\uE51E", typeof(SPUIViewModel)),
            new SettingsPageItem("编译", "\uE423", typeof(SPCompileViewModel)),
            new SettingsPageItem("编辑器", "\uE50C", typeof(SPEditViewModel)),
        ];

        FooterPages =
        [
            new SettingsPageItem("关于 Zinc", "\uE9E4", typeof(SPAboutViewModel)),
        ];

        NavigateTo(MenuPages[0]);
    }

    public IReadOnlyList<SettingsPageItem> MenuPages { get; }

    public IReadOnlyList<SettingsPageItem> FooterPages { get; }

    public void NavigateTo(SettingsPageItem? page)
    {
        if (page is null) return;

        SelectedPage = page;
    }

    partial void OnSelectedPageChanged(SettingsPageItem? value)
    {
        if (value is not null && !ReferenceEquals(CurrentViewModel?.GetType(), value.ViewModelType))
        {
            CurrentViewModel = _services.GetRequiredService(value.ViewModelType);
        }
    }
}
