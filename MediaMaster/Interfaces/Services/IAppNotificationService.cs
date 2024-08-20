using Microsoft.Windows.AppNotifications;

namespace MediaMaster.Interfaces.Services;

public interface IAppNotificationService
{
    void Initialize();

    Task HandleNotificationAsync(AppNotificationActivatedEventArgs args);
}
