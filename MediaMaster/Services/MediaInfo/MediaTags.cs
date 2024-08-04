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

    public override void UpdateControl(bool isCompact)
    {
        if (TagView == null) return;
        TagView.MediaId = Media?.MediaId;
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

        MediaDbContext.MediaChanged += async (sender, args) =>
        {
            if (args.Media.MediaId != TagView.MediaId || ReferenceEquals(sender, this) || !args.Flags.HasFlag(MediaChangeFlags.TagsChanged)) return;
            var currentTagIds = TagView.Tags.Select(t => t.TagId).ToList();

            if (args.TagsAdded != null)
            {
                foreach (var tag in args.TagsAdded)
                {
                    if (!currentTagIds.Contains(tag.TagId))
                    {
                        TagView.Tags.Add(tag);
                    }
                }
            }

            if (args.TagsRemoved != null)
            {
                foreach (var tag in args.TagsRemoved)
                {
                    var existingTag = TagView.Tags.FirstOrDefault(t => t.TagId == tag.TagId);
                    if (existingTag != null)
                    {
                        TagView.Tags.Remove(existingTag);
                    }
                }
            }

            await TagView.UpdateItemSource(TagView.Tags);
        };
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
        return media == null | !isCompact;
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

    private async void SaveSelectedTags(ICollection<Tag> selectedTags)
    {
        await using (MediaDbContext dataBase = new())
        {
            if (Media != null)
            {
                Media = await dataBase.Medias.Include(m => m.Tags).FirstOrDefaultAsync(m => m.MediaId == Media.MediaId);

                if (Media != null)
                {
                    HashSet<int> currentTagIds = Media.Tags.Select(t => t.TagId).ToHashSet();
                    HashSet<int> selectedTagIds = selectedTags.Select(t => t.TagId).ToHashSet();

                    List<int> tagIdsToAdd = selectedTagIds.Except(currentTagIds).ToList();
                    List<int> tagIdsToRemove = currentTagIds.Except(selectedTagIds).ToList();

                    if (tagIdsToAdd.Count != 0 || tagIdsToRemove.Count != 0)
                    {
                        // Bulk add new tags
                        if (tagIdsToAdd.Count != 0)
                        {
                            List<MediaTag> newMediaTags = tagIdsToAdd.Select(tagId => new MediaTag { MediaId = Media.MediaId, TagId = tagId }).ToList();
                            await dataBase.BulkInsertAsync(newMediaTags);
                        }

                        // Bulk remove old tags
                        if (tagIdsToRemove.Count != 0)
                        {
                            List<MediaTag> mediaTagsToRemove = await dataBase.MediaTags
                                .Where(mt => mt.MediaId == Media.MediaId && tagIdsToRemove.Contains(mt.TagId))
                                .ToListAsync();
                            await dataBase.BulkDeleteAsync(mediaTagsToRemove);
                        }

                        if (MediaDbContext.ArchivedTag != null)
                        {
                            if (tagIdsToAdd.Contains(MediaDbContext.ArchivedTag.TagId))
                            {
                                Media.IsArchived = true;
                            }
                            else if (tagIdsToRemove.Contains(MediaDbContext.ArchivedTag.TagId))
                            {
                                Media.IsArchived = false;
                            }
                        }

                        if (MediaDbContext.FavoriteTag != null)
                        {
                            if (tagIdsToAdd.Contains(MediaDbContext.FavoriteTag.TagId))
                            {
                                Media.IsFavorite = true;
                            }
                            else if (tagIdsToRemove.Contains(MediaDbContext.FavoriteTag.TagId))
                            {
                                Media.IsFavorite = false;
                            }
                        }

                        Media.Modified = DateTime.UtcNow;
                        await dataBase.SaveChangesAsync();
                    }

                    ICollection<Tag> tagsToAdd = selectedTags.Where(tag => tagIdsToAdd.Contains(tag.TagId)).ToList();
                    ICollection<Tag> tagsToRemove = Media.Tags.Where(tag => tagIdsToRemove.Contains(tag.TagId)).ToList();
                    MediaDbContext.InvokeMediaChange(this, MediaChangeFlags.MediaChanged | MediaChangeFlags.TagsChanged, Media, tagsToAdd, tagsToRemove);
                }
            }
        }
    }
}

