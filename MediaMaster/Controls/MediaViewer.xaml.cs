using EFCore.BulkExtensions;
using MediaMaster.DataBase;
using MediaMaster.DataBase.Models;
using MediaMaster.Services;
using Microsoft.EntityFrameworkCore;
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

    private MyCancellationTokenSource? _tokenSource;

    public MediaViewer()
    {
        InitializeComponent();

        TagView.SelectTagsInvoked += (_, _) => SaveSelectedTags(TagView.Tags);
        TagView.RemoveTagsInvoked += (_, _) => SaveSelectedTags(TagView.Tags);
    }

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
                TagView.MediaId = value?.MediaId;
            }
        }
    }

    private async void SaveSelectedTags(IEnumerable<Tag> selectedTags)
    {
        await using (MediaDbContext dataBase = new())
        {
            if (Media != null)
            {
                var trackedMedia = await dataBase.Medias.Select(m => new { m.MediaId, Tags = m.Tags.Select(t => new { t.TagId }) }).FirstOrDefaultAsync(m => m.MediaId == Media.MediaId);

                if (trackedMedia != null)
                {
                    HashSet<int> currentTagIds = trackedMedia.Tags.Select(t => t.TagId).ToHashSet();
                    HashSet<int> selectedTagIds = selectedTags.Select(t => t.TagId).ToHashSet();

                    List<int> tagsToAdd = selectedTagIds.Except(currentTagIds).ToList();
                    List<int> tagsToRemove = currentTagIds.Except(selectedTagIds).ToList();

                    // Bulk add new tags
                    if (tagsToAdd.Count != 0)
                    {
                        List<MediaTag> newMediaTags = tagsToAdd.Select(tagId => new MediaTag { MediaId = trackedMedia.MediaId, TagId = tagId }).ToList();
                        await dataBase.BulkInsertAsync(newMediaTags);
                    }

                    // Bulk remove old tags
                    if (tagsToRemove.Count != 0)
                    {
                        List<MediaTag> mediaTagsToRemove = await dataBase.MediaTags
                            .Where(mt => mt.MediaId == trackedMedia.MediaId && tagsToRemove.Contains(mt.TagId))
                            .ToListAsync();
                        await dataBase.BulkDeleteAsync(mediaTagsToRemove);
                    }
                }
            }
        }
    }

    private void SetMediaExtensionIcon()
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