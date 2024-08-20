using MediaMaster.Interfaces.Services;
using MediaMaster.Views;
using Windows.Storage;
using MediaMaster.DataBase;
using Microsoft.Windows.AppNotifications;
using Microsoft.Windows.AppLifecycle;
using Windows.ApplicationModel.Activation;
using WinUI3Localizer;
using WinUICommunity;
using MediaMaster.Services.Navigation;

namespace MediaMaster.Services;

public class ActivationService : IActivationService
{
    public async Task ActivateAsync()
    {
        // Handle activation via ActivationHandlers.
        var args =  await HandleActivationAsync();

        //// Execute tasks before activation.
        //await InitializeAsync();

        //// Set the MainWindow Content.

        //// Activate the MainWindow.
        //App.MainWindow?.Activate();

        //// Execute tasks after activation.
        //await StartupAsync();

        if (args is not null)
        {
            await LaunchApp(args);
        }

        App.GetService<ITranslationService>().LanguageChanged += async (_, _) => await ResetContextMenu();

        App.Flyout = new FlyoutWindow();
        App.GetService<TrayIconService>().SetInTray();
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
        //await Task.Delay(10);
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

        await App.GetService<BrowserService>().InitializeAsync();
    }

    private bool _contextMenuLock = true;
    private readonly ContextMenuService _menuService = new();

    private async Task ResetContextMenu()
    {
        if (_contextMenuLock)
        {
            _contextMenuLock = false;
            var installationPath = AppContext.BaseDirectory;
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
                Icon = $"{installationPath}/Assets/WindowIcon.ico"
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

    public async Task LaunchApp(string args)
    {
        Dictionary<string, string?> parameters = GetParams(args);

        if (parameters.TryGetValue("Files", out var filesValue))
        {
            if (filesValue is null) return;

            if (App.MainWindow?.Visible == true)
            {
                App.MainWindow.Show();
                var files = filesValue.Split(", ").Select(f => f.Replace("\"", "").Trim());
                await Task.Run(() => MediaService.AddMediaAsync(files));
            }
            else
            {
                var noMainWindow = App.MainWindow == null;
                if (noMainWindow)
                {
                    await LoadServices();
                }


                if (!MediaService.IsRunning)
                {
                    var xmlPayload = $"""
                                        <toast launch="action=edit">
                                            <visual>
                                                <binding template="ToastGeneric">
                                                    <text>{"adding_medias_toast_text".GetLocalizedString()}</text>
                                                </binding>
                                            </visual>
                                            <actions>
                                                <action activationType="system" arguments="dismiss" content="{"dismiss_toast_button".GetLocalizedString()}"/>
                                            </actions>
                                        </toast>
                                        """;

                    var toast = new AppNotification(xmlPayload)
                    {
                        ExpiresOnReboot = true
                    };

                    var notificationManager = AppNotificationManager.Default;
                    notificationManager.Show(toast);

                    var files = filesValue.Split(", ").Select(f => f.Replace("\"", "").Trim());
                    var mediaAddedCount = await Task.Run(() => MediaService.AddMediaAsync(files));

                    if (!App.GetService<SettingsService>().DoNotSendMediaAddedConfirmationNotification)
                    {
                        xmlPayload = $"""
                                          <toast launch="action=edit">
                                              <visual>
                                                  <binding template="ToastGeneric">
                                                      <text>{string.Format("medias_added_toast_text".GetLocalizedString(), mediaAddedCount)}</text>
                                                  </binding>
                                              </visual>
                                              <actions>
                                                  <action content='{"view_toast_button".GetLocalizedString()}' arguments='action=view'/>
                                                  <action activationType="system" arguments="dismiss" content="{"dismiss_toast_button".GetLocalizedString()}"/>
                                              </actions>
                                          </toast>
                                          """;

                        toast = new AppNotification(xmlPayload)
                        {
                            ExpiresOnReboot = true
                        };

                        notificationManager.Show(toast);
                    }
                }

                if (!App.GetService<SettingsService>().LeaveAppRunning && noMainWindow)
                {
                    App.Shutdown();
                }
                else
                {
                    await CreateWindow();
                    await LoadWindow();
                }
            }
        }
        else
        {
            var noMainWindow = App.MainWindow == null;
            if (noMainWindow)
            {
                await CreateWindow();
            }

            if (!parameters.ContainsKey("NoWindow"))
            {
                App.MainWindow?.Show();
            }

            if (noMainWindow)
            {
                await LoadServices();
                await LoadWindow();
            }
        }
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

            args = args.Contains("--") ? args[(args.IndexOf("--", StringComparison.Ordinal))..] : "";
        }

        return parameters;
    }
}
