using System.Collections.Specialized;
using System.Web;
using CommunityToolkit.WinUI;
using MediaMaster.Interfaces.Services;
using Microsoft.Windows.AppNotifications;

namespace MediaMaster.Services;

public class AppNotificationService : IAppNotificationService
{
    //private readonly INavigationService _navigationService;
    private readonly IActivationService _activationService;

    //public AppNotificationActivationHandler(INavigationService navigationService, IAppNotificationService notificationService)
    public AppNotificationService(IActivationService activationService)
    {
        //_navigationService = navigationService;
        _activationService = activationService;
    }

    ~AppNotificationService()
    {
        Unregister();
    }

    public void Initialize()
    {
        AppNotificationManager.Default.NotificationInvoked += OnNotificationInvoked;
        // Must be called before any GetActivatedEventArgs calls or else the app will crash.
        AppNotificationManager.Default.Register();
    }

    public async void OnNotificationInvoked(AppNotificationManager? sender, AppNotificationActivatedEventArgs args)
    {
        await App.DispatcherQueue.EnqueueAsync(async () => await HandleNotificationAsync(args));
    }

    public async Task HandleNotificationAsync(AppNotificationActivatedEventArgs args)
    {
        foreach (var (key, value) in args.Arguments)
        {
            Debug.WriteLine($"{key}: {value}");
        }

        if (App.MainWindow is null)
        {
            await _activationService.LaunchWindow(true);
            await _activationService.LoadWindow();
        }
        else
        {
            await _activationService.LaunchWindow(true);
        }

        if (!args.Arguments.TryGetValue("action", out var action)) return;

        switch (ParseArguments(args.Argument)["action"])
        {
            case "edit":
                _activationService.AddFiles();
                break;
        }

        //App.MainWindow?.ShowMessageDialogAsync("TODO: Handle notification invocations when your app is already running.", "Notification Invoked");

        //App.MainWindow?.BringToFront();

        //_navigationService.NavigateTo(typeof(SettingsViewModel).FullName!);
    }

    public bool Show(string payload)
    {
        var appNotification = new AppNotification(payload);

        AppNotificationManager.Default.Show(appNotification);

        return appNotification.Id != 0;
    }

    public NameValueCollection ParseArguments(string arguments)
    {
        return HttpUtility.ParseQueryString(arguments);
    }

    public void Unregister()
    {
        AppNotificationManager.Default.Unregister();
    }
}
