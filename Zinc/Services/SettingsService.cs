using System;
using System.ComponentModel;
using System.IO;
using System.Text.Json;
using System.Text.Json.Serialization;
using Zinc.Abstractions;
using Zinc.Converters;

namespace Zinc.Services;

public class SettingsService<T> : ISettingsService<T>
    where T : class, INotifyPropertyChanged, new()
{
    private readonly string _filePath;
    private readonly JsonSerializerOptions _jsonOptions;
    private readonly object _sync = new();
    private T _current;
    private bool _isLoading;

    public T Current => _current;

    public event EventHandler<SettingsChangedEventArgs<T>>? SettingsChanged;

    public SettingsService(string? appName = null)
    {
        appName ??= "Zinc";

        var appData = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
        var directory = Path.Combine(appData, appName);
        Directory.CreateDirectory(directory);

        _filePath = Path.Combine(directory, "settings.json");

        _jsonOptions = new JsonSerializerOptions
        {
            WriteIndented = true,
            PropertyNameCaseInsensitive = true,
            DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
            Converters = {
                new JsonStringEnumConverter(),
                new FontFamilyToJsonConverter()
            }
        };

        _current = Load();
        _current.PropertyChanged += OnSettingsPropertyChanged;
    }

    public void Save()
    {
        lock (_sync)
        {
            try
            {
                var json = JsonSerializer.Serialize(_current, _jsonOptions);
                File.WriteAllText(_filePath, json);
            }
            catch (Exception ex)
            {
                // TODO: 记录日志
            }
        }
    }

    public void Reload()
    {
        lock (_sync)
        {
            _isLoading = true;
            try
            {
                _current.PropertyChanged -= OnSettingsPropertyChanged;
                _current = Load();
                _current.PropertyChanged += OnSettingsPropertyChanged;
            }
            finally
            {
                _isLoading = false;
            }
        }

        SettingsChanged?.Invoke(
            this,
            new SettingsChangedEventArgs<T>(_current, propertyName: null));
    }

    private T Load()
    {
        try
        {
            if (File.Exists(_filePath))
            {
                var json = File.ReadAllText(_filePath);
                var settings = JsonSerializer.Deserialize<T>(json, _jsonOptions);
                if (settings is not null)
                    return settings;
            }
        }
        catch (Exception ex)
        {
            // TODO: 记录日志
        }

        return new T();
    }

    private void OnSettingsPropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (_isLoading)
            return;

        Save();
        SettingsChanged?.Invoke(
            this,
            new SettingsChangedEventArgs<T>(_current, e.PropertyName));
    }
}