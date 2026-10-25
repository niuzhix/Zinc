using Avalonia;
using System;
using System.Diagnostics;
using System.Runtime.InteropServices;

namespace Zinc;

internal sealed class Program
{
    [DllImport("kernel32.dll")]
    private static extern IntPtr GetConsoleWindow();

    [DllImport("user32.dll")]
    private static extern bool ShowWindow(IntPtr hWnd, int nCmdShow);

    private const int SW_HIDE = 0;
    private const int SW_SHOW = 5;

    [STAThread]
    public static void Main(string[] args)
    {
        // 调试时或显式传入 --console 时显示控制台窗口。
        bool consoleVisible = Debugger.IsAttached || Array.IndexOf(args, "--console") >= 0;
        if (consoleVisible)
        {
            ShowConsole();
        }

        try
        {
            BuildAvaloniaApp()
                .StartWithClassicDesktopLifetime(args);
        }
        finally
        {
            // 退出时隐藏本次打开的控制台窗口。
            if (consoleVisible)
            {
                HideConsole();
            }
        }
    }

    // Avalonia 启动配置；可视化设计器也会调用，请勿删除。
    public static AppBuilder BuildAvaloniaApp()
        => AppBuilder.Configure<App>()
           .UsePlatformDetect()
#if DEBUG
           .WithDeveloperTools()
#endif
           .WithInterFont()
           .LogToTrace();

    private static void ShowConsole()
    {
        if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
        {
            var handle = GetConsoleWindow();
            if (handle == IntPtr.Zero)
            {
                AllocConsole();
            }
            else
            {
                ShowWindow(handle, SW_SHOW);
            }
        }
    }

    private static void HideConsole()
    {
        if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
        {
            var handle = GetConsoleWindow();
            if (handle != IntPtr.Zero)
            {
                ShowWindow(handle, SW_HIDE);
            }
        }
    }

    [DllImport("kernel32.dll", SetLastError = true)]
    private static extern bool AllocConsole();
}