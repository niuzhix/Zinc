using Avalonia;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Markup.Xaml;
using FluentAvalonia.Styling;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Linq;
using Zinc.Abstractions;
using Zinc.Core.Abstractions;
using Zinc.Core.Models;
using Zinc.Core.Services;
using Zinc.Models;
using Zinc.Services;
using Zinc.ViewModels;
using Zinc.Views;
using Zinc.Views.SettingsPages;

namespace Zinc
{
    public partial class App : Application
    {
        public static IServiceProvider Services { get; private set; } = null!;

        public static FluentAvaloniaTheme? Theme { get; private set; }

        /// <summary>应用版本号，供“关于”页显示。</summary>
        public static string AppVersion { get; } = GetVersion();

        public override void Initialize()
        {
            AvaloniaXamlLoader.Load(this);
        }

        public override void OnFrameworkInitializationCompleted()
        {
#if DEBUG
            LaunchDebugConsole();
#endif
            var services = new ServiceCollection();

            services.AddSingleton<ISettingsService<AppSettings>>(
                _ => new SettingsService<AppSettings>("Zinc"));

            services.AddSingleton<IDialogService, DialogService>();
            services.AddSingleton<IFileService, FileService>();
            services.AddSingleton<IJudgeService, JudgeService>();
            services.AddSingleton<IFormatService, FormatService>();
            services.AddSingleton<IProgramService, ProgramService>();
            services.AddSingleton<IEditorViewModelFactory, EditorViewModelFactory>();
            services.AddSingleton<IWindowService, WindowService>();

            services.AddSingleton<MainWindowViewModel>();
            services.AddSingleton<MainViewModel>();
            services.AddTransient<SettingsWindowViewModel>();
            services.AddTransient<SPUIViewModel>();
            services.AddTransient<SPEditViewModel>();
            services.AddTransient<SPCompileViewModel>();
            services.AddTransient<SPAboutViewModel>();

            Services = services.BuildServiceProvider();

            Theme = Current?.Styles.OfType<FluentAvaloniaTheme>().FirstOrDefault();

            // 启动时一次性校正持久化的编辑器字体，避免各消费方拿到不可用的字体。
            EditorFontResolver.EnsureUsable(
                Services.GetRequiredService<ISettingsService<AppSettings>>().Current);

            if (ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
            {
                desktop.MainWindow = new MainWindow
                {
                    DataContext = Services.GetRequiredService<MainWindowViewModel>()
                };

                // 设置写入是防抖的，退出时释放容器以冲刷尚未落盘的改动。
                desktop.Exit += (_, _) => (Services as IDisposable)?.Dispose();
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
            Console.WriteLine($"    Zinc Editor v{AppVersion}\n");
            Console.WriteLine($"Start time:    {DateTime.Now:yyyy-MM-dd HH:mm:ss}");
            Console.WriteLine($"Platform:      {Environment.OSVersion.Platform}");
            Console.ForegroundColor = ConsoleColor.Yellow;
            Console.WriteLine("Debug console started.\n");
            Console.ForegroundColor = ConsoleColor.White;
        }

        private static string GetVersion()
        {
            var version = typeof(App).Assembly.GetName().Version;
            return version?.ToString() ?? "1.0.0";
        }
    }
}
