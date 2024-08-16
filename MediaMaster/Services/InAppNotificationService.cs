using CommunityToolkit.WinUI;
using CommunityToolkit.WinUI.Behaviors;

namespace MediaMaster.Services;

public class InAppNotificationService
{
    private StackedNotificationsBehavior? _stackedNotificationsBehavior;

    public void RegisterStackedNotificationsBehavior(StackedNotificationsBehavior stackedNotificationsBehavior)
    {
        _stackedNotificationsBehavior = stackedNotificationsBehavior;
    }

    public void SendNotification(Notification notification)
    {
        App.DispatcherQueue.EnqueueAsync(() => _stackedNotificationsBehavior?.Show(notification));
    }
}

