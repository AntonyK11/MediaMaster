using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.WinUI;
using MediaMaster.WIn32;
using Windows.Foundation;
using WinUIEx;
using static MediaMaster.WIn32.WindowsNativeInterfaces;

namespace MediaMaster.Services;

internal sealed partial class TasksService : ObservableObject
{
    private static readonly ITaskbarList3 TaskbarInstance = (ITaskbarList3)new TaskbarInstance();
    [ObservableProperty] public partial Visibility FlyoutProgressBarLoading { get; set; } = Visibility.Collapsed;
    [ObservableProperty] public partial int FlyoutTasksNumber { get; set; }
    [ObservableProperty] public partial Visibility MainProgressBarLoading { get; set; } = Visibility.Collapsed;
    [ObservableProperty] public partial int MainTasksNumber { get; set; }

    public event TypedEventHandler<object, object?>? MainTaskAdded;
    public event TypedEventHandler<object, object?>? FlyoutTaskAdded;
    public event TypedEventHandler<object, object?>? NoMoreTasksRunning;

    public async Task AddMainTask()
    {
        await App.DispatcherQueue.EnqueueAsync(() =>
        {
            MainTasksNumber += 1;
            MainTaskAdded?.Invoke(this, null);
        });
    }

    public async Task AddFlyoutTask()
    {
        await App.DispatcherQueue.EnqueueAsync(() =>
        {
            FlyoutTasksNumber += 1;
            FlyoutTaskAdded?.Invoke(this, null);
        });
    }

    public async Task AddGlobalTak()
    {
        await App.DispatcherQueue.EnqueueAsync(() =>
        {
            MainTasksNumber += 1;
            FlyoutTasksNumber += 1;

            MainTaskAdded?.Invoke(this, null);
            FlyoutTaskAdded?.Invoke(this, null);
        });
    }

    public async Task RemoveMainTask()
    {
        await App.DispatcherQueue.EnqueueAsync(() =>
        {
            MainTasksNumber -= 1;

            if (!IsTaskRunning())
            {
                NoMoreTasksRunning?.Invoke(this, null);
            }
        });
    }

    public async Task RemoveFlyoutTask()
    {
        await App.DispatcherQueue.EnqueueAsync(() =>
        {
            FlyoutTasksNumber -= 1;

            if (!IsTaskRunning())
            {
                NoMoreTasksRunning?.Invoke(this, null);
            }
        });
    }

    public async Task RemoveGlobalTak()
    {
        await App.DispatcherQueue.EnqueueAsync(() =>
        {
            MainTasksNumber -= 1;
            FlyoutTasksNumber -= 1;

            if (!IsTaskRunning())
            {
                NoMoreTasksRunning?.Invoke(this, null);
            }
        });
    }

    public bool IsTaskRunning()
    {
        return MainTasksNumber != 0 || FlyoutTasksNumber != 0;
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