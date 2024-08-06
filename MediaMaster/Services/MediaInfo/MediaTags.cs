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

    public override void UpdateControl()
    {
        if (TagView == null) return;
        TagView.MediaIds = Medias.Select(m => m.MediaId).ToList();
        TagView.AddTagButton = !IsCompact;
        if (IsCompact)
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
            Title.Visibility = IsCompact ? Visibility.Collapsed : Visibility.Visible;
        }
    }

    public override void Setup()
    {
        StackPanel = new StackPanel
        {
            Spacing = 10
        };
        Title = GetTitle();
        TagView = new TagView
        {
            MaxHeight = 200,
            AddTagButton = !IsCompact
        };
        StackPanel.Children.Add(Title);
        StackPanel.Children.Add(TagView);
        Parent.Children.Add(StackPanel);

        TagView.SelectTagsInvoked += (_, _) => SaveSelectedTags(TagView.Tags);
        TagView.RemoveTagsInvoked += (_, _) => SaveSelectedTags(TagView.Tags);

        MediaDbContext.MediasChanged += async (sender, args) =>
        {
            if (!args.MediaIds.Intersect(TagView.MediaIds).Any() || ReferenceEquals(sender, this) || !args.Flags.HasFlag(MediaChangeFlags.TagsChanged)) return;
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

    public override bool ShowInfo(ICollection<Media> medias)
    {
        return medias.Count == 0 || !IsCompact;
    }

    public override void Show()
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
            if (Medias.Count != 0)
            {
                Medias = dataBase.Medias.Include(m => m.Tags).Where(media => Medias.Select(m => m.MediaId).Contains(media.MediaId)).ToList();

                if (Medias.Count != 0)
                {
                    HashSet<int> currentTagIds = Medias
                        .SelectMany(m => m.Tags).Select(t => t.TagId)
                        .GroupBy(t => t)
                        .Where(g => g.Count() == Medias.Count)
                        .Select(g => g.Key).ToHashSet();

                    HashSet<int> selectedTagIds = selectedTags.Select(t => t.TagId).ToHashSet();

                    List<int> tagIdsToAdd = selectedTagIds.Except(currentTagIds).ToList();
                    List<int> tagIdsToRemove = currentTagIds.Except(selectedTagIds).ToList();

                    if (tagIdsToAdd.Count != 0 || tagIdsToRemove.Count != 0)
                    {
                        foreach (var media in Medias)
                        {
                            // Bulk add new tags
                            if (tagIdsToAdd.Count != 0)
                            {
                                List<MediaTag> newMediaTags = tagIdsToAdd.Select(tagId => new MediaTag { MediaId = media.MediaId, TagId = tagId }).ToList();
                                await dataBase.BulkInsertOrUpdateAsync(newMediaTags);
                            }

                            // Bulk remove old tags
                            if (tagIdsToRemove.Count != 0)
                            {
                                List<MediaTag> mediaTagsToRemove = await dataBase.MediaTags
                                    .Where(mt => mt.MediaId == media.MediaId && tagIdsToRemove.Contains(mt.TagId))
                                    .ToListAsync();
                                await dataBase.BulkDeleteAsync(mediaTagsToRemove);
                            }

                            if (MediaDbContext.ArchivedTag != null)
                            {
                                if (tagIdsToAdd.Contains(MediaDbContext.ArchivedTag.TagId))
                                {
                                    media.IsArchived = true;
                                }
                                else if (tagIdsToRemove.Contains(MediaDbContext.ArchivedTag.TagId))
                                {
                                    media.IsArchived = false;
                                }
                            }

                            if (MediaDbContext.FavoriteTag != null)
                            {
                                if (tagIdsToAdd.Contains(MediaDbContext.FavoriteTag.TagId))
                                {
                                    media.IsFavorite = true;
                                }
                                else if (tagIdsToRemove.Contains(MediaDbContext.FavoriteTag.TagId))
                                {
                                    media.IsFavorite = false;
                                }
                            }

                            media.Modified = DateTime.UtcNow;
                        }

                        await dataBase.BulkUpdateAsync(Medias);
                    }

                    ICollection<Tag> tagsToAdd = selectedTags.Where(tag => tagIdsToAdd.Contains(tag.TagId)).ToList();
                    ICollection<Tag> tagsToRemove = Medias.SelectMany(m => m.Tags).Where(tag => tagIdsToRemove.Contains(tag.TagId)).ToList();
                    MediaDbContext.InvokeMediaChange(this, MediaChangeFlags.MediaChanged | MediaChangeFlags.TagsChanged, Medias, tagsToAdd, tagsToRemove);
                }
            }
        }
    }
}

