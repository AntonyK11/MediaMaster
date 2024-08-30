using System.Runtime.InteropServices;
using Microsoft.UI.Dispatching;
using WinRT;
using Microsoft.Windows.AppLifecycle;
using CommunityToolkit.WinUI;
using MediaMaster.Interfaces.Services;

namespace MediaMaster;

internal static partial class Program
{
    // Code from App.g.i.cs
    // Entry point for the application.
    [LibraryImport("Microsoft.ui.xaml.dll")]
    private static partial void XamlCheckProcessRequirements();

    // TODO: Made the Main method synchronous to allow drag and drop to work
    // https://github.com/microsoft/microsoft-ui-xaml/issues/9061#issuecomment-1925433956

    [STAThread]
    public static int Main(string[] args)
    {
        ComWrappersSupport.InitializeComWrappers();

        XamlCheckProcessRequirements();

        var isRedirect = DecideRedirection();
        if (!isRedirect)
        {
            Application.Start(_ =>
            {
                DispatcherQueueSynchronizationContext context = new(DispatcherQueue.GetForCurrentThread());
                SynchronizationContext.SetSynchronizationContext(context);
                new App();
            });
        }

        return 0;
    }

    private static bool DecideRedirection()
    {
        var isRedirect = false;

        AppInstance? keyInstance = AppInstance.FindOrRegisterForKey("MediaMaster");

        if (keyInstance.IsCurrent)
        {
            keyInstance.Activated += OnActivated;
        }
        else
        {
            isRedirect = true;
            AppActivationArguments? args = AppInstance.GetCurrent().GetActivatedEventArgs();
            keyInstance.RedirectActivationToAsync(args).GetAwaiter().GetResult();
        }

        return isRedirect;
    }

    private static async void OnActivated(object? sender, AppActivationArguments args)
    {
        await App.DispatcherQueue.EnqueueAsync(async () =>
        {
            var activationArgs = await App.GetService<IActivationService>().HandleActivationAsync(args);

            if (activationArgs is not null)
            {
                await App.GetService<IActivationService>().LaunchApp(activationArgs);
            };
        }); 
    }
}