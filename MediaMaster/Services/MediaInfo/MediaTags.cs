﻿using CommunityToolkit.WinUI.Controls;
using EFCore.BulkExtensions;
using MediaMaster.Controls;
using MediaMaster.DataBase;
using Microsoft.EntityFrameworkCore;
using WinUI3Localizer;

namespace MediaMaster.Services.MediaInfo;

public sealed class MediaTags(DockPanel parent) : MediaInfoControlBase(parent)
{
    private StackPanel? _stackPanel;
    private TagView? _tagView;

    protected override string TranslationKey => "MediaTags";

    protected override void UpdateControl()
    {
        if (_tagView == null) return;
        _tagView.MediaIds = Medias.Select(m => m.MediaId).ToHashSet();
    }

    protected override void Setup()
    {
        _stackPanel = new StackPanel
        {
            Spacing = 10
        };
        _stackPanel.SetValue(DockPanel.DockProperty, Dock.Top);
        AddAnimation(_stackPanel);

        Title = GetTitleTextBlock();
        _tagView = new TagView
        {
            MaxHeight = 200
        };
        _stackPanel.Children.Add(Title);
        _stackPanel.Children.Add(_tagView);
        Parent.Children.Add(_stackPanel);

        _tagView.Layout = new WrapLayout
        {
            HorizontalSpacing = 4,
            VerticalSpacing = 4
        };

        _tagView.SelectTagsInvoked += (_, _) => SaveSelectedTags(_tagView.Tags);
        _tagView.RemoveTagsInvoked += (_, _) => SaveSelectedTags(_tagView.Tags);

        MediaDbContext.MediasChanged += async (sender, args) =>
        {
            if (!args.MediaIds.Intersect(_tagView.MediaIds).Any() || ReferenceEquals(sender, this) ||
                !args.Flags.HasFlag(MediaChangeFlags.TagsChanged)) return;
            HashSet<int> currentTagIds = _tagView.Tags.Select(t => t.TagId).ToHashSet();

            if (args.TagsAdded != null)
            {
                foreach (Tag tag in args.TagsAdded)
                {
                    if (!currentTagIds.Contains(tag.TagId))
                    {
                        _tagView.Tags.Add(tag);
                    }
                }
            }

            if (args.TagsRemoved != null)
            {
                foreach (Tag tag in args.TagsRemoved)
                {
                    Tag? existingTag = _tagView.Tags.FirstOrDefault(t => t.TagId == tag.TagId);
                    if (existingTag != null)
                    {
                        _tagView.Tags.Remove(existingTag);
                    }
                }
            }

            await _tagView.UpdateItemSource(_tagView.Tags);
        };
    }

    protected override void SetupTranslations()
    {
        if (Title != null)
        {
            Uids.SetUid(Title, $"/Media/{TranslationKey}_Title");
        }
    }

    protected override bool ShowInfo(ICollection<Media> medias)
    {
        return medias.Count != 0;
    }

    protected override void Show()
    {
        if (_stackPanel != null)
        {
            _stackPanel.Visibility = Visibility.Visible;
        }
    }

    protected override void Hide()
    {
        if (_stackPanel != null)
        {
            _stackPanel.Visibility = Visibility.Collapsed;
        }
    }

    private async void SaveSelectedTags(ICollection<Tag> selectedTags)
    {
        if (Medias.Count != 0)
        {
            await using (var database = new MediaDbContext())
            {
                ICollection<Tag> tagsToAdd = [];
                ICollection<Tag> tagsToRemove = [];

                var transactionSuccessful = await Transaction.Try(database, async () =>
                {
                    Medias = database.Medias.Include(m => m.Tags)
                        .Where(media => Medias.Select(m => m.MediaId).Contains(media.MediaId)).ToList();

                    if (Medias.Count != 0)
                    {
                        HashSet<int> currentTagIds = Medias
                            .SelectMany(m => m.Tags).Select(t => t.TagId)
                            .GroupBy(t => t)
                            .Where(g => g.Count() == Medias.Count)
                            .Select(g => g.Key).ToHashSet();

                        HashSet<int> selectedTagIds = selectedTags.Select(t => t.TagId).ToHashSet();

                        HashSet<int> tagIdsToAdd = selectedTagIds.Except(currentTagIds).ToHashSet();
                        HashSet<int> tagIdsToRemove = currentTagIds.Except(selectedTagIds).ToHashSet();

                        if (tagIdsToAdd.Count != 0 || tagIdsToRemove.Count != 0)
                        {
                            foreach (Media media in Medias)
                            {
                                // Bulk add new tags
                                if (tagIdsToAdd.Count != 0)
                                {
                                    List<MediaTag> newMediaTags = tagIdsToAdd.Select(tagId =>
                                        new MediaTag { MediaId = media.MediaId, TagId = tagId }).ToList();
                                    await database.BulkInsertOrUpdateAsync(newMediaTags);
                                }

                                // Bulk remove old tags
                                if (tagIdsToRemove.Count != 0)
                                {
                                    List<MediaTag> mediaTagsToRemove = await database.MediaTags
                                        .Where(mt => mt.MediaId == media.MediaId && tagIdsToRemove.Contains(mt.TagId))
                                        .ToListAsync();
                                    await database.BulkDeleteAsync(mediaTagsToRemove);
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

                            await database.BulkUpdateAsync(Medias);

                            tagsToAdd =
                                selectedTags.Where(tag => tagIdsToAdd.Contains(tag.TagId)).ToList();
                            tagsToRemove = Medias
                                .SelectMany(m => m.Tags)
                                .Where(tag => tagIdsToRemove.Contains(tag.TagId))
                                .ToList();
                        }
                    }
                });

                if (transactionSuccessful)
                {
                    MediaDbContext.InvokeMediaChange(this,
                        MediaChangeFlags.MediaChanged | MediaChangeFlags.TagsChanged,
                        Medias, tagsToAdd, tagsToRemove);
                }
            }
        }
    }
}