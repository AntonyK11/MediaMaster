using Microsoft.UI.Xaml;
using MediaMaster.Services;
using Microsoft.UI.Input;

namespace MediaMaster.Controls;

public sealed partial class MediaIcon
{
    public static readonly DependencyProperty? UrisProperty
        = DependencyProperty.Register(
            nameof(Uris),
            typeof(ICollection<string>),
            typeof(MediaIcon),
            new PropertyMetadata(null));

    private MyCancellationTokenSource? _tokenSource;
    private TaskCompletionSource<int> _task = new();

    public ICollection<string> Uris
    {
        get
        {
            var uris = (ICollection<string>?)GetValue(UrisProperty);
            if (uris == null)
            {
                uris = [];
                SetValue(UrisProperty, uris);
            }

            return uris;
        }
        set
        {
            SetValue(UrisProperty, value);
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

    public static readonly DependencyProperty ContextMenuProperty
        = DependencyProperty.Register(
            nameof(ContextMenu),
            typeof(bool),
            typeof(MediaViewer),
            new PropertyMetadata(true));

    public bool ContextMenu
    {
        get => (bool)GetValue(ContextMenuProperty);
        set => SetValue(ContextMenuProperty, value);
    }

    public static readonly DependencyProperty CanOpenProperty
        = DependencyProperty.Register(
            nameof(CanOpen),
            typeof(bool),
            typeof(MediaViewer),
            new PropertyMetadata(true));

    public bool CanOpen
    {
        get => (bool)GetValue(CanOpenProperty);
        set
        {
            SetValue(CanOpenProperty, value);
            if (value)
            {
                ProtectedCursor = InputSystemCursor.Create(InputSystemCursorShape.Hand);
            }
            else
            {
                ProtectedCursor = InputSystemCursor.Create(InputSystemCursorShape.Arrow);
            }
        }
    }

    public static readonly DependencyProperty DelayLoadingProperty
        = DependencyProperty.Register(
            nameof(DelayLoading),
            typeof(bool),
            typeof(MediaViewer),
            new PropertyMetadata(true));

    public bool DelayLoading
    {
        get => (bool)GetValue(DelayLoadingProperty);
        set => SetValue(DelayLoadingProperty, value);
    }

    public static readonly DependencyProperty IconMarginProperty
        = DependencyProperty.Register(
            nameof(IconMargin),
            typeof(Thickness),
            typeof(MediaViewer),
            new PropertyMetadata(new Thickness(0, 0, 0, 0)));

    public Thickness IconMargin
    {
        get => (Thickness)GetValue(IconMarginProperty);
        set => SetValue(IconMarginProperty, value);
    }

    public static readonly DependencyProperty IconHeightProperty
        = DependencyProperty.Register(
            nameof(IconHeight),
            typeof(double),
            typeof(MediaViewer),
            new PropertyMetadata(double.NaN));

    public double IconHeight
    {
        get => (double)GetValue(IconHeightProperty);
        set => SetValue(IconHeightProperty, value);
    }

    public Microsoft.UI.Xaml.Controls.Image IconImage => Image;

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
                Arguments = $"/select, \"{Uris}\"",
                FileName = "explorer.exe"
            };

            Process.Start(startInfo);
        }
    }

    private async void SetIconAsync()
    {
        if (Uris.Count != 1)
        {
            Image.Source = IconService.DefaultIcon;
            return;
        }

        var uri = Uris.First();

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

        if (uri == Uris.First())
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

        _tokenSource = IconService.AddImage(Uris.First(), ImageMode, (int)ActualWidth, (int)ActualHeight, Image);
    }
}