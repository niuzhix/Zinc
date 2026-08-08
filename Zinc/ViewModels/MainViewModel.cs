using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using Zinc.Core.Abstractions;
using Zinc.Core.Services;
using Zinc.Models;
using Zinc.Views;

namespace Zinc.ViewModels;

public partial class MainViewModel : ObservableObject
{
    private IDialogService _dialogService;
    private IFileService _fileService;

    [ObservableProperty]
    private ObservableCollection<TabItemModel> _tabs = new ObservableCollection<TabItemModel>()
    {
        new TabItemModel(){ Header = "New Document 0", Content = new EditorView(){ } }
    };

    [ObservableProperty]
    private TabItemModel? _selectedItem;

    private IReadOnlyList<FileFilter> _filters = new List<FileFilter>()
    {
        new FileFilter(){ Name = "C++代码文件", Patterns = ["*.cpp", "*.cxx"] }
    };

    public MainViewModel()
    {
        _dialogService = new DialogService();
        _fileService = new FileService();
        _selectedItem = Tabs[0];
    }

    [RelayCommand]
    private async Task OpenAsync()
    {
        var filepath = await _dialogService.OpenFilePathAsync("选择要打开的文件", _filters);
        if (!string.IsNullOrEmpty(filepath))
        {
            var file = _fileService.LoadFile(filepath);
            Tabs.Add(new TabItemModel() { Header = filepath.Split("\\").Last(), Content = new EditorView(file, filepath) });
        }
    }

    [RelayCommand]
    private async Task SaveCurrentTabAsync()
    {
        if (SelectedItem?.Content?.DataContext is EditorViewModel ev)
        {
            await ev.SaveAsync();
            SelectedItem.Header = ev.Filename;
        }
    }

    [RelayCommand]
    private async Task SaveAsCurrentTabAsync()
    {
        if (SelectedItem?.Content?.DataContext is EditorViewModel ev)
        {
            await ev.SaveAsAsync();
        }
    }

}
