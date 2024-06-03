using Windows.Storage;
using Microsoft.UI.Xaml;
using Windows.System;
using System.Diagnostics;
using MediaMaster.Services;

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

    public string? MediaPath
    {
        get => (string?)GetValue(MediaPathProperty);
        set
        {
            SetValue(MediaPathProperty, value);
            if (_tokenSource is { IsDisposed: false })
            {
                _tokenSource?.Cancel();
            }

            _tokenSource = IconService.AddImage1(MediaPath, ImageMode, (int)MaxHeight, (int)MaxHeight, Image);
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
            if (_tokenSource is { IsDisposed: false })
            {
                _tokenSource?.Cancel();
            }

            _tokenSource = IconService.AddImage1(MediaPath, ImageMode, (int)MaxHeight, (int)MaxHeight, Image);
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
    }

    public async void OpenMedia()
    {
        if (MediaPath != null && File.Exists(MediaPath))
        {
            var file = await StorageFile.GetFileFromPathAsync(MediaPath);
            await Launcher.LaunchFileAsync(file);
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
}