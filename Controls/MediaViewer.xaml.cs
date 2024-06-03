using MediaMaster.DataBase.Models;
using MediaMaster.Services;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;

// To learn more about WinUI, the WinUI project structure,
// and more about our project templates, see: http://aka.ms/winui-project-info.

namespace MediaMaster.Controls;

public sealed partial class MediaViewer : UserControl
{

    public static readonly DependencyProperty? MediaProperty
        = DependencyProperty.Register(
            nameof(Media),
            typeof(Media),
            typeof(MediaViewer),
            new PropertyMetadata(null));

    public Media? Media
    {
        get => (Media)GetValue(MediaProperty);
        set
        {
            if (value != Media)
            {
                SetValue(MediaProperty, value);

                Visibility = value != null ? Visibility.Visible : Visibility.Collapsed;

                MediaIcon.MediaPath = value?.FilePath;
                MediaExtensionIcon.Source = null;
                SetMediaExtensionIcon();
            }
        }
    }

    public MediaViewer()
    {
        InitializeComponent();

        //Visibility = Visibility.Collapsed;
    }

    private MyCancellationTokenSource? _tokenSource;

    public void SetMediaExtensionIcon()
    {
        if (Media != null)
        {
            if (_tokenSource is { IsDisposed: false })
            {
                _tokenSource?.Cancel();
            }

            _tokenSource = IconService.AddImage1(Media.FilePath, ImageMode.IconOnly, 24, 24, MediaExtensionIcon);
        }
    }
}

