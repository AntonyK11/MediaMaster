using CommunityToolkit.WinUI;
using MediaMaster.Interfaces.Services;
using MediaMaster.Services.Navigation;
using MediaMaster.Views;
using Microsoft.Windows.AppNotifications;

namespace MediaMaster.Services;

public sealed class AppNotificationService(IActivationService activationService) : IAppNotificationService
{
    public void Initialize()
    {
        AppNotificationManager.Default.NotificationInvoked += OnNotificationInvoked;
        // Must be called before any GetActivatedEventArgs calls or else the app will crash.
        AppNotificationManager.Default.Register();
    }

    public async Task HandleNotificationAsync(AppNotificationActivatedEventArgs args)
    {
        if (App.MainWindow is null)
        {
            await activationService.CreateWindow();
            App.MainWindow?.Show();
            await activationService.LoadServices();
            await activationService.LoadWindow();
        }
        else
        {
            App.MainWindow.Show();
        }

        App.GetService<MainNavigationService>().NavigateTo(typeof(HomePage).FullName);
    }

    ~AppNotificationService()
    {
        Unregister();
    }

    private async void OnNotificationInvoked(AppNotificationManager? sender, AppNotificationActivatedEventArgs args)
    {
        await App.DispatcherQueue.EnqueueAsync(async () => await HandleNotificationAsync(args));
    }

    private static void Unregister()
    {
        AppNotificationManager.Default.Unregister();
    }
}