using System.Diagnostics;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using MediaMaster.Interfaces.Services;
using MediaMaster.Services;
using MediaMaster.ViewModels;
using MediaMaster.Views;
using Microsoft.UI.Dispatching;
using MediaMaster.DataBase;
using Microsoft.UI.Xaml;
using H.NotifyIcon;
using Windows.Storage;
using System.Text.Json;
using MediaMaster.Services.Navigation;


namespace MediaMaster;

/// <summary>
///     Provides application-specific behavior to supplement the default Application class.
/// </summary>
public partial class App : Application
{
    private IHost Host { get; }

    public static T GetService<T>()
        where T : class
    {
        if ((Current as App)!.Host.Services.GetService(typeof(T)) is not T service)
        {
            throw new ArgumentException(
                $"{typeof(T)} needs to be registered in ConfigureServices within App.xaml.cs.");
        }

        return service;
    }

    public static MainWindow? MainWindow;

    public static FlyoutWindow? Flyout;

    public static TaskbarIcon? TrayIcon;

    public static readonly DispatcherQueue DispatcherQueue = DispatcherQueue.GetForCurrentThread();

    public App()
    {
        InitializeComponent();

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
                services.AddTransient<MainNavigationViewService>();

                services.AddSingleton<IActivationService, ActivationService>();
                services.AddSingleton<IPageService, PageService>();
                services.AddSingleton<MainNavigationService>();
                services.AddSingleton<FlyoutNavigationService>();

                services.AddSingleton<ITeachingService, TeachingService>();
                services.AddSingleton<BrowserService>();

                // Views and ViewModels
                services.AddSingleton<ShellViewModel>();
                services.AddSingleton<ShellPage>();
                services.AddSingleton<HomePage>();
                services.AddSingleton<CategoriesPage>();
                services.AddSingleton<CategoriesViewModel>();
                services.AddTransient<SettingsViewModel>();
                services.AddTransient<SettingsPage>();

                services.AddSingleton<FlyoutWindow>();
                services.AddSingleton<Views.Flyout.ShellPage>();
                services.AddSingleton<ViewModels.Flyout.ShellViewModel>();
                services.AddSingleton<Views.Flyout.HomePage>();
                services.AddSingleton<ViewModels.Flyout.HomeViewModel>();
            }).Build();

        GetService<IAppNotificationService>().Initialize();

        // Fixes the issue where the mica background would flicker to the system theme if the application theme was not the same
        if (ApplicationData.Current.LocalSettings.Values.TryGetValue(ThemeSelectorService.SettingsKey, out var obj))
        {
            var theme = JsonSerializer.Deserialize<ElementTheme>((string)obj);

            if (theme is ElementTheme.Dark)
            {
                RequestedTheme = ApplicationTheme.Dark;
            }
            else if (theme is ElementTheme.Light)
            {
                RequestedTheme = ApplicationTheme.Light;
            }
        }

        UnhandledException += App_UnhandledException;
    }

    protected override async void OnLaunched(LaunchActivatedEventArgs args)
    {
        // TODO: Remove
        if (await WinUI3XamlPreview.Preview.IsXamlPreviewLaunched())
        {
            return;
        }
        base.OnLaunched(args);

        await GetService<IActivationService>().ActivateAsync();
        //Thread thread = new(async () => { await GetService<DataBaseService>().InitializeAsync(); })
        //{
        //    Name = "DataBaseService"
        //};
        //thread.Start();
        //await Task.Run(() => GetService<DataBaseService>().InitializeAsync());
        // await db.InitializeAsync();

        //await GetService<DataBaseService>().InitializeAsync();

        //var activatedArgs = AppInstance.GetCurrent().GetActivatedEventArgs();
        //switch (activatedArgs.Kind)
        //{
        //    case ExtendedActivationKind.AppNotification:
        //        await GetService<IAppNotificationService>().HandleNotificationAsync((AppNotificationActivatedEventArgs)activatedArgs.Data);
        //        break;
        //    case ExtendedActivationKind.Launch:
        //    {
        //        LaunchActivatedEventArgs? launchArgs = (LaunchActivatedEventArgs)activatedArgs.Data;
        //        await LaunchApp(launchArgs.Arguments);
        //        break;
        //    }
        //    case ExtendedActivationKind.StartupTask:
        //        if (GetService<SettingsService>().LeaveAppRunning)
        //        {
        //            await LaunchApp("--NoWindow");
        //        }
        //        else
        //        {
        //            await LaunchApp("");
        //        }
        //        break;
        //    default:
        //        await LaunchApp("");
        //        break;
        //}

        //await LoadWindow();

        //GetService<ITranslationService>().LanguageChanged += async (_, _) => await ResetContextMenu();

        //RegistryKey? _key = Registry.CurrentUser.OpenSubKey("SOFTWARE\\Classes\\Folder\\Shell", true);
        //System.Diagnostics.Debug.WriteLine(_key);
        //RegistryKey newkey = _key.CreateSubKey("My Menu Item");
        //newkey.SetValue("AppliesTo", "under:C:");
        //System.Diagnostics.Debug.WriteLine(newkey);

        //RegistryKey subNewkey = newkey.CreateSubKey("Command");
        //subNewkey.SetValue("", "MediaMaster.exe");
        //System.Diagnostics.Debug.WriteLine(subNewkey);
        //subNewkey.Close();

        //newkey.Close();
        //_key.Close();

        //_key = Registry.CurrentUser.OpenSubKey("SOFTWARE\\Classes\\Directory\\Shell", true);
        //System.Diagnostics.Debug.WriteLine(_key);
        //newkey = _key.CreateSubKey("My Menu Item");
        //newkey.SetValue("AppliesTo", "under:C:");
        //System.Diagnostics.Debug.WriteLine(newkey);

        //subNewkey = newkey.CreateSubKey("Command");
        //subNewkey.SetValue("", "MediaMaster.exe");
        //System.Diagnostics.Debug.WriteLine(subNewkey);
        //subNewkey.Close();

        //newkey.Close();
        //_key.Close();

        //_key = Registry.CurrentUser.OpenSubKey("SOFTWARE\\Classes\\Directory\\Background\\Shell", true);
        //System.Diagnostics.Debug.WriteLine(_key);
        //newkey = _key.CreateSubKey("My Menu Item");
        //newkey.SetValue("AppliesTo", "under:C:");
        //System.Diagnostics.Debug.WriteLine(newkey);

        //subNewkey = newkey.CreateSubKey("Command");
        //subNewkey.SetValue("", "MediaMaster.exe");
        //System.Diagnostics.Debug.WriteLine(subNewkey);
        //subNewkey.Close();

        //newkey.Close();
        //_key.Close();
    }

    private void App_UnhandledException(object sender, Microsoft.UI.Xaml.UnhandledExceptionEventArgs e)
    {
        Debug.WriteLine(e.Exception);
    }

    public static void Shutdown()
    {
        FlyoutService.Dispose();
        ((App)Current).Exit();
        Environment.Exit(0);
    }
}