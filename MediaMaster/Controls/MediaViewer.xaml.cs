using System.Numerics;
using DependencyPropertyGenerator;
using EFCore.BulkExtensions;
using MediaMaster.DataBase;
using MediaMaster.Services;
using MediaMaster.Services.MediaInfo;
using Microsoft.EntityFrameworkCore;

namespace MediaMaster.Controls;

[DependencyProperty("IconHeight", typeof(int), DefaultValue = 300)]
[DependencyProperty("ForceUpdate", typeof(bool), DefaultValue = true)]
[DependencyProperty("ImageMode", typeof(ImageMode), DefaultValue = ImageMode.IconAndThumbnail)]
[DependencyProperty("IconMargin", typeof(Thickness), DefaultValueExpression = "new Thickness(0)")]
public sealed partial class MediaViewer : UserControl
{
    private bool _listenForCheckUncheck = true;

    private MediaInfoService _mediaInfoService;

    private List<Media> _medias = [];

    public MediaViewer()
    {
        InitializeComponent();

        MediaDbContext.MediasChanged += (sender, args) =>
        {
            var intersection = args.MediaIds.Intersect(_medias.Select(m => m.MediaId)).ToHashSet();
            if (intersection.Count == 0 || ReferenceEquals(sender, this)) return;

            if (args.Flags.HasFlag(MediaChangeFlags.TagsChanged))
            {
                foreach (var media in _medias.Where(media => intersection.Contains(media.MediaId)).ToList())
                {
                    _medias.Remove(media);
                    _medias.Add(args.Medias.First(m => m.MediaId == media.MediaId));
                }

                SetupToggleButtons(_medias);
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

    private async void FavoriteToggleButton_OnChecked(object sender, RoutedEventArgs e)
    {
        if (MediaDbContext.FavoriteTag != null && _listenForCheckUncheck)
        {
            await using (var database = new MediaDbContext())
            {
                var transactionSuccessful = await Transaction.Try(database, async () =>
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
                });

                if (transactionSuccessful)
                {
                    MediaDbContext.InvokeMediaChange(this, MediaChangeFlags.MediaChanged | MediaChangeFlags.TagsChanged,
                        _medias, [MediaDbContext.FavoriteTag]);
                }
            }
        }
    }

    private async void FavoriteToggleButton_OnUnchecked(object sender, RoutedEventArgs e)
    {
        if (MediaDbContext.FavoriteTag != null && _listenForCheckUncheck)
        {
            await using (var database = new MediaDbContext())
            {
                var transactionSuccessful = await Transaction.Try(database, async () =>
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
                });

                if (transactionSuccessful)
                {
                    MediaDbContext.InvokeMediaChange(this, MediaChangeFlags.MediaChanged | MediaChangeFlags.TagsChanged,
                        _medias, tagsRemoved: [MediaDbContext.FavoriteTag]);
                }
            }
        }
    }

    private async void ArchiveToggleButton_OnUnchecked(object sender, RoutedEventArgs e)
    {
        if (MediaDbContext.ArchivedTag != null && _listenForCheckUncheck)
        {
            await using (var database = new MediaDbContext())
            {
                var transactionSuccessful = await Transaction.Try(database, async () =>
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
                });

                if (transactionSuccessful)
                {
                    MediaDbContext.InvokeMediaChange(this, MediaChangeFlags.MediaChanged | MediaChangeFlags.TagsChanged,
                        _medias, tagsRemoved: [MediaDbContext.ArchivedTag]);
                }
            }
        }
    }

    private async void ArchiveToggleButton_OnChecked(object sender, RoutedEventArgs e)
    {
        if (MediaDbContext.ArchivedTag != null && _listenForCheckUncheck)
        {
            await using (var database = new MediaDbContext())
            {
                var transactionSuccessful = await Transaction.Try(database, async () =>
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

                });

                if (transactionSuccessful)
                {
                    MediaDbContext.InvokeMediaChange(this, MediaChangeFlags.MediaChanged | MediaChangeFlags.TagsChanged,
                        _medias, [MediaDbContext.ArchivedTag]);
                }
            }
        }
    }
}