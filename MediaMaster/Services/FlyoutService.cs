using H.NotifyIcon;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml;

namespace MediaMaster.Services;

public static class FlyoutService
{
    public static void Initialize()
    {
        App.Flyout = new FlyoutWindow();

        var launchFlyoutCommand = (XamlUICommand)((App)Application.Current).Resources["ToggleFlyoutCommand"];
        launchFlyoutCommand.ExecuteRequested += (_, _) =>
        {
            App.Flyout.Toggle_Flyout();
        };

        var exitApplicationCommand = (XamlUICommand)((App)Application.Current).Resources["ExitApplicationCommand"];
        exitApplicationCommand.ExecuteRequested += (_, _) =>
        {
            App.Shutdown();
        };

        var launchApplicationCommand = (XamlUICommand)((App)Application.Current).Resources["LaunchApplicationCommand"];
        launchApplicationCommand.ExecuteRequested += (_, _) =>
        {
            App.MainWindow?.Show();
        };

        App.TrayIcon = (TaskbarIcon)((App)Application.Current).Resources["TrayIcon"];
        App.TrayIcon.ForceCreate(enablesEfficiencyMode: false);
    }

    public static void Dispose()
    {
        if (App.TrayIcon?.IsDisposed == false)
        {
            App.TrayIcon.Dispose();
        }
    }
}
