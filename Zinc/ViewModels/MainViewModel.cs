using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Zinc.Abstractions;
using Zinc.Core.Abstractions;
using Zinc.Models;

namespace Zinc.ViewModels;

public partial class MainViewModel : ObservableObject
{
    private readonly IDialogService _dialogService;
    private readonly IFileService _fileService;
    private readonly IEditorViewModelFactory _editorFactory;
    private readonly IWindowService _windowService;

    private readonly IReadOnlyList<FileFilter> _filters =
    [
        new() { Name = "C++代码文件", Patterns = ["*.cpp", "*.cxx"] }
    ];

    private int _untitledCount;

    public ObservableCollection<EditorViewModel> Tabs { get; } = new();

    [ObservableProperty]
    private EditorViewModel? _selectedItem;

    public MainViewModel(
        IDialogService dialogService,
        IFileService fileService,
        IEditorViewModelFactory editorFactory,
        IWindowService windowService)
    {
        _dialogService = dialogService;
        _fileService = fileService;
        _editorFactory = editorFactory;
        _windowService = windowService;

        AddNewTab();
    }


    /// <summary>新建一个空白标签页并选中。</summary>
    [RelayCommand]
    public void AddNewTab()
    {
        var vm = _editorFactory.Create();
        vm.FileName = $"未标题 {++_untitledCount}";
        Tabs.Add(vm);
        SelectedItem = vm;
    }

    /// <summary>关闭标签页；关闭最后一个标签页时自动补一个空白页。</summary>
    public void CloseTab(EditorViewModel vm)
    {
        Tabs.Remove(vm);
        vm.Dispose();

        if (Tabs.Count == 0)
        {
            AddNewTab();
        }

        SelectedItem = Tabs.FirstOrDefault();
    }


    [RelayCommand]
    private void OpenSettings() => _windowService.ShowSettings();

    [RelayCommand]
    private async Task OpenAsync()
    {
        var filePath = await _dialogService.OpenFilePathAsync("选择要打开的文件", _filters);
        if (string.IsNullOrEmpty(filePath))
            return;

        // 同一文件只保留一个标签页，已打开时直接切换。
        var existing = Tabs.FirstOrDefault(t =>
            string.Equals(t.FilePath, filePath, StringComparison.OrdinalIgnoreCase));
        if (existing is not null)
        {
            SelectedItem = existing;
            return;
        }

        var file = _fileService.LoadFile(filePath);
        var vm = _editorFactory.Create(file, filePath);
        vm.FileName = Path.GetFileName(filePath);

        Tabs.Add(vm);
        SelectedItem = vm;
    }

    [RelayCommand]
    private Task SaveCurrentTabAsync()
    {
        return SelectedItem?.SaveAsync() ?? Task.CompletedTask;
    }

    [RelayCommand]
    private Task SaveAsCurrentTabAsync()
    {
        return SelectedItem?.SaveAsAsync() ?? Task.CompletedTask;
    }
}