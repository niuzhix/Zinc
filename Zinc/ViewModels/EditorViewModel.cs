using Avalonia.Controls.Shapes;
using AvaloniaEdit.Document;
using AvaloniaEdit.TextMate;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Zinc.Core.Abstractions;
using Zinc.Core.Models;
using Zinc.Core.Services;
using Zinc.Models;
using Zinc.Views;

namespace Zinc.ViewModels;
	public partial class EditorViewModel : ObservableObject
{
	private readonly ISettingsService _settings;
	private readonly IDialogService _dialogService;
	private readonly IFileService _fileService;

	[ObservableProperty]
	private TextDocument content;

	[ObservableProperty]
	private string? filename = string.Empty;
	
	private string? filepath = string.Empty;
	private readonly IReadOnlyList<FileFilter> _filters = new List<FileFilter>()
	{
		new FileFilter(){ Name = "C++代码文件", Patterns = ["*.cpp", "*.cxx"] }
	};

	public EditorViewModel(string? _content = null, string? _path = null)
	{
		_settings = new SettingsService();
		_settings.Preload();
		_dialogService = new DialogService();
		_fileService = new FileService();

		Content = new TextDocument();
		if (!string.IsNullOrEmpty(_content))
		{
			Content.Insert(0, _content);
		}
		filepath = _path;
		Filename = _path?.Split("\\").Last();
	}

	[RelayCommand]
	public async Task SaveAsync()
	{
		if (string.IsNullOrEmpty(filepath))
		{
			var selectedpath = await _dialogService.SaveFilePathAsync("选择保存文件位置", "未标题", ".cpp", _filters);
			if (string.IsNullOrEmpty(selectedpath))
			{
				return;
			}
			filepath = selectedpath;
		}

		_fileService.SaveFile(filepath, Content.Text);
		Filename = filepath?.Split("\\").Last();

	}

	[RelayCommand]
	public async Task SaveAsAsync()
	{
		var selectedpath = await _dialogService.SaveFilePathAsync("选择保存文件位置", "未标题", ".cpp", _filters);
		if (string.IsNullOrEmpty(selectedpath))
		{
			return;
		}

		_fileService.SaveFile(selectedpath, Content.Text);

	}

	public AppSettings Settings => _settings.appSettings;
	public void SaveSettings() => _settings.Save();
}
