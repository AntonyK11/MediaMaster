using EFCore.BulkExtensions;
using MediaMaster.Controls;
using MediaMaster.DataBase;
using MediaMaster.DataBase.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using WinUI3Localizer;

namespace MediaMaster.Services.MediaInfo;

public class MediaTags(StackPanel parent) : MediaInfoControlBase(parent)
{
    public TextBlock? TextBlock;
    public TagView? TagView;

    public override string TranslationKey { get; set; } = "MediaTags";

    public override void Initialize(Media media)
    {
        base.Initialize(media);

        if (TagView == null) return;
        TagView.MediaId = media.MediaId;
    }

    public override void Setup()
    {
        var stackPanel = new StackPanel
        {
            Spacing = 10
        };
        TextBlock = new TextBlock();
        TagView = new TagView
        {
            Layout = new FlowLayout
            {
                MinColumnSpacing = 4,
                MinRowSpacing = 4
            }
        };
        stackPanel.Children.Add(TextBlock);
        stackPanel.Children.Add(TagView);
        Parent.Children.Add(stackPanel);

        TagView.SelectTagsInvoked += (_, _) => SaveSelectedTags(TagView.Tags);
        TagView.RemoveTagsInvoked += (_, _) => SaveSelectedTags(TagView.Tags);
    }

    public override void SetupTranslations()
    {
        if (TextBlock != null)
        {
            Uids.SetUid(TextBlock, $"/Media/{TranslationKey}_Title");
        }
    }

    public override void Show()
    {
        if (TextBlock != null && TagView != null)
        {
            TextBlock.Visibility = Visibility.Visible;
            TagView.Visibility = Visibility.Visible;
        }
    }

    public override void Hide()
    {
        if (TextBlock != null && TagView != null)
        {
            TextBlock.Visibility = Visibility.Collapsed;
            TagView.Visibility = Visibility.Collapsed;
        }
    }

    private async void SaveSelectedTags(IEnumerable<Tag> selectedTags)
    {
        if (TagView == null) return;

        await using (MediaDbContext dataBase = new())
        {
            if (TagView.MediaId != null)
            {
                var trackedMedia = await dataBase.Medias.Select(m => new { m.MediaId, Tags = m.Tags.Select(t => new { t.TagId }) }).FirstOrDefaultAsync(m => m.MediaId == TagView.MediaId);

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
}

