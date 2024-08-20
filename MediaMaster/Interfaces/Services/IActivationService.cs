using Microsoft.Windows.AppLifecycle;

namespace MediaMaster.Interfaces.Services;

public interface IActivationService
{
    Task ActivateAsync();

    Task CreateWindow();

    Task LoadServices();

    Task LoadWindow();

    Task<string?> HandleActivationAsync(AppActivationArguments? activationArgs = null);

    Task LaunchApp(string args);
}
