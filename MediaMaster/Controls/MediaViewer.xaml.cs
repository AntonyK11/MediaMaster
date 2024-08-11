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
    public static readonly DependencyProperty? MediasProperty
        = DependencyProperty.Register(
            nameof(Medias),
            typeof(ICollection<Media>),
            typeof(MediaViewer),
            new PropertyMetadata(null));

    public ICollection<Media> Medias
    {
        get
        {
            var medias = (ICollection<Media>?)GetValue(MediasProperty);
            if (medias == null)
            {
                medias = [];
                SetValue(MediasProperty, medias);
            }
            return medias;
        }
        set
        {
            //Visibility = value != null ? Visibility.Visible : Visibility.Collapsed;

            ICollection<Media> updatedMedias = value;
            if (value.Count != 0)
            {
                ArchiveToggleButton.IsEnabled = true;
                FavoriteToggleButton.IsEnabled = true;
                if (ForceUpdate)
                {
                    updatedMedias = [];
                    using (var database = new MediaDbContext())
                    {
                        var mediaIds = value.Select(m => m.MediaId).ToHashSet();
                        var foundMedias = database.Medias.Where(m => mediaIds.Contains(m.MediaId)).ToList();
                        foreach (var foundMedia in foundMedias)
                        {
                            updatedMedias.Add(foundMedia);
                        }
                    }
                }
            }
            else
            {
                ArchiveToggleButton.IsEnabled = false;
                FavoriteToggleButton.IsEnabled = false;
            }
            SetValue(MediasProperty, updatedMedias);
            _mediaInfoService.SetMedia(updatedMedias, IsCompact);

            SetupToggleButtons(updatedMedias);
        }
    }

    public Media? Media
    {
        get => Medias.FirstOrDefault();
        set
        {
            if (value != null)
            {
                Medias = [value];
            }
            else
            {
                Medias = [];
            }
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
                _mediaInfoService.SetMedia([], value);
            }
            else
            {
                ScrollView.Visibility = Visibility.Visible;
                StackPanelCompact.Visibility = Visibility.Collapsed;
                ProtectedCursor = InputSystemCursor.Create(InputSystemCursorShape.Arrow);
                _mediaInfoService = new MediaInfoService(StackPanel);
                _mediaInfoService.SetMedia([], value);
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
        _mediaInfoService.SetMedia([], IsCompact);

        MediaDbContext.MediasChanged += (sender, args) =>
        {
            if (!args.MediaIds.Intersect(Medias.Select(m => m.MediaId)).Any() || ReferenceEquals(sender, this) || !args.Flags.HasFlag(MediaChangeFlags.TagsChanged)) return;
            SetupToggleButtons(args.Medias);
        };
    }

    private bool _listenForCheckUncheck = true;

    public void SetupToggleButtons(ICollection<Media> medias)
    {
        var checkFavorite = medias.Count != 0;
        var checkArchive = checkFavorite;

        foreach (var media in medias)
        {
            if (!media.IsFavorite)
            {
                checkFavorite = false;
            }
            if (!media.IsArchived)
            {
                checkArchive = false;
            }
        }

        _listenForCheckUncheck = false;
        if (checkFavorite != FavoriteToggleButton.IsChecked)
        {
            FavoriteToggleButton.IsChecked = checkFavorite;
        }

        if (checkArchive != ArchiveToggleButton.IsChecked)
        {
            ArchiveToggleButton.IsChecked = checkArchive;
        }
        _listenForCheckUncheck = true;
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

        if (MediaDbContext.FavoriteTag != null && _listenForCheckUncheck)
        {
            await using (var database = new MediaDbContext())
            {
                ICollection<MediaTag> mediaTags = [];

                foreach (var media in Medias)
                {
                    if (!media.IsFavorite)
                    {
                        var mediaTag = new MediaTag()
                        {
                            MediaId = media.MediaId,
                            TagId = MediaDbContext.FavoriteTag.TagId
                        };
                        mediaTags.Add(mediaTag);
                    }

                    media.IsFavorite = true;
                    media.Modified = DateTime.UtcNow;
                }

                await database.BulkInsertAsync(mediaTags);
                await database.BulkUpdateAsync(Medias);
            }

            MediaDbContext.InvokeMediaChange(this, MediaChangeFlags.MediaChanged | MediaChangeFlags.TagsChanged, Medias, tagsAdded: [MediaDbContext.FavoriteTag]);
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

        if (MediaDbContext.FavoriteTag != null && _listenForCheckUncheck)
        {
            await using (var database = new MediaDbContext())
            {
                var mediaIds = Medias.Select(media => media.MediaId).ToHashSet();
                var mediaTags = await database.MediaTags.Where(m => mediaIds.Contains(m.MediaId) && m.TagId == MediaDbContext.FavoriteTag.TagId).ToListAsync();
                await database.BulkDeleteAsync(mediaTags);

                foreach (var media in Medias)
                {
                    media.IsFavorite = false;
                    media.Modified = DateTime.UtcNow;
                }

                await database.BulkUpdateAsync(Medias);
            }
            MediaDbContext.InvokeMediaChange(this, MediaChangeFlags.MediaChanged | MediaChangeFlags.TagsChanged, Medias, tagsRemoved: [MediaDbContext.FavoriteTag]);
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

        if (MediaDbContext.ArchivedTag != null && _listenForCheckUncheck)
        {
            await using (var database = new MediaDbContext())
            {
                var mediaIds = Medias.Select(media => media.MediaId).ToHashSet();
                var mediaTags = await database.MediaTags.Where(m => mediaIds.Contains(m.MediaId) && m.TagId == MediaDbContext.ArchivedTag.TagId).ToListAsync();
                await database.BulkDeleteAsync(mediaTags);

                foreach (var media in Medias)
                {
                    media.IsArchived = false;
                    media.Modified = DateTime.UtcNow;
                }

                await database.BulkUpdateAsync(Medias);
            }

            MediaDbContext.InvokeMediaChange(this, MediaChangeFlags.MediaChanged | MediaChangeFlags.TagsChanged, Medias, tagsRemoved: [MediaDbContext.ArchivedTag]);
        }
    }

    private async void ArchiveToggleButton_OnChecked(object sender, RoutedEventArgs e)
    {
        CreateOrUpdateTranslationAnimation(2, 0);
        ArchiveIconGrid.StartAnimation(_translationAnimation);

        CreateOrUpdateScaleAnimation(1, 1.2f, 1);
        ArchiveIconGrid.StartAnimation(_scaleAnimation);

        if (MediaDbContext.ArchivedTag != null && _listenForCheckUncheck)
        {
            await using (var database = new MediaDbContext())
            {
                ICollection<MediaTag> mediaTags = [];

                foreach (var media in Medias)
                {
                    if (!media.IsArchived)
                    {
                        var mediaTag = new MediaTag()
                        {
                            MediaId = media.MediaId,
                            TagId = MediaDbContext.ArchivedTag.TagId
                        };
                        mediaTags.Add(mediaTag);
                    }

                    media.IsArchived = true;
                    media.Modified = DateTime.UtcNow;
                }

                await database.BulkInsertAsync(mediaTags);
                await database.BulkUpdateAsync(Medias);
            }

            MediaDbContext.InvokeMediaChange(this, MediaChangeFlags.MediaChanged | MediaChangeFlags.TagsChanged, Medias, tagsAdded: [MediaDbContext.ArchivedTag]);
        }
    }

    private void ArchiveToggleButton_OnPointerPressed(object sender, PointerRoutedEventArgs e)
    {
        CreateOrUpdateTranslationAnimation(0, 2);
        ArchiveIconGrid.StartAnimation(_translationAnimation);
    }
}