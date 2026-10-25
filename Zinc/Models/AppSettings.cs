using Avalonia.Media;
using System.Collections.Generic;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using Zinc.Core.Models;

namespace Zinc.Models;

/// <summary>提供 <see cref="INotifyPropertyChanged"/> 实现的轻量基类。</summary>
public abstract class ObservableObjects : INotifyPropertyChanged
{
    public event PropertyChangedEventHandler? PropertyChanged;

    protected bool SetProperty<T>(
        ref T field,
        T value,
        [CallerMemberName] string? propertyName = null)
    {
        if (EqualityComparer<T>.Default.Equals(field, value))
            return false;

        field = value;
        OnPropertyChanged(propertyName);
        return true;
    }

    protected void OnPropertyChanged([CallerMemberName] string? propertyName = null)
        => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
}

public class AppSettings : ObservableObjects
{
    /// <summary>跟随系统主题。</summary>
    public const int ThemeSystem = 0;

    /// <summary>强制明亮主题。</summary>
    public const int ThemeLight = 1;

    /// <summary>强制黑暗主题。</summary>
    public const int ThemeDark = 2;

    private int _theme = ThemeSystem;
    private bool _isCustomThemeColorEnabled = false;
    private Color _themeColor;
    private double _opacity = 1;
    private FontFamily _editorFont = new("avares://Zinc/Assets/Fonts/FiraCode.ttf#Fira Code");
    private FontWeight _editorFontWeight = FontWeight.Medium;
    private double _editorFontSize = 16;
    private string _compilerPath = string.Empty;
    private bool _enableO2 = true;
    private bool _enableGDB = true;
    private CppStandard _standardVersion = CppStandard.Cpp11;
    private bool _warningCheck = true;
    private bool _overAddressCheck = false;
    private bool _showLineNumbers = true;
    private bool _highlightCurrentLine = true;
    private bool _wordWrap = false;
    private bool _cutCopyWholeLine = true;
    private bool _autoFormatting = true;

    public int Theme
    {
        get => _theme;
        set => SetProperty(ref _theme, value);
    }

    public bool IsCustomThemeColorEnabled
    {
        get => _isCustomThemeColorEnabled;
        set => SetProperty(ref _isCustomThemeColorEnabled, value);
    }

    public Color ThemeColor
    {
        get => _themeColor;
        set => SetProperty(ref _themeColor, value);
    }

    public double Opacity
    {
        get => _opacity;
        set => SetProperty(ref _opacity, value);
    }

    public FontFamily EditorFont
    {
        get => _editorFont;
        set => SetProperty(ref _editorFont, value);
    }

    public FontWeight EditorFontWeight
    {
        get => _editorFontWeight;
        set => SetProperty(ref _editorFontWeight, value);
    }

    public double EditorFontSize
    {
        get => _editorFontSize;
        set => SetProperty(ref _editorFontSize, value);
    }

    public string CompilerPath
    {
        get => _compilerPath;
        set => SetProperty(ref _compilerPath, value);
    }

    public bool EnableO2
    {
        get => _enableO2;
        set => SetProperty(ref _enableO2, value);
    }

    public bool EnableGDB
    {
        get => _enableGDB;
        set => SetProperty(ref _enableGDB, value);
    }

    public CppStandard StandardVersion
    {
        get => _standardVersion;
        set => SetProperty(ref _standardVersion, value);
    }

    public bool WarningCheck
    {
        get => _warningCheck;
        set => SetProperty(ref _warningCheck, value);
    }

    public bool OverAddressCheck
    {
        get => _overAddressCheck;
        set => SetProperty(ref _overAddressCheck, value);
    }

    public bool ShowLineNumbers
    {
        get => _showLineNumbers;
        set => SetProperty(ref _showLineNumbers, value);
    }

    public bool HighlightCurrentLine
    {
        get => _highlightCurrentLine;
        set => SetProperty(ref _highlightCurrentLine, value);
    }

    public bool WordWrap
    {
        get => _wordWrap;
        set => SetProperty(ref _wordWrap, value);
    }

    public bool CutCopyWholeLine
    {
        get => _cutCopyWholeLine;
        set => SetProperty(ref _cutCopyWholeLine, value);
    }

    public bool AutoFormatting
    {
        get => _autoFormatting;
        set => SetProperty(ref _autoFormatting, value);
    }
}