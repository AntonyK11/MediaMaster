using MediaMaster.DataBase;
using MediaMaster.DataBase.Models;
using MediaMaster.Services;
using MediaMaster.Services.MediaInfo;
using Microsoft.UI.Input;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;

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
            //Visibility = value != null ? Visibility.Visible : Visibility.Collapsed;

            if (value == null) return;

            Media? updatedMedia;
            if (ForceUpdate)
            {
                using (var database = new MediaDbContext())
                {
                    updatedMedia = database.Find<Media>(value.MediaId);
                }
                if (updatedMedia == null) return;
            }
            else
            {
                updatedMedia = value;
            }
            SetValue(MediaProperty, updatedMedia);

            _mediaInfoService.SetMedia(updatedMedia, IsCompact);


        }
    }

    public static readonly DependencyProperty IconHeightProperty
        = DependencyProperty.Register(
            nameof(IconHeight),
            typeof(int),
            typeof(MediaViewer),
            new PropertyMetadata(300));

    public int IconHeight
    {
        get => (int)GetValue(IconHeightProperty);
        set => SetValue(IconHeightProperty, value);
    }

    public static readonly DependencyProperty IsCompactProperty
        = DependencyProperty.Register(
            nameof(IsCompact),
            typeof(bool),
            typeof(MediaViewer),
            new PropertyMetadata(false));

    public bool IsCompact
    {
        get => (bool)GetValue(IsCompactProperty);
        set
        {
            SetValue(IsCompactProperty, value);
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

    public static readonly DependencyProperty ForceUpdateProperty
        = DependencyProperty.Register(
            nameof(ForceUpdate),
            typeof(bool),
            typeof(MediaViewer),
            new PropertyMetadata(true));

    public bool ForceUpdate
    {
        get => (bool)GetValue(ForceUpdateProperty);
        set => SetValue(ForceUpdateProperty, value);
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
        set => SetValue(ImageModeProperty, value);
    }

    public static readonly DependencyProperty DelayIconLoadingProperty
        = DependencyProperty.Register(
            nameof(DelayIconLoading),
            typeof(bool),
            typeof(MediaViewer),
            new PropertyMetadata(true));

    public bool DelayIconLoading
    {
        get => (bool)GetValue(DelayIconLoadingProperty);
        set => SetValue(DelayIconLoadingProperty, value);
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

    private readonly MediaInfoService _mediaInfoService;
    private MyCancellationTokenSource? _tokenSource;


    public MediaViewer()
    {
        InitializeComponent();

        _mediaInfoService = new MediaInfoService(StackPanel);
        _mediaInfoService.SetMedia(null, IsCompact);
    }
}