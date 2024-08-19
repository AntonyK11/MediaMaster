using MediaMaster.Interfaces.Services;
using MediaMaster.Views;
using WinUIEx;
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

        if (args is not null) await LaunchApp(args);

        await LoadWindow();

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

    public async Task LaunchWindow(bool show)
    {
        await App.GetService<IThemeSelectorService>().InitializeAsync();
        App.MainWindow ??= new MainWindow();

        if (!show) return;

        App.MainWindow.Show();
    }

    public async Task LoadWindow()
    {
        if (App.MainWindow is null) return;

        await LoadServices();
        App.MainWindow.Content = App.GetService<ShellPage>();
        App.MainWindow.InitializeTheme();
        App.GetService<MainNavigationService>().NavigateTo(typeof(HomePage).FullName!);

        await App.GetService<BrowserService>().InitializeAsync();
    }

    private async Task LoadServices()
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
            foreach (var file in Directory.EnumerateFiles(menuFolder.Path)) File.Delete(file);

            await _menuService.SaveAsync(menu);
            await _menuService.BuildToCacheAsync();
            _menuService.SetCustomMenuName($"{"add_context_menu".GetLocalizedString()} (Win 11)");
        }
        _contextMenuLock = true;
    }

    public void AddFiles()
    {

    }

    public async Task LaunchApp(string args)
    {
        Dictionary<string, string?> parameters = GetParams(args);

        if (parameters.TryGetValue("Files", out var filesValue))
        {
            if (filesValue is null) return;

            if (App.MainWindow is not null && (App.MainWindow.GetWindowStyle() & WindowStyle.Visible) != 0)
            {
                await LaunchWindow(true);
                AddFiles();

                DateTime time = DateTime.Now;
                Debug.WriteLine("Adding files");
                var files = filesValue.Split(", ").Select(f => f.Replace("\"", "").Trim());
                await Task.Run(() => MediaService.AddMediaAsync(files).ConfigureAwait(false));
                Debug.WriteLine(filesValue);
                Debug.WriteLine(DateTime.Now - time);
            }
            else
            {
                if (App.MainWindow is null)
                {
                    await using (MediaDbContext database = new())
                    {
                        await database.InitializeAsync();
                    }
                    await Task.WhenAll(
                        App.GetService<ITranslationService>().InitializeAsync(),
                        App.GetService<SettingsService>().InitializeAsync()
                    );
                }

                var files = filesValue.Split(", ");
                foreach (var file in files)
                {
                    //await App.GetService<MediaService>().AddMediaAsync(file.Replace("\"", ""));
                    Debug.WriteLine(file.Replace("\"", ""));
                }
                //await App.GetService<DataBaseService>().SaveChangesAsync();

                var xmlPayload = $"""
                                  <toast launch="action=edit">
                                    <visual>
                                      <binding template="ToastGeneric">
                                        <text>{"media_added_toast_text".GetLocalizedString()}</text>
                                      </binding>
                                    </visual>
                                    <actions>
                                      <action content='{"edit_toast_button".GetLocalizedString()}' arguments='action=edit'/>
                                      <action activationType="system" arguments="dismiss"  content="{"dismiss_toast_button".GetLocalizedString()}"/>
                                    </actions>
                                  </toast>
                                  """;

                var toast = new AppNotification(xmlPayload)
                {
                    ExpiresOnReboot = true
                };

                var notificationManager = AppNotificationManager.Default;
                notificationManager.Show(toast);

                if (!App.GetService<SettingsService>().LeaveAppRunning)
                {
                    App.Shutdown();
                }
            }
        }
        else
        {
            await LaunchWindow(!parameters.ContainsKey("NoWindow"));
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
