using System.Numerics;
using EFCore.BulkExtensions;
using MediaMaster.DataBase;
using MediaMaster.DataBase.Models;
using MediaMaster.Services;
using MediaMaster.Services.MediaInfo;
using Microsoft.EntityFrameworkCore;
using Microsoft.UI.Composition;
using Microsoft.UI.Input;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Input;

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
                ScrollView.Visibility = Visibility.Collapsed;
                StackPanelCompact.Visibility = Visibility.Visible;
                ProtectedCursor = InputSystemCursor.Create(InputSystemCursorShape.Hand);
                _mediaInfoService = new MediaInfoService(StackPanelCompact);
                _mediaInfoService.SetMedia(null, value);
            }
            else
            {
                ScrollView.Visibility = Visibility.Visible;
                StackPanelCompact.Visibility = Visibility.Collapsed;
                ProtectedCursor = InputSystemCursor.Create(InputSystemCursorShape.Arrow);
                _mediaInfoService = new MediaInfoService(StackPanel);
                _mediaInfoService.SetMedia(null, value);
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

    private MediaInfoService _mediaInfoService;


    public MediaViewer()
    {
        InitializeComponent();

        _mediaInfoService = new MediaInfoService(StackPanel);
        _mediaInfoService.SetMedia(null, IsCompact);

        MediaDbContext.MediaChanged += (_, args) =>
        {
            if (Media == null || args.Media.MediaId != Media.MediaId || !args.Flags.HasFlag(MediaChangeFlags.TagsChanged)) return;
            SetValue(MediaProperty, args.Media);
        };
    }

    private SpringVector3NaturalMotionAnimation? _translationAnimation;
    private SpringScalarNaturalMotionAnimation? _rotationAnimation;
    private Vector3KeyFrameAnimation? _scaleAnimation;

    private void CreateOrUpdateTranslationAnimation(float? initialValue, float finalValue)
    {
        if (_translationAnimation == null)
        {
            _translationAnimation = App.MainWindow!.Compositor.CreateSpringVector3Animation();
            _translationAnimation.Target = "Translation";
            _translationAnimation.Period = TimeSpan.FromMilliseconds(32);
            _translationAnimation.StopBehavior = AnimationStopBehavior.SetToFinalValue;
        }

        _translationAnimation.InitialValue = initialValue == null ? null : new Vector3(0, (float)initialValue, 0);
        _translationAnimation.FinalValue = new Vector3(0, finalValue, 0);
    }

    private void CreateOrUpdateRotationAnimation(float? initialValue, float finalValue)
    {
        if (_rotationAnimation == null)
        {
            _rotationAnimation = App.MainWindow!.Compositor.CreateSpringScalarAnimation();
            _rotationAnimation.Target = "Rotation";
            _rotationAnimation.Period = TimeSpan.FromMilliseconds(64);
            _rotationAnimation.StopBehavior = AnimationStopBehavior.SetToFinalValue;
        }

        _rotationAnimation.InitialValue = initialValue;
        _rotationAnimation.FinalValue = finalValue;
    }

    private void CreateOrUpdateScaleAnimation(float initialValue, float middleValue, float finalValue)
    {
        if (_scaleAnimation == null)
        {
            _scaleAnimation = App.MainWindow!.Compositor.CreateVector3KeyFrameAnimation();
            _scaleAnimation.Target = "Scale";
            _scaleAnimation.Duration = TimeSpan.FromMilliseconds(256);
            _scaleAnimation.StopBehavior = AnimationStopBehavior.SetToFinalValue;
        }

        _scaleAnimation.InsertKeyFrame(0, new Vector3(initialValue));
        _scaleAnimation.InsertKeyFrame(0.5f, new Vector3(middleValue));
        _scaleAnimation.InsertKeyFrame(1, new Vector3(finalValue));
    }

    private void FavoriteToggleButton_OnPointerPressed(object sender, PointerRoutedEventArgs e)
    {
        CreateOrUpdateTranslationAnimation(0, 2);
        FavoriteIconGrid.StartAnimation(_translationAnimation);
    }

    private async void FavoriteToggleButton_OnChecked(object sender, RoutedEventArgs e)
    {
        CreateOrUpdateTranslationAnimation(2, 0);
        FavoriteIconGrid.StartAnimation(_translationAnimation);

        CreateOrUpdateRotationAnimation(-360, 0);
        FavoriteIconGrid.StartAnimation(_rotationAnimation);

        if (Media != null && MediaDbContext.FavoriteTag != null && !Media.IsFavorite)
        {
            Media.IsFavorite = true;
            Media.Modified = DateTime.UtcNow;

            await using (var database = new MediaDbContext())
            {
                await database.BulkUpdateAsync([Media]);

                var mediaTag = new MediaTag()
                {
                    MediaId = Media.MediaId,
                    TagId = MediaDbContext.FavoriteTag.TagId
                };
                await database.BulkInsertAsync([mediaTag]);
            }

            MediaDbContext.InvokeMediaChange(MediaChangeFlags.MediaChanged | MediaChangeFlags.TagsChanged, Media, tagsAdded: [MediaDbContext.FavoriteTag]);
        }
    }

    private void FavoriteToggleButton_OnPointerExited(object sender, PointerRoutedEventArgs e)
    {
        CreateOrUpdateTranslationAnimation(null, 0);
        FavoriteIconGrid.StartAnimation(_translationAnimation);
    }

    private async void FavoriteToggleButton_OnUnchecked(object sender, RoutedEventArgs e)
    {
        CreateOrUpdateTranslationAnimation(2, 0);
        FavoriteIconGrid.StartAnimation(_translationAnimation);

        if (Media != null && MediaDbContext.FavoriteTag != null && Media.IsFavorite)
        {
            Media.IsFavorite = false;
            Media.Modified = DateTime.UtcNow;

            await using (var database = new MediaDbContext())
            {
                await database.BulkUpdateAsync([Media]);

                var mediaTag = await database.MediaTags.FirstOrDefaultAsync(m => m.MediaId == Media.MediaId && m.TagId == MediaDbContext.FavoriteTag.TagId);
                if (mediaTag != null)
                {
                    await database.BulkDeleteAsync([mediaTag]);
                }
            }

            MediaDbContext.InvokeMediaChange(MediaChangeFlags.MediaChanged | MediaChangeFlags.TagsChanged, Media, tagsRemoved: [MediaDbContext.FavoriteTag]);
        }
    }

    private void FavoriteToggleButton_OnPointerEntered(object sender, PointerRoutedEventArgs e)
    {
        if (!e.GetCurrentPoint(FavoriteToggleButton).Properties.IsLeftButtonPressed) return;
        CreateOrUpdateTranslationAnimation(0, 2);
        FavoriteIconGrid.StartAnimation(_translationAnimation);
    }

    private void ArchiveToggleButton_OnPointerEntered(object sender, PointerRoutedEventArgs e)
    {
        if (!e.GetCurrentPoint(FavoriteToggleButton).Properties.IsLeftButtonPressed) return;
        CreateOrUpdateTranslationAnimation(0, 2);
        ArchiveIconGrid.StartAnimation(_translationAnimation);
    }

    private void ArchiveToggleButton_OnPointerExited(object sender, PointerRoutedEventArgs e)
    {
        CreateOrUpdateTranslationAnimation(null, 0);
        ArchiveIconGrid.StartAnimation(_translationAnimation);
    }

    private async void ArchiveToggleButton_OnUnchecked(object sender, RoutedEventArgs e)
    {
        CreateOrUpdateTranslationAnimation(2, 0);
        ArchiveIconGrid.StartAnimation(_translationAnimation);

        if (Media != null && MediaDbContext.ArchivedTag != null && Media.IsArchived)
        {
            Media.IsArchived = false;
            Media.Modified = DateTime.UtcNow;

            await using (var database = new MediaDbContext())
            {
                await database.BulkUpdateAsync([Media]);

                var mediaTag = await database.MediaTags.FirstOrDefaultAsync(m => m.MediaId == Media.MediaId && m.TagId == MediaDbContext.ArchivedTag.TagId);
                if (mediaTag != null)
                {
                    await database.BulkDeleteAsync([mediaTag]);
                }
            }

            MediaDbContext.InvokeMediaChange(MediaChangeFlags.MediaChanged | MediaChangeFlags.TagsChanged, Media, tagsRemoved: [MediaDbContext.ArchivedTag]);
        }
    }

    private async void ArchiveToggleButton_OnChecked(object sender, RoutedEventArgs e)
    {
        CreateOrUpdateTranslationAnimation(2, 0);
        ArchiveIconGrid.StartAnimation(_translationAnimation);

        CreateOrUpdateScaleAnimation(1, 1.2f, 1);
        ArchiveIconGrid.StartAnimation(_scaleAnimation);

        if (Media != null && MediaDbContext.ArchivedTag != null && !Media.IsArchived)
        {
            Media.IsArchived = true;
            Media.Modified = DateTime.UtcNow;

            await using (var database = new MediaDbContext())
            {
                await database.BulkUpdateAsync([Media]);

                var mediaTag = new MediaTag()
                {
                    MediaId = Media.MediaId,
                    TagId = MediaDbContext.ArchivedTag.TagId
                };
                await database.BulkInsertAsync([mediaTag]);
            }

            MediaDbContext.InvokeMediaChange(MediaChangeFlags.MediaChanged | MediaChangeFlags.TagsChanged, Media, tagsAdded: [MediaDbContext.ArchivedTag]);
        }
    }

    private void ArchiveToggleButton_OnPointerPressed(object sender, PointerRoutedEventArgs e)
    {
        CreateOrUpdateTranslationAnimation(0, 2);
        ArchiveIconGrid.StartAnimation(_translationAnimation);
    }
}