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
    public TagView? TagView;
    public StackPanel? StackPanel;

    public override string TranslationKey { get; set; } = "MediaTags";

    public override void UpdateControl(Media? media, bool isCompact)
    {
        if (TagView == null) return;
        TagView.MediaId = media?.MediaId;
        TagView.AddTagButton = !isCompact;
        if (isCompact)
        {
            TagView.Layout = new StackLayout()
            {
                Orientation = Orientation.Horizontal,
                Spacing = 4
            };
        }
        else
        {
            TagView.Layout = new FlowLayout
            {
                MinColumnSpacing = 4,
                MinRowSpacing = 4
            };
        }

        if (Title != null)
        {
            Title.Visibility = isCompact ? Visibility.Collapsed : Visibility.Visible;
        }
    }

    public override void Setup(bool isCompact)
    {
        StackPanel = new StackPanel
        {
            Spacing = 10
        };
        Title = GetTitle();
        TagView = new TagView
        {
            MaxHeight = 200,
            AddTagButton = !isCompact
        };
        StackPanel.Children.Add(Title);
        StackPanel.Children.Add(TagView);
        Parent.Children.Add(StackPanel);

        TagView.SelectTagsInvoked += (_, _) => SaveSelectedTags(TagView.Tags);
        TagView.RemoveTagsInvoked += (_, _) => SaveSelectedTags(TagView.Tags);
    }

    public override void SetupTranslations()
    {
        if (Title != null)
        {
            Uids.SetUid(Title, $"/Media/{TranslationKey}_Title");
        }
    }

    public override bool ShowInfo(Media? media, bool isCompact)
    {
        return true;
    }

    public override void Show(bool isCompact)
    {
        if (StackPanel != null)
        {
            StackPanel.Visibility = Visibility.Visible;
        }
    }

    public override void Hide()
    {
        if (StackPanel != null)
        {
            StackPanel.Visibility = Visibility.Collapsed;
        }
    }

    private async void SaveSelectedTags(IEnumerable<Tag> selectedTags)
    {
        if (TagView == null) return;

        await using (MediaDbContext dataBase = new())
        {
            if (TagView.MediaId != null)
            {
                var trackedMedia = await dataBase.Medias.Include(m => m.Tags).FirstOrDefaultAsync(m => m.MediaId == TagView.MediaId);

                if (trackedMedia != null)
                {
                    HashSet<int> currentTagIds = trackedMedia.Tags.Select(t => t.TagId).ToHashSet();
                    HashSet<int> selectedTagIds = selectedTags.Select(t => t.TagId).ToHashSet();

                    List<int> tagsToAdd = selectedTagIds.Except(currentTagIds).ToList();
                    List<int> tagsToRemove = currentTagIds.Except(selectedTagIds).ToList();

                    if (tagsToAdd.Count != 0 || tagsToRemove.Count != 0)
                    {
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

                        trackedMedia.Modified = DateTime.UtcNow;
                        await dataBase.SaveChangesAsync();
                    }
                }
            }
        }
    }
}

