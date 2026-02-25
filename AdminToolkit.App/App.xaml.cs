using System.Windows;
using System.Windows.Threading;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Wpf.Ui;
using AdminToolkit.App.Services;
using AdminToolkit.App.ViewModels;
using AdminToolkit.App.Views;

namespace AdminToolkit.App;

public partial class App : Application
{
    private static readonly IHost _host = Host.CreateDefaultBuilder()
        .ConfigureServices((_, services) =>
        {
            services.AddSingleton<INavigationService, NavigationService>();
            services.AddSingleton<ISnackbarService, SnackbarService>();
            services.AddSingleton<IContentDialogService, ContentDialogService>();

            services.AddSingleton<AuthService>();
            services.AddSingleton<GraphService>();
            services.AddSingleton<ScriptRunnerService>();
            services.AddSingleton<DbService>();
            services.AddSingleton<LoggingService>();

            services.AddSingleton<MainViewModel>();
            services.AddTransient<DashboardViewModel>();
            services.AddTransient<UsersViewModel>();
            services.AddTransient<GroupsViewModel>();
            services.AddTransient<SharedMailboxesViewModel>();
            services.AddTransient<RunbookViewModel>();
            services.AddTransient<SettingsViewModel>();

            services.AddSingleton<MainWindow>();
            services.AddTransient<DashboardPage>();
            services.AddTransient<UsersPage>();
            services.AddTransient<GroupsPage>();
            services.AddTransient<SharedMailboxesPage>();
            services.AddTransient<RunbookPage>();
            services.AddTransient<SettingsPage>();
        })
        .Build();

    public static IServiceProvider Services => _host.Services;

    protected override async void OnStartup(StartupEventArgs e)
    {
        await _host.StartAsync();

        var dbService = Services.GetRequiredService<DbService>();
        dbService.Initialize();

        var mainWindow = Services.GetRequiredService<MainWindow>();
        mainWindow.Show();

        base.OnStartup(e);
    }

    protected override async void OnExit(ExitEventArgs e)
    {
        await _host.StopAsync();
        _host.Dispose();
        base.OnExit(e);
    }

    private void OnDispatcherUnhandledException(object sender, DispatcherUnhandledExceptionEventArgs e)
    {
        MessageBox.Show(
            $"An unexpected error occurred:\n\n{e.Exception.Message}",
            "Error",
            MessageBoxButton.OK,
            MessageBoxImage.Error);
        e.Handled = true;
    }
}
