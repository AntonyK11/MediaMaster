using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.WinUI;
using MediaMaster.WIn32;
using WinUIEx;
using static MediaMaster.WIn32.WindowsNativeInterfaces;

namespace MediaMaster.Services;

internal sealed partial class TasksService : ObservableObject
{
    private static readonly ITaskbarList3 TaskbarInstance = (ITaskbarList3)new TaskbarInstance();
    [ObservableProperty] private Visibility _flyoutProgressBarLoading = Visibility.Collapsed;
    [ObservableProperty] private int _flyoutTasksNumber;
    [ObservableProperty] private Visibility _mainProgressBarLoading = Visibility.Collapsed;
    [ObservableProperty] private int _mainTasksNumber;

    public void AddMainTask()
    {
        App.DispatcherQueue.EnqueueAsync(() => { MainTasksNumber += 1; });
    }

    public void AddFlyoutTask()
    {
        App.DispatcherQueue.EnqueueAsync(() => { FlyoutTasksNumber += 1; });
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
        App.DispatcherQueue.EnqueueAsync(() => { MainTasksNumber -= 1; });
    }

    public void RemoveFlyoutTask()
    {
        App.DispatcherQueue.EnqueueAsync(() => { FlyoutTasksNumber -= 1; });
    }

    public void RemoveGlobalTak()
    {
        App.DispatcherQueue.EnqueueAsync(() =>
        {
            MainTasksNumber -= 1;
            FlyoutTasksNumber -= 1;
        });
    }

    partial void OnMainTasksNumberChanged(int oldValue, int newValue)
    {
        if (oldValue != newValue)
        {
            App.DispatcherQueue.EnqueueAsync(() =>
            {
                MainProgressBarLoading = newValue > 0 ? Visibility.Visible : Visibility.Collapsed;
            });
        }
    }

    partial void OnFlyoutTasksNumberChanged(int oldValue, int newValue)
    {
        if (oldValue != newValue)
        {
            App.DispatcherQueue.EnqueueAsync(() =>
            {
                if (newValue > 0)
                {
                    FlyoutProgressBarLoading = Visibility.Visible;
                    SetProgressState(WindowsNativeValues.TaskBarProgressState.Indeterminate);
                }
                else
                {
                    FlyoutProgressBarLoading = Visibility.Collapsed;
                    SetProgressState(WindowsNativeValues.TaskBarProgressState.NoProgress);
                }
            });
        }
    }

    private static void SetProgressState(WindowsNativeValues.TaskBarProgressState taskBarProgress)
    {
        if (App.MainWindow != null)
        {
            TaskbarInstance.SetProgressState(App.MainWindow.GetWindowHandle(), taskBarProgress);
        }
    }
}