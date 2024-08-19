using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.WinUI;

namespace MediaMaster.Services;

internal partial class TasksService : ObservableObject
{
    [ObservableProperty] private Visibility _mainProgressBarLoading = Visibility.Collapsed;
    [ObservableProperty] private Visibility _flyoutProgressBarLoading = Visibility.Collapsed;
    [ObservableProperty] private int _mainTasksNumber;
    [ObservableProperty] private int _flyoutTasksNumber;

    public void AddMainTask()
    {
        App.DispatcherQueue.EnqueueAsync(() =>
        {
        MainTasksNumber += 1;
        });
    }

    public void AddFlyoutTask()
    {
        App.DispatcherQueue.EnqueueAsync(() =>
        {
            FlyoutTasksNumber += 1;
        });

    }

    public void AddGlobalTak()
    {
        App.DispatcherQueue.EnqueueAsync(() =>
        {
            MainTasksNumber += 1;
            FlyoutTasksNumber += 1;
        });

    }

    public void RemoveMainTask()
    {
        App.DispatcherQueue.EnqueueAsync(() =>
        {
            MainTasksNumber -= 1;
        });

    }

    public void RemoveFlyoutTask()
    {
        App.DispatcherQueue.EnqueueAsync(() =>
        {
            FlyoutTasksNumber -= 1;
        });
    }

    public void RemoveGlobalTak()
    {
        App.DispatcherQueue.EnqueueAsync(() =>
        {
            MainTasksNumber -= 1;
            FlyoutTasksNumber -= 1;
        });
    }

    partial void OnMainTasksNumberChanged(int value)
    {
        App.DispatcherQueue.EnqueueAsync(() => MainProgressBarLoading = value > 0 ? Visibility.Visible : Visibility.Collapsed);
    }

    partial void OnFlyoutTasksNumberChanged(int value)
    {
        App.DispatcherQueue.EnqueueAsync(() => FlyoutProgressBarLoading = value > 0 ? Visibility.Visible : Visibility.Collapsed);
    }
}

