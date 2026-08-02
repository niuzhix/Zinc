using Avalonia;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Data.Core;
using Avalonia.Data.Core.Plugins;
using Avalonia.Markup.Xaml;
using System;
using System.Linq;
using Zinc.ViewModels;
using Zinc.Views;

namespace Zinc
{
    public partial class App : Application
    {
        public override void Initialize()
        {
            AvaloniaXamlLoader.Load(this);
        }

        public override void OnFrameworkInitializationCompleted()
        {
#if DEBUG
            LaunchDebugConsole();
#endif
            if (ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
            {
                desktop.MainWindow = new MainWindow
                {
                    DataContext = new MainWindowViewModel(),
                };
            }

            base.OnFrameworkInitializationCompleted();
        }

        public static void LaunchDebugConsole()
        {
            Console.ForegroundColor = ConsoleColor.DarkCyan;
            Console.WriteLine(@"
     ███████╗██╗███╗   ██╗ ██████╗ 
     ╚══███╔╝██║████╗  ██║██╔════╝
       ███╔╝ ██║██╔██╗ ██║██║     
      ███╔╝  ██║██║╚██╗██║██║     
     ███████╗██║██║ ╚████║╚██████╗
     ╚══════╝╚═╝╚═╝  ╚═══╝ ╚═════╝
            ");
            Console.ForegroundColor = ConsoleColor.Gray;
            Console.WriteLine($"    Zinc Editor v{GetVersion()}\n");
            Console.WriteLine($"Start time:    {DateTime.Now:yyyy-MM-dd HH:mm:ss}");
            Console.WriteLine($"Platform:      {Environment.OSVersion.Platform}");
            Console.ForegroundColor = ConsoleColor.Yellow;
            Console.WriteLine("Debug console started.\n");
            Console.ForegroundColor = ConsoleColor.White;
        }
        private static string GetVersion()
        {
            var version = typeof(Program).Assembly.GetName().Version;
            return version?.ToString() ?? "1.0.0";
        }
    }
}