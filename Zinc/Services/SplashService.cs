using Avalonia.Media;
using Avalonia.Media.Imaging;
using Avalonia.Platform;
using FluentAvalonia.UI.Windowing;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace Zinc.Services;

public class ZincSplashScreen : IFAApplicationSplashScreen
{
    public IImage? AppIcon => new Bitmap(
        AssetLoader.Open(new Uri("avares://Zinc/Assets/splash.png")));
    public string? AppName => null;
    public object? SplashScreenContent => null;
    public int MinimumShowTime => 1000;
    public Task RunTasks(CancellationToken cancellationToken) => Task.CompletedTask;
}
