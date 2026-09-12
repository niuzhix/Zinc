using Avalonia;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Markup.Xaml;
using FluentAvalonia.Styling;
using Microsoft.Extensions.DependencyInjection;
using System;
using Zinc.Abstractions;
using Zinc.Core.Abstractions;
using Zinc.Core.Models;
using Zinc.Core.Services;
using Zinc.Models;
using Zinc.Services;
using Zinc.ViewModels;
using Zinc.Views;

namespace Zinc
{
    public partial class App : Application
    {
        public static IServiceProvider Services { get; private set; } = null!;

        public static FluentAvaloniaTheme? Theme { get; private set; }

        public static string? AppVersion { get; set; }

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
                var services = new ServiceCollection();

                services.AddSingleton<ISettingsService<AppSettings>>(
                    _ => new SettingsService<AppSettings>("Zinc"));

                services.AddSingleton<IDialogService, DialogService>();
                services.AddSingleton<IFileService, FileService>();
                services.AddSingleton<IJudgeService, JudgeService>();
                services.AddSingleton<IProgramService, ProgramService>();

                services.AddSingleton<MainWindowViewModel>();
                services.AddSingleton<MainViewModel>();
                services.AddTransient<SPUIViewModel>();
                services.AddTransient<SPCompileViewModel>();
                services.AddTransient<SPAboutViewModel>();
                services.AddTransient<EditorViewModel>();

                Services = services.BuildServiceProvider();

                desktop.MainWindow = new MainWindow
                {
                    DataContext = ActivatorUtilities.CreateInstance<MainWindowViewModel>(App.Services)
                };

                Theme = App.Current.Styles[0] as FluentAvaloniaTheme;
                base.OnFrameworkInitializationCompleted();
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
            AppVersion = version?.ToString() ?? "1.0.0";
            return AppVersion;
        }
    }
}