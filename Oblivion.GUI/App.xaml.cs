using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Oblivion.Data.Db;
using Oblivion.Data.Repositories;
using Oblivion.GUI.MVVM.View;
using Oblivion.GUI.MVVM.ViewModel;
using Oblivion.GUI.Services;
using Oblivion.Interpop;
using System;
using System.IO;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Threading;
using ISnackbarService = Wpf.Ui.ISnackbarService;
using SnackbarService = Wpf.Ui.SnackbarService;

namespace Oblivion.GUI
{
    /// <summary>
    /// Interaction logic for App.xaml
    /// </summary>
    public partial class App : Application
    {
        public static IHost AppHost { get; private set; }

        public App()
        {

            var dbPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "Oblivion", "Oblivion.db");

            Directory.CreateDirectory(Path.GetDirectoryName(dbPath)!);

            AppHost = Host.CreateDefaultBuilder()
                .ConfigureServices((host, services) =>
                {

                    services.AddDbContextFactory<OblivionDbContext>(options =>
                    options.UseSqlite($"Data Source={dbPath}", b => b.MigrationsAssembly("Oblivion.Data")));

                    //ViewModels
                    services.AddSingleton<ShellViewModel>();

                    //Services
                    services.AddSingleton<AppNavigationService>();
                    services.AddSingleton<OblivionApiService>();
                    services.AddSingleton<ThemeService>();
                    services.AddSingleton<PdfExportService>();
                    services.AddSingleton<FileExportService>();
                    services.AddSingleton<ISnackbarService, SnackbarService>();
                    services.AddSingleton<NotificationService>();

                    //Repositories
                    services.AddSingleton<WorkspaceRepository>();
                    services.AddSingleton<FileRepository>();

                    //View
                    services.AddSingleton<Shell>();
                    services.AddTransient<StartupView>();
                })
                .Build();
        }

        protected override void OnStartup(StartupEventArgs e)
        {
            DispatcherUnhandledException += OnDispatcherUnhandledException;
            AppDomain.CurrentDomain.UnhandledException += OnAppDomainUnhandledException;
            TaskScheduler.UnobservedTaskException += OnUnobservedTaskException;

            var shell = AppHost.Services.GetRequiredService<Shell>();
            shell.DataContext = AppHost.Services.GetService<ShellViewModel>();
            shell.Show();

            var snackbar = AppHost.Services.GetRequiredService<ISnackbarService>();
            snackbar.SetSnackbarPresenter(shell.SnackBarPres);

            var themeService = AppHost.Services.GetRequiredService<ThemeService>();
            themeService.LoadFromFile();

            base.OnStartup(e);
        }

        private void OnDispatcherUnhandledException(object sender, DispatcherUnhandledExceptionEventArgs e)
        {
            LogException("UI", e.Exception);
            ShowErrorNotification(e.Exception);
            e.Handled = true;
        }

        private void OnAppDomainUnhandledException(object sender, UnhandledExceptionEventArgs e)
        {
            if (e.ExceptionObject is Exception ex)
                LogException("AppDomain", ex);
        }

        private void OnUnobservedTaskException(object? sender, UnobservedTaskExceptionEventArgs e)
        {
            LogException("Task", e.Exception.GetBaseException());
            e.SetObserved();
        }

        private static void LogException(string source, Exception ex)
        {
            try
            {
                var logDir = Path.Combine(
                    Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
                    "Oblivion");
                Directory.CreateDirectory(logDir);

                var logPath = Path.Combine(logDir, "error.log");
                var entry = $"[{DateTime.Now:yyyy-MM-dd HH:mm:ss}] [{source}] {ex.GetType().Name}: {ex.Message}\n{ex.StackTrace}\n\n";
                File.AppendAllText(logPath, entry);
            }
            catch
            {
                // Logging itself must never throw
            }
        }

        private static void ShowErrorNotification(Exception ex)
        {
            try
            {
                var notify = AppHost.Services.GetService<NotificationService>();
                notify?.Error("Unexpected Error", ex.Message);
            }
            catch
            {
                // Notification may not be available yet
            }
        }
    }

}
