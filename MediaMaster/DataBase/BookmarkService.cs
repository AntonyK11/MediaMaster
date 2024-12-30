using BookmarksManager;
using CommunityToolkit.WinUI;
using MediaMaster.Services;
using MediaMaster.Views.Dialog;
using Microsoft.EntityFrameworkCore;

namespace MediaMaster.DataBase;

public static class BookmarkService
{
    public static async Task AddBookmarksAsync(
        ICollection<BrowserFolder>? browserFolders = null,
        HashSet<int>? userTagsId = null,
        string userNotes = "",
        bool generateBookmarkTags = true)
    {
        if (MediaService.IsRunning)
        {
            await App.DispatcherQueue.EnqueueAsync(MediaService.CannotAddMedias);
            return;
        }

        MediaService.IsRunning = true;
        var mediaAddedCount = 0;

        var isFavorite = MediaDbContext.FavoriteTag != null && userTagsId?.Contains(MediaDbContext.FavoriteTag.TagId) == true;
        var isArchive = MediaDbContext.ArchivedTag != null && userTagsId?.Contains(MediaDbContext.ArchivedTag.TagId) == true;

        await using (var database = new MediaDbContext())
        {
            MediaService.Watch.Restart();

            var transactionSuccessful = await Transaction.Try(database, async () =>
            {
                Dictionary<string, Tag> tags = await database.Tags
                    .Select(t => new Tag { TagId = t.TagId, Name = t.Name })
                    .GroupBy(t => t.Name)
                    .Select(g => g.First())
                    .ToDictionaryAsync(t => t.Name);

                HashSet<string> existingMediaUris = await database.Medias
                    .Select(m => m.Uri)
                    .ToHashSetAsync();
                
                var mediaBatch = new List<Media>(MediaService.BatchSize);
                var tagBatch = new List<Tag>(MediaService.BatchSize);
                
                Task? task = null;
                foreach (var (mediaItem, tagItem) in EnumerateBookmarkItems(browserFolders, existingMediaUris, tags, isFavorite, isArchive, userNotes, generateBookmarkTags))
                {
                    mediaBatch.Add(mediaItem);
                    tagBatch.AddRange(tagItem);

                    if (mediaBatch.Count >= MediaService.BatchSize)
                    {
                        mediaAddedCount += mediaBatch.Count;
                        if (task is { IsCompleted: false })
                        {
                            await task;
                        }
                        task = MediaService.ProcessBatch(mediaBatch, tagBatch, userTagsId, database);
                    }
                }
                
                if (task is { IsCompleted: false })
                {
                    await task;
                }
                
                if (mediaBatch.Count > 0)
                {
                    mediaAddedCount += mediaBatch.Count;
                    await MediaService.ProcessBatch(mediaBatch, tagBatch, userTagsId, database);
                }

                Debug.WriteLine($"Added {mediaAddedCount} bookmarks in {MediaService.Watch.Elapsed}");
                MediaService.Watch.Stop();

                await App.DispatcherQueue.EnqueueAsync(() => MediaService.MediasAdded(mediaAddedCount));

                tags.Clear();
                existingMediaUris.Clear();
            });

            if (transactionSuccessful)
            {
                MediaDbContext.InvokeMediaChange(null, MediaChangeFlags.MediaAdded, mediaCount: mediaAddedCount);
            }
        }

        MediaService.IsRunning = false;
    }

    private static IEnumerable<(Media, ICollection<Tag>)> EnumerateBookmarkItems(
        ICollection<BrowserFolder>? browserFolders,
        HashSet<string> existingMediaUris,
        Dictionary<string, Tag> tags,
        bool isFavorite,
        bool isArchive,
        string userNotes,
        bool generateBookmarkTags)
    {
        if (browserFolders == null) yield break;

        foreach (var folder in browserFolders)
        {
            foreach (var (mediaItem, tagItem) in EnumerateBookmarkItem(folder.BookmarkFolder, existingMediaUris, tags,
                         isFavorite, isArchive, userNotes, generateBookmarkTags))
            {
                yield return (mediaItem, tagItem);
            }
        }
    }

    private static IEnumerable<(Media, ICollection<Tag>)> EnumerateBookmarkItem(
        BookmarkFolder browserFolder,
        HashSet<string> existingMediaUris,
        Dictionary<string, Tag> tags,
        bool isFavorite,
        bool isArchive,
        string userNotes,
        bool generateBookmarkTags)
    {
        foreach (var folder in browserFolder)
        {
            switch (folder)
            {
                case BookmarkFolder subFolder:
                    foreach (var (mediaItem, tagItem) in EnumerateBookmarkItem(
                                 subFolder,
                                 existingMediaUris,
                                 tags,
                                 isFavorite,
                                 isArchive,
                                 userNotes,
                                 generateBookmarkTags))
                    {
                        yield return (mediaItem, tagItem);
                    }

                    break;

                case BookmarkLink link when !existingMediaUris.Contains(link.Url):
                    existingMediaUris.Add(link.Url);
                    yield return AddBookmarkMedia(link, browserFolder, tags, isFavorite, isArchive, userNotes,
                        generateBookmarkTags);
                    break;
            }
        }
    }

    private static (Media, ICollection<Tag>) AddBookmarkMedia(
        BookmarkLink bookmarkLink,
        BookmarkFolder parentFolder,
        Dictionary<string, Tag> tags,
        bool isFavorite,
        bool isArchive,
        string userNotes,
        bool generateBookmarkTags)
    {
        var notes = bookmarkLink.Description;
        if (notes == null)
        {
            notes = userNotes;
        }
        else
        {
            notes += "\n\n" + userNotes;
        }

        var media = new Media
        {
            Name = bookmarkLink.Title,
            Notes = notes,
            Uri = bookmarkLink.Url,
            IsFavorite = isFavorite,
            IsArchived = isArchive
        };

        var (isNew, tag) = MediaService.GetWebsiteTag(bookmarkLink.Url, tags);
        ICollection<Tag> newTags = [];
        if (tag != null)
        {
            if (isNew)
            {
                newTags.Add(tag);
            }

            media.Tags.Add(tag);
        }

        if (generateBookmarkTags)
        {
            var (isParentNew, parentTag) = EnsureBookmarkFolderTag(parentFolder.Title, null, tags);
            if (isParentNew)
            {
                newTags.Add(parentTag);
            }

            media.Tags.Add(parentTag);
        }

        return (media, newTags);
    }

    private static (bool isNew, Tag tag) EnsureBookmarkFolderTag(string folderTitle, Tag? parentTag,
        Dictionary<string, Tag> tags)
    {
        var isNew = false;
        if (!tags.TryGetValue(folderTitle, out var tag))
        {
            isNew = true;
            tag = new Tag
            {
                Name = folderTitle,
                Flags = TagFlags.UserTag
            };

            if (parentTag != null)
            {
                tag.Parents.Add(parentTag);
                tag.FirstParentReferenceName = parentTag.ReferenceName;
            }

            tags.Add(folderTitle, tag);
        }

        return (isNew, tag);
    }
}