using Avalonia.Media;
using System.Collections.Generic;
using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace Zinc.Models;

public abstract class ObservableObjects : INotifyPropertyChanged
{
    private readonly Dictionary<string, ObservableObjects> _childObjects = new();

    public event PropertyChangedEventHandler? PropertyChanged;

    protected bool SetProperty<T>(
        ref T field,
        T value,
        [CallerMemberName] string? propertyName = null)
    {
        if (EqualityComparer<T>.Default.Equals(field, value))
            return false;

        if (field is ObservableObjects oldChild && propertyName is not null)
        {
            UnsubscribeChild(propertyName, oldChild);
        }

        field = value;

        if (value is ObservableObjects newChild && propertyName is not null)
        {
            SubscribeChild(propertyName, newChild);
        }

        OnPropertyChanged(propertyName);
        return true;
    }

    protected void SubscribeChild(string propertyName, ObservableObjects child)
    {
        if (propertyName is null || child is null)
            return;

        child.PropertyChanged -= OnChildPropertyChanged;
        child.PropertyChanged += OnChildPropertyChanged;
        _childObjects[propertyName] = child;
    }

    protected void UnsubscribeChild(string propertyName, ObservableObjects child)
    {
        if (propertyName is null || child is null)
            return;

        child.PropertyChanged -= OnChildPropertyChanged;
        _childObjects.Remove(propertyName);
    }

    protected void OnPropertyChanged([CallerMemberName] string? propertyName = null)
        => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));

    private void OnChildPropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        foreach (var pair in _childObjects)
        {
            if (ReferenceEquals(pair.Value, sender))
            {
                PropertyChanged?.Invoke(
                    this,
                    new PropertyChangedEventArgs($"{pair.Key}.{e.PropertyName}"));
                break;
            }
        }
    }
}

public class AppSettings : ObservableObjects
{
    private int _theme = 0;
    private bool _isCustomThemeColorEnabled = true;
    private Color _themeColor;
    private double _opacity;

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
}