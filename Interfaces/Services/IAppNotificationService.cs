using Microsoft.Windows.AppNotifications;
using System.Collections.Specialized;

namespace MediaMaster.Interfaces.Services;

public interface IAppNotificationService
{
    void Initialize();

    Task HandleNotificationAsync(AppNotificationActivatedEventArgs args);

    bool Show(string payload);

    NameValueCollection ParseArguments(string arguments);

    void Unregister();
}
