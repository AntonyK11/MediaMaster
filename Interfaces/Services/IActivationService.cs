using Microsoft.Windows.AppLifecycle;

namespace MediaMaster.Interfaces.Services;

public interface IActivationService
{
    Task ActivateAsync();

    Task LaunchWindow(bool show);

    Task LoadWindow();

    void AddFiles();

    Task<string?> HandleActivationAsync(AppActivationArguments? activationArgs = null);

    Task LaunchApp(string args);
}
