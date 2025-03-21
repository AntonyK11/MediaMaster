using System.Diagnostics.CodeAnalysis;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using MediaMaster.Interfaces.Services;
using MediaMaster.Services;
using MediaMaster.ViewModels;
using MediaMaster.Views;
using Microsoft.UI.Dispatching;
using Windows.Storage;
using System.Text.Json;
using MediaMaster.DataBase;
using MediaMaster.Services.Navigation;
using MediaMaster.Helpers;
using WinUIEx;

namespace MediaMaster;

/// <summary>
///     Provides application-specific behavior to supplement the default Application class.
/// </summary>
public sealed partial class App : Application
{
    private IHost Host { get; }

    public static T GetService<T>()
        where T : class
    {
        if ((Current as App)!.Host.Services.GetService(typeof(T)) is not T service)
        {
            throw new ArgumentException($"{typeof(T)} needs to be registered in ConfigureServices within App.xaml.cs.");
        }

        return service;
    }

    public static MainWindow? MainWindow { get; set; }

    public static FlyoutWindow? Flyout { get; set; }

    public static readonly DispatcherQueue DispatcherQueue = DispatcherQueue.GetForCurrentThread();

   // Used to support dynamic language for trimming
   [method: DynamicDependency(DynamicallyAccessedMemberTypes.PublicProperties, typeof(ContentDialog))]
   [method: DynamicDependency(DynamicallyAccessedMemberTypes.PublicProperties, typeof(MenuFlyoutItem))]
   [method: DynamicDependency(DynamicallyAccessedMemberTypes.PublicProperties, typeof(TextBox))]
   [method: DynamicDependency(DynamicallyAccessedMemberTypes.PublicProperties, typeof(SelectorBarItem))]
   [method: DynamicDependency(DynamicallyAccessedMemberTypes.PublicProperties, typeof(MenuBarItem))]
   [method: DynamicDependency(DynamicallyAccessedMemberTypes.PublicProperties, typeof(ComboBox))]
    public App()
    {
        this.InitializeComponent();

        Host = Microsoft.Extensions.Hosting.Host.
            CreateDefaultBuilder().
            UseContentRoot(AppContext.BaseDirectory).
            ConfigureServices((_, services) =>
            {
                // Services
                services.AddSingleton<IAppNotificationService, AppNotificationService>();
                services.AddSingleton<IThemeSelectorService, ThemeSelectorService>();
                services.AddSingleton<ITranslationService, TranslationService>();
                services.AddSingleton<SettingsService>();
                services.AddSingleton<MainNavigationViewService>();

                services.AddSingleton<IActivationService, ActivationService>();
                services.AddSingleton<IPageService, PageService>();
                services.AddSingleton<MainNavigationService>();
                services.AddSingleton<FlyoutNavigationService>();

                services.AddSingleton<ITeachingService, TeachingService>();
                services.AddSingleton<BrowserService>();

                // Views and ViewModels
                services.AddSingleton<ShellViewModel>();
                services.AddSingleton<ShellPage>();
                services.AddTransient<HomePage>();
                services.AddTransient<SettingsViewModel>();
                services.AddTransient<SettingsPage>();

                services.AddSingleton<FlyoutWindow>();
                services.AddSingleton<Views.Flyout.ShellPage>();
                services.AddSingleton<ViewModels.Flyout.ShellViewModel>();
                services.AddSingleton<Views.Flyout.HomePage>();
                services.AddSingleton<ViewModels.Flyout.HomeViewModel>();
                services.AddSingleton<Views.Flyout.AddMediasPage>();
                services.AddSingleton<ViewModels.Flyout.AddMediasViewModel>();

                services.AddSingleton<TrayIconService>();
                services.AddSingleton<TasksService>();

                services.AddSingleton<SearchSavingService>(); 
            }).Build();

        GetService<IAppNotificationService>().Initialize();

        // Fixes the issue where the mica background would flicker to the system theme if the application theme was not the same
        if (ApplicationData.Current.LocalSettings.Values.TryGetValue(ThemeSelectorService.SettingsKey, out var obj))
        {
            var theme = JsonSerializer.Deserialize((string)obj, SourceGenerationContext.Default.ElementTheme);

            RequestedTheme = theme switch
            {
                ElementTheme.Dark => ApplicationTheme.Dark,
                ElementTheme.Light => ApplicationTheme.Light,
                _ => RequestedTheme
            };
        }

        UnhandledException += App_UnhandledException;
    }

    protected override async void OnLaunched(LaunchActivatedEventArgs args)
    {
        base.OnLaunched(args);

        await GetService<IActivationService>().ActivateAsync();
    }

    private void App_UnhandledException(object sender, Microsoft.UI.Xaml.UnhandledExceptionEventArgs e)
    {
        Debug.WriteLine(e.Exception);
    }

    public static void Shutdown()
    {
        if (!GetService<TasksService>().IsTaskRunning())
        {
            GetService<TrayIconService>().Dispose();
            Flyout?.Close();
            MainWindow?.Close();
            Current.Exit();
            //Environment.Exit(0);
        }
        else
        {
            GetService<TasksService>().NoMoreTasksRunning += RegisteredShutdown;
        }
    }

    private static void RegisteredShutdown(object sender, object? args)
    {
        GetService<TasksService>().NoMoreTasksRunning -= RegisteredShutdown;
        if (MainWindow?.GetWindowStyle().HasFlag(WindowStyle.Visible) == false && Flyout?.IsOpen == false && !GetService<SettingsService>().LeaveAppRunning)
        {
            Shutdown();
        }
    }
}