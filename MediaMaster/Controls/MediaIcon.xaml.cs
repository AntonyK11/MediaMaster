using Microsoft.UI.Xaml;
using System.Diagnostics;
using Windows.UI.Core;
using MediaMaster.Services;
using Microsoft.UI.Input;

namespace MediaMaster.Controls;

public sealed partial class MediaIcon
{
    public static readonly DependencyProperty? MediaPathProperty
        = DependencyProperty.Register(
            nameof(MediaPath),
            typeof(string),
            typeof(MediaIcon),
            new PropertyMetadata(null));

    private MyCancellationTokenSource? _tokenSource;
    private TaskCompletionSource<int> _task = new();

    public string? MediaPath
    {
        get => (string?)GetValue(MediaPathProperty);
        set
        {
            SetValue(MediaPathProperty, value);
            _task.SetResult(0);
            _task = new TaskCompletionSource<int>();
            SetIconAsync();
        }
    }

    public static readonly DependencyProperty? ImageModeProperty
        = DependencyProperty.Register(
            nameof(ImageMode),
            typeof(ImageMode),
            typeof(MediaIcon),
            new PropertyMetadata(ImageMode.IconAndThumbnail));

    public ImageMode ImageMode
    {
        get => (ImageMode)GetValue(ImageModeProperty);
        set
        {
            SetValue(ImageModeProperty, value);
            _task.SetResult(0);
            _task = new TaskCompletionSource<int>();
            SetIconAsync();
        }
    }

    public static readonly DependencyProperty LoadIconProperty
        = DependencyProperty.Register(
            nameof(LoadIcon),
            typeof(bool),
            typeof(MediaViewer),
            new PropertyMetadata(true));

    public bool LoadIcon
    {
        get => (bool)GetValue(LoadIconProperty);
        set => SetValue(LoadIconProperty, value);
    }

    public MediaIcon()
    {
        InitializeComponent();
        OpenMediaFlyout.Click += (_, _) => OpenMedia();
        OpenFolderFlyout.Click += (_, _) => OpenFolder();
        DoubleTapped += (_, _) => OpenMedia();

        ProtectedCursor = InputCursor.CreateFromCoreCursor(new CoreCursor(CoreCursorType.Hand, 0));
    }

    public void OpenMedia()
    {
        if (MediaPath != null && File.Exists(MediaPath))
        {
            try
            {
                ProcessStartInfo startInfo = new(MediaPath)
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
        if (MediaPath != null && File.Exists(MediaPath))
        {
            ProcessStartInfo startInfo = new()
            {
                Arguments = Path.GetDirectoryName(MediaPath),
                FileName = "explorer.exe"
            };

            Process.Start(startInfo);
        }
    }

    private async void SetIconAsync()
    {
        var path = MediaPath;
        if (Image.Source != null)
        {
            Image.Source = null;
        }

        await Task.WhenAny(
            Task.Delay(500),
            _task.Task);

        if (path == MediaPath)
        {
            SetIcon();
        }
    }

    private void SetIcon()
    {
        if (_tokenSource is { IsDisposed: false })
        {
            _tokenSource?.Cancel();
        }

        _tokenSource = IconService.AddImage1(MediaPath, ImageMode, (int)ActualHeight, (int)ActualHeight, Image);
    }
}