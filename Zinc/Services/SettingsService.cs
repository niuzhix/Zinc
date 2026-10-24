using System;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading;
using Zinc.Abstractions;
using Zinc.Converters;

namespace Zinc.Services;

public class SettingsService<T> : ISettingsService<T>, IDisposable
    where T : class, INotifyPropertyChanged, new()
{
    /// <summary>属性连续变化时，合并为一次落盘的等待时间。</summary>
    private const int SaveDebounceMilliseconds = 500;

    private readonly string _filePath;
    private readonly JsonSerializerOptions _jsonOptions;
    private readonly object _sync = new();
    private T _current;
    private bool _isLoading;
    private bool _disposed;
    private string? _pendingJson;
    private Timer? _saveTimer;

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

    /// <summary>立即把当前设置写入磁盘，并取消已排队的延迟写入。</summary>
    public void Save()
    {
        string json;
        try
        {
            json = JsonSerializer.Serialize(_current, _jsonOptions);
        }
        catch (Exception ex)
        {
            Log("序列化设置失败", ex);
            return;
        }

        lock (_sync)
        {
            _pendingJson = null;
            WriteFile(json);
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
            Log("读取设置失败，已回退到默认值", ex);
        }

        return new T();
    }

    private void OnSettingsPropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (_isLoading)
            return;

        QueueSave();
        SettingsChanged?.Invoke(
            this,
            new SettingsChangedEventArgs<T>(_current, e.PropertyName));
    }

    /// <summary>在调用方线程完成序列化，把耗时的文件写入推迟到防抖计时器。</summary>
    private void QueueSave()
    {
        string json;
        try
        {
            json = JsonSerializer.Serialize(_current, _jsonOptions);
        }
        catch (Exception ex)
        {
            Log("序列化设置失败", ex);
            return;
        }

        lock (_sync)
        {
            _pendingJson = json;
        }

        if (_disposed)
            return;

        _saveTimer ??= new Timer(_ => FlushPending(), null, Timeout.Infinite, Timeout.Infinite);
        _saveTimer.Change(SaveDebounceMilliseconds, Timeout.Infinite);
    }

    private void FlushPending()
    {
        string? json;
        lock (_sync)
        {
            json = _pendingJson;
            _pendingJson = null;
        }

        if (json is null)
            return;

        lock (_sync)
        {
            WriteFile(json);
        }
    }

    private void WriteFile(string json)
    {
        try
        {
            File.WriteAllText(_filePath, json);
        }
        catch (Exception ex)
        {
            Log($"写入设置文件失败: {_filePath}", ex);
        }
    }

    private static void Log(string message, Exception ex)
        => Trace.TraceError($"[SettingsService] {message}: {ex}");

    public void Dispose()
    {
        if (_disposed)
            return;

        _disposed = true;
        _current.PropertyChanged -= OnSettingsPropertyChanged;

        Timer? timer;
        lock (_sync)
        {
            timer = _saveTimer;
            _saveTimer = null;
        }

        timer?.Dispose();
        FlushPending();
        GC.SuppressFinalize(this);
    }
}
