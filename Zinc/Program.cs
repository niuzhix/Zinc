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
        // Show Debug Console when debugging or explicitly requested with --console.
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
            // Hide the console we opened on exit.
            if (consoleVisible)
            {
                HideConsole();
            }
        }
    }

    // Avalonia configuration, don't remove; also used by visual designer.
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