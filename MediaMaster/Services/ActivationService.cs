using Windows.ApplicationModel.Activation;
using Windows.Storage;
using MediaMaster.DataBase;
using MediaMaster.Interfaces.Services;
using MediaMaster.Services.Navigation;
using Microsoft.Windows.AppLifecycle;
using Microsoft.Windows.AppNotifications;
using WinUI3Localizer;
using WinUICommunity;
using MediaMaster.Views.Flyout;
using Microsoft.UI.Xaml.Media.Animation;
using WinUIEx;
using HomePage = MediaMaster.Views.HomePage;
using ShellPage = MediaMaster.Views.ShellPage;
using MediaMaster.Views.Dialog;

namespace MediaMaster.Services;

public class ActivationService : IActivationService
{
    private readonly ContextMenuService _menuService = new();

    private bool _contextMenuLock = true;

    public async Task ActivateAsync()
    {
        var args = await HandleActivationAsync();

        if (args is not null)
        {
            await LaunchApp(args);
        }

        App.GetService<ITranslationService>().LanguageChanged += async (_, _) => await ResetContextMenu();

#if !DEBUG
        if (!App.GetService<SettingsService>().TutorialWasShown)
#endif
        {
            App.GetService<ITeachingService>().Start();
        }

        if (App.Flyout == null)
        {
            App.Flyout = new FlyoutWindow();
            App.GetService<TrayIconService>().SetInTray();
        }
    }

    public async Task<string?> HandleActivationAsync(AppActivationArguments? activationArgs = null)
    {
        activationArgs ??= AppInstance.GetCurrent().GetActivatedEventArgs();

        switch (activationArgs.Kind)
        {
            case ExtendedActivationKind.AppNotification:
                if (activationArgs.Data is AppNotificationActivatedEventArgs notificationArgs)
                {
                    await App.GetService<IAppNotificationService>().HandleNotificationAsync(notificationArgs);
                    return null;
                }

                break;

            case ExtendedActivationKind.Launch:
                if (activationArgs.Data is ILaunchActivatedEventArgs launchArgs)
                {
                    return launchArgs.Arguments;
                }

                break;

            case ExtendedActivationKind.StartupTask:
                await App.GetService<SettingsService>().InitializeAsync();
                return App.GetService<SettingsService>().LeaveAppRunning ? "--NoWindow" : "";

            default:
                return "";
        }

        return "";
    }

    public async Task CreateWindow()
    {
        if (App.MainWindow == null)
        {
            await App.GetService<IThemeSelectorService>().InitializeAsync();
            App.MainWindow = new MainWindow();
        }
    }

    public async Task LoadServices()
    {
        await using (MediaDbContext database = new())
        {
            await database.InitializeAsync();
        }

        await Task.WhenAll(
            App.GetService<ITranslationService>().InitializeAsync(),
            App.GetService<SettingsService>().InitializeAsync(),
            ResetContextMenu()
        );
    }

    public async Task LoadWindow()
    {
        if (App.MainWindow is null) return;

        App.MainWindow.Content = App.GetService<ShellPage>();
        App.MainWindow.InitializeTheme();
        App.GetService<MainNavigationService>().NavigateTo(typeof(HomePage).FullName);

        await BrowserService.InitializeAsync();
    }

    public async Task LaunchApp(string args)
    {
        Dictionary<string, string?> parameters = GetParams(args);

        var noMainWindow = App.MainWindow == null;
        if (noMainWindow)
        {
            await CreateWindow();
        }

        if (parameters.ContainsKey("NoWindow"))
        {
            if (noMainWindow)
            {
                await LoadServices();
                if (App.GetService<SettingsService>().LeaveAppRunning)
                {
                    ShowAppRunningInBackgroundPopup();
                }
            }
            else if (App.MainWindow?.Visible == true)
            {
                App.MainWindow.Show();
            }
        }
        else
        {
            App.MainWindow?.Show();
            if (noMainWindow)
            {
                await LoadServices();
            }
        }


        parameters.TryGetValue("Files", out var filesValue);
        if (filesValue != null)
        {
            ICollection<string> mediaPaths = filesValue.Split(", ").Select(f => f.Replace("\"", "").Trim()).ToList();

            if (App.MainWindow?.Visible == true)
            {
                App.MainWindow.SetForegroundWindow();
                await AddMediasDialog.ShowDialogAsync(mediaPaths);
            }
            else
            {
                if (App.Flyout == null)
                {
                    App.Flyout = new FlyoutWindow();
                    App.GetService<TrayIconService>().SetInTray();
                }

                if (!App.Flyout.IsOpen)
                {
                    App.Flyout.AutoClose = true;
                }

                App.Flyout.ShowFlyout();
                App.GetService<FlyoutNavigationService>().NavigateTo(typeof(AddMediasPage).FullName!, mediaPaths, new SlideNavigationTransitionInfo { Effect = SlideNavigationTransitionEffect.FromRight });
            }
        }

        if (noMainWindow)
        {
            await LoadWindow();
        }
        
    }

    public void ShowAppRunningInBackgroundPopup()
    {
#if !DEBUG
        if (App.GetService<SettingsService>().RunInBackgroundPopupShown) return;
#endif

        var xmlPayload = $"""
                      <toast launch="action=edit">
                          <visual>
                              <binding template="ToastGeneric">
                                  <text>{"InAppNotification_AppMinimized_Title".GetLocalizedString()}</text>
                                  <text>{"InAppNotification_AppMinimized_SubTitle".GetLocalizedString()}</text>
                              </binding>
                          </visual>
                          <actions>
                              <action activationType="system" arguments="dismiss" content="{"Dismiss_ToastButton".GetLocalizedString()}"/>
                          </actions>
                      </toast>
                      """;

        var toast = new AppNotification(xmlPayload)
        {
            ExpiresOnReboot = true
        };

        var notificationManager = AppNotificationManager.Default;
        notificationManager.Show(toast);
        App.GetService<SettingsService>().RunInBackgroundPopupShown = true;
    }

    private async Task ResetContextMenu()
    {
        if (_contextMenuLock)
        {
            _contextMenuLock = false;
            ContextMenuItem menu = new()
            {
                Title = $"{"add_context_menu".GetLocalizedString()}",
                AcceptDirectory = true,
                AcceptFileFlag = 1,
                AcceptExts = "*",
                AcceptMultipleFilesFlag = 2,
                PathDelimiter = ", ",
                Exe = "MediaMaster.exe",
                Param = "--Files \"{path}\" --NoWindow",
                Icon = $"{AppContext.BaseDirectory}Assets\\WindowIcon.ico"
            };

            _menuService.ClearCache();
            StorageFolder? menuFolder = await _menuService.GetMenusFolderAsync();
            foreach (var file in Directory.EnumerateFiles(menuFolder.Path))
            {
                File.Delete(file);
            }

            await _menuService.SaveAsync(menu);
            await _menuService.BuildToCacheAsync();
            _menuService.SetCustomMenuName($"{"add_context_menu".GetLocalizedString()} (Win 11)");
        }

        _contextMenuLock = true;
    }

    private static Dictionary<string, string?> GetParams(string args)
    {
        Dictionary<string, string?> parameters = [];
        args = args.Trim();

        while (args.Contains("--"))
        {
            args = args[(args.IndexOf("--", StringComparison.Ordinal) + 2)..];

            string param;
            if (args.Contains(' '))
            {
                param = args[..args.IndexOf(' ')];
            }
            else
            {
                parameters[args] = null;
                break;
            }

            args = args[(param.Length + 1)..].Trim();

            var value = args.Contains("--") ? args[..args.IndexOf("--", StringComparison.Ordinal)] : args;
            parameters[param] = value.Length == 0 ? null : value;

            args = args.Contains("--") ? args[args.IndexOf("--", StringComparison.Ordinal)..] : "";
        }

        return parameters;
    }
}