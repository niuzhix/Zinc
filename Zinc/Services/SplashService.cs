using Avalonia.Media;
using Avalonia.Media.Imaging;
using Avalonia.Platform;
using FluentAvalonia.UI.Windowing;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace Zinc.Services;

/// <summary>启动画面配置。</summary>
public class ZincSplashScreen : IFAApplicationSplashScreen
{
    private const int MinimumShowTimeMilliseconds = 1000;
    private const string SplashImageUri = "avares://Zinc/Assets/splash.png";

    /// <summary>图标只解码一次，避免每次读取属性都新建位图。</summary>
    private static readonly Lazy<IImage> SplashImage =
        new(() => new Bitmap(AssetLoader.Open(new Uri(SplashImageUri))));

    public IImage? AppIcon => SplashImage.Value;

    public string? AppName => null;

    public object? SplashScreenContent => null;

    public int MinimumShowTime => MinimumShowTimeMilliseconds;

    public Task RunTasks(CancellationToken cancellationToken) => Task.CompletedTask;
}
