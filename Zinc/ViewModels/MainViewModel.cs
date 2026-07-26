using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using System.Collections.ObjectModel;
using System.Xml.Serialization;

namespace Zinc.ViewModels;
public partial class MainViewModel : ObservableObject
{
    [ObservableProperty]
    private ObservableCollection<EditorViewModel> _tabs = new();
    [ObservableProperty]
    private EditorViewModel? _selectedTab;

    private int _tabCount = 1;

    public MainViewModel()
    {
        var _defaulttab = new EditorViewModel()
        {
            Header = "未标题1",
            Content = string.Empty
        };
        Tabs.Add(_defaulttab);
        SelectedTab = _defaulttab;
    }

    [RelayCommand]
    private void AddTab()
    {
        var _newTab = new EditorViewModel()
        {
            Header = $"未标题{++_tabCount}",
            Content = string.Empty
        };
        Tabs.Add(_newTab);
        SelectedTab = _newTab;
    }

    [RelayCommand]
    private void RemoveTab(EditorViewModel? tab)
    {
        if (tab == null || Tabs.Count <= 1) return;
        
        var _index = Tabs.IndexOf(tab);
        Tabs.RemoveAt(_index);

        if (SelectedTab == tab)
        {
            SelectedTab = _index < Tabs.Count ? Tabs[_index] : Tabs[^1];
        }
    }
}
