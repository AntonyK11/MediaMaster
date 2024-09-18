using System.Numerics;
using DependencyPropertyGenerator;
using EFCore.BulkExtensions;
using MediaMaster.DataBase;
using MediaMaster.Services;
using MediaMaster.Services.MediaInfo;
using Microsoft.EntityFrameworkCore;
using Microsoft.UI.Composition;
using Microsoft.UI.Xaml.Input;

namespace MediaMaster.Controls;

[DependencyProperty("IconHeight", typeof(int), DefaultValue = 300)]
[DependencyProperty("ForceUpdate", typeof(bool), DefaultValue = true)]
[DependencyProperty("ImageMode", typeof(ImageMode), DefaultValue = ImageMode.IconAndThumbnail)]
[DependencyProperty("IconMargin", typeof(Thickness), DefaultValueExpression = "new Thickness(0)")]
public partial class MediaViewer : UserControl
{
    private bool _listenForCheckUncheck = true;

    private MediaInfoService _mediaInfoService;
    private SpringScalarNaturalMotionAnimation? _rotationAnimation;
    private Vector3KeyFrameAnimation? _scaleAnimation;

    private SpringVector3NaturalMotionAnimation? _translationAnimation;

    private List<Media> _medias = [];

    public MediaViewer()
    {
        InitializeComponent();

        MediaDbContext.MediasChanged += (sender, args) =>
        {
            if (!args.MediaIds.Intersect(_medias.Select(m => m.MediaId)).Any() || ReferenceEquals(sender, this)) return;

            if (args.Flags.HasFlag(MediaChangeFlags.TagsChanged))
            {
                SetupToggleButtons(args.Medias);
            }

            if (args.Flags.HasFlag(MediaChangeFlags.UriChanged))
            {
                if (_medias.Count == 1)
                {
                    MediaIcon.Uris = [args.Medias.First(m => m.MediaId == _medias.First().MediaId).Uri];
                }
            }
        };

        _mediaInfoService = new MediaInfoService(DockPanel);
        _mediaInfoService.SetMedia([]);
    }

    public void SetMedias(HashSet<int> mediaIds)
    {
        _medias.Clear();
        if (mediaIds.Count != 0)
        {
            ArchiveToggleButton.IsEnabled = true;
            FavoriteToggleButton.IsEnabled = true;
            if (ForceUpdate)
            {
                using (var database = new MediaDbContext())
                {
                    _medias = database.Medias.Where(m => mediaIds.Contains(m.MediaId)).ToList();
                }
            }
        }
        else
        {
            ArchiveToggleButton.IsEnabled = false;
            FavoriteToggleButton.IsEnabled = false;
        }

        MediaIcon.Uris = _medias.Select(m => m.Uri).ToList();

        _mediaInfoService.SetMedia(_medias);

        SetupToggleButtons(_medias);
    }

    private void SetupToggleButtons(ICollection<Media> medias)
    {
        var checkFavorite = medias.Count != 0;
        var checkArchive = checkFavorite;

        foreach (Media media in medias)
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

                foreach (Media media in _medias)
                {
                    if (!media.IsFavorite)
                    {
                        var mediaTag = new MediaTag
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
                await database.BulkUpdateAsync(_medias);
            }

            MediaDbContext.InvokeMediaChange(this, MediaChangeFlags.MediaChanged | MediaChangeFlags.TagsChanged, _medias,
                [MediaDbContext.FavoriteTag]);
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
                HashSet<int> mediaIds = _medias.Select(media => media.MediaId).ToHashSet();
                List<MediaTag> mediaTags = await database.MediaTags
                    .Where(m => mediaIds.Contains(m.MediaId) && m.TagId == MediaDbContext.FavoriteTag.TagId)
                    .ToListAsync();
                await database.BulkDeleteAsync(mediaTags);

                foreach (Media media in _medias)
                {
                    media.IsFavorite = false;
                    media.Modified = DateTime.UtcNow;
                }

                await database.BulkUpdateAsync(_medias);
            }

            MediaDbContext.InvokeMediaChange(this, MediaChangeFlags.MediaChanged | MediaChangeFlags.TagsChanged, _medias,
                tagsRemoved: [MediaDbContext.FavoriteTag]);
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
                HashSet<int> mediaIds = _medias.Select(media => media.MediaId).ToHashSet();
                List<MediaTag> mediaTags = await database.MediaTags
                    .Where(m => mediaIds.Contains(m.MediaId) && m.TagId == MediaDbContext.ArchivedTag.TagId)
                    .ToListAsync();
                await database.BulkDeleteAsync(mediaTags);

                foreach (Media media in _medias)
                {
                    media.IsArchived = false;
                    media.Modified = DateTime.UtcNow;
                }

                await database.BulkUpdateAsync(_medias);
            }

            MediaDbContext.InvokeMediaChange(this, MediaChangeFlags.MediaChanged | MediaChangeFlags.TagsChanged, _medias,
                tagsRemoved: [MediaDbContext.ArchivedTag]);
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

                foreach (Media media in _medias)
                {
                    if (!media.IsArchived)
                    {
                        var mediaTag = new MediaTag
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
                await database.BulkUpdateAsync(_medias);
            }

            MediaDbContext.InvokeMediaChange(this, MediaChangeFlags.MediaChanged | MediaChangeFlags.TagsChanged, _medias,
                [MediaDbContext.ArchivedTag]);
        }
    }

    private void ArchiveToggleButton_OnPointerPressed(object sender, PointerRoutedEventArgs e)
    {
        CreateOrUpdateTranslationAnimation(0, 2);
        ArchiveIconGrid.StartAnimation(_translationAnimation);
    }
}