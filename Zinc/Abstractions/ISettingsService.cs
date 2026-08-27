using System;
using System.ComponentModel;

namespace Zinc.Abstractions;

public interface ISettingsService<T>
    where T : class, INotifyPropertyChanged, new()
{
    T Current { get; }

    event EventHandler<SettingsChangedEventArgs<T>>? SettingsChanged;

    void Save();

    void Reload();
}

public class SettingsChangedEventArgs<T> : EventArgs
{
    public SettingsChangedEventArgs(T settings, string? propertyName)
    {
        Settings = settings;
        PropertyName = propertyName;
    }

    public T Settings { get; }

    public string? PropertyName { get; }
}