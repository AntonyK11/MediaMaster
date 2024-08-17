using CommunityToolkit.WinUI;
using DependencyPropertyGenerator;
using MediaMaster.Extensions;
using MediaMaster.Services;
using Microsoft.UI.Input;

namespace MediaMaster.Controls;

[DependencyProperty("Uris", typeof(ICollection<string>), DefaultValueExpression = "new List<string>()")]
[DependencyProperty("ImageMode", typeof(ImageMode), DefaultValue = ImageMode.IconAndThumbnail)]
[DependencyProperty("LoadIcon", typeof(bool), DefaultValue = true)]
[DependencyProperty("ContextMenu", typeof(bool), DefaultValue = true)]
[DependencyProperty("CanOpen", typeof(bool), DefaultValue = true)]
[DependencyProperty("DelayLoading", typeof(bool), DefaultValue = true)]
[DependencyProperty("IconMargin", typeof(Thickness), DefaultValueExpression = "new Thickness(0)")]
[DependencyProperty("IconHeight", typeof(double), DefaultValue = double.NaN)]
public sealed partial class MediaIcon
{
    private TaskCompletionSource? _taskSource;
    private TaskCompletionSource _task = new();

    partial void OnUrisChanged(ICollection<string> newValue)
    {
        _task.SetResult();
        _task = new TaskCompletionSource();

        if (_taskSource is { Task.IsCompleted: false })
        {
            _taskSource.SetResult();
        }
        _taskSource = new TaskCompletionSource();
        SetIconAsync(_taskSource);

        OpenFileFlyout.Visibility = Visibility.Collapsed;
        OpenFolderFlyout.Visibility = Visibility.Collapsed;
        OpenWebPageFlyout.Visibility = Visibility.Collapsed;

        if (newValue.Count != 0)
        {
            if (ContextMenu)
            {
                VisualStateManager.GoToState(this, "ShowFlyout", true);
            }

            if (newValue.First().IsWebsite())
            {
                OpenWebPageFlyout.Visibility = Visibility.Visible;
            }
            else
            {
                OpenFileFlyout.Visibility = Visibility.Visible;
                OpenFolderFlyout.Visibility = Visibility.Visible;
            }
        }
        else
        {
            VisualStateManager.GoToState(this, "HideFlyout", true);
        }
    }

    partial void OnImageModeChanged()
    {
        _task.SetResult();
        _task = new TaskCompletionSource();

        if (_taskSource is { Task.IsCompleted: false })
        {
            _taskSource.SetResult();
        }
        _taskSource = new TaskCompletionSource();
        SetIconAsync(_taskSource);
    }

    partial void OnContextMenuChanged(bool newValue)
    {
        if (newValue)
        {
            VisualStateManager.GoToState(this, "ShowFlyout", true);
        }
        else
        {
            VisualStateManager.GoToState(this, "HideFlyout", true);
        }
    }

    partial void OnCanOpenChanged(bool newValue)
    {
        ProtectedCursor = InputSystemCursor.Create(newValue ? InputSystemCursorShape.Hand : InputSystemCursorShape.Arrow);
    }

    public Image IconImage => Image;

    public MediaIcon()
    {
        InitializeComponent();
        DoubleTapped += (_, _) => Open();

        if (CanOpen)
        {
            ProtectedCursor = InputSystemCursor.Create(InputSystemCursorShape.Hand);
        }

        Loaded += (_, _) =>
        {
            if (ContextMenu)
            {
                OpenFileFlyout.Click += (_, _) => Open();
                OpenFolderFlyout.Click += (_, _) => OpenFolder();
                OpenWebPageFlyout.Click += (_, _) => Open();
            }
        };
    }

    public void Open()
    {
        if (Uris.Count == 1)
        {
            try
            {
                ProcessStartInfo startInfo = new(Uris.First())
                {
                    UseShellExecute = true
                };

                Process.Start(startInfo);
            }
            catch
            {
                // ignored
            }
        }
    }

    public void OpenFolder()
    {
        if (Uris.Count == 1 && File.Exists(Uris.First()))
        {
            ProcessStartInfo startInfo = new()
            {
                Arguments = $"/select, \"{Uris.First()}\"",
                FileName = "explorer.exe"
            };

            Process.Start(startInfo);
        }
    }

    private async void SetIconAsync(TaskCompletionSource tcs)
    {
        if (Uris.Count != 1)
        {
            Image.Source = IconService.GetDefaultIcon(null);
            return;
        }

        var uri = Uris.First();

        var icon = await IconService.GetIconAsync(uri, ImageMode | ImageMode.CacheOnly, (int)ActualWidth, (int)ActualHeight);

        if (tcs.Task.IsCompleted) return;

        if (icon != null)
        {
            await App.DispatcherQueue.EnqueueAsync(() => Image.Source = icon);
            return;
        }

        if (Image.Source != null)
        {
            Image.Source = null;
        }

        if (DelayLoading)
        {
            await Task.WhenAny(
                Task.Delay(500),
                _task.Task);
        }

        if (uri == Uris.First() && !tcs.Task.IsCompleted)
        {
            IconService.SetIcon(Uris.First(), ImageMode, (int)ActualWidth, (int)ActualHeight, Image, tcs);
        }
    }
}