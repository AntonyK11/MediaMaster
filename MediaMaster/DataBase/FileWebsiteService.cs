using CommunityToolkit.WinUI;
using HtmlAgilityPack;
using MediaMaster.Extensions;
using MediaMaster.Services;
using Microsoft.EntityFrameworkCore;

namespace MediaMaster.DataBase;

public static class FileWebsiteService
{
    public static Task AddMediaAsync(IEnumerable<string> uris, HashSet<int>? userTagsId = null, string userNotes = "")
    {
        var trimmedUris = uris.Select(uri => new KeyValuePair<string?, string>(null, uri.Trim()));
        return AddMediaAsync(trimmedUris, userTagsId: userTagsId, userNotes: userNotes);
    }

    public static async Task AddMediaAsync(
        IEnumerable<KeyValuePair<string?, string>>? nameUris = null,
        HashSet<int>? userTagsId = null,
        string userNotes = "")
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
                foreach (var (mediaItem, tagItem) in EnumerateMediaItems(nameUris, existingMediaUris, tags, isFavorite, isArchive, userNotes))
                {
                    mediaBatch.Add(mediaItem);
                    if (tagItem != null)
                    {
                        tagBatch.Add(tagItem);
                    }
                    
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
                    if (task is { IsCompleted: false })
                    {
                        await task;
                    }
                    await MediaService.ProcessBatch(mediaBatch, tagBatch, userTagsId, database);
                }
                
                Debug.WriteLine($"Added {mediaAddedCount} medias in {MediaService.Watch.Elapsed}");
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
    
    private static IEnumerable<(Media, Tag?)> EnumerateMediaItems(
        IEnumerable<KeyValuePair<string?, string>>? nameUris,
        HashSet<string> existingMediaUris,
        Dictionary<string, Tag> tags,
        bool isFavorite,
        bool isArchive,
        string userNotes)
    {
        if (nameUris == null) yield break;

        foreach (var nameUri in nameUris)
        {
            if (nameUri.Value.IsWebsite())
            {
                if (!existingMediaUris.Add(nameUri.Value))
                {
                    continue;
                }

                yield return AddWebMedia(nameUri, tags, isFavorite, isArchive, userNotes);
            }
            else
            {
                foreach (var file in GetFiles(nameUri).Where(p => !existingMediaUris.Contains(p.Value)))
                {
                    existingMediaUris.Add(nameUri.Value);
                    yield return AddFileMedia(file, tags, isFavorite, isArchive, userNotes);
                }
            }
        }
    }
    
    private static IEnumerable<KeyValuePair<string?, string>> GetFiles(KeyValuePair<string?, string> nameUri)
    {
        var path = nameUri.Value;
        if (Directory.Exists(path))
        {
            EnumerationOptions opt = new()
            {
                RecurseSubdirectories = true,
                IgnoreInaccessible = true,
                ReturnSpecialDirectories = true,
                AttributesToSkip = FileAttributes.None
            };
            return Directory.EnumerateFiles(path, "*", opt)
                .Select(f => new KeyValuePair<string?, string>(null, f));
        }

        return File.Exists(path) ? new[] { nameUri } : Array.Empty<KeyValuePair<string?, string>>();
    }
    
    private static (Media, Tag?) AddFileMedia(
        KeyValuePair<string?, string> nameUri,
        Dictionary<string, Tag> tags,
        bool isFavorite,
        bool isArchive,
        string userNotes)
    {
        var media = new Media
        {
            Name = nameUri.Key ?? Path.GetFileNameWithoutExtension(nameUri.Value),
            Notes = userNotes,
            Uri = nameUri.Value,
            IsFavorite = isFavorite,
            IsArchived = isArchive
        };

        var (isNew, tag) = MediaService.GetFileTag(nameUri.Value, tags);
        Tag? newTag = null;
        if (tag != null)
        {
            if (isNew)
            {
                newTag = tag;
            }
            
            media.Tags.Add(tag);
        }

        return (media, newTag);
    }
    
    private static (Media, Tag?) AddWebMedia(
        KeyValuePair<string?, string> nameUri,
        Dictionary<string, Tag> tags,
        bool isFavorite,
        bool isArchive,
        string userNotes)
    {
        var title = nameUri.Key;
        if (title == null)
        {
            var webGet = new HtmlWeb();
            var document = webGet.Load(nameUri.Value);
            title = document.DocumentNode.SelectSingleNode("html/head/title")?.InnerText ?? string.Empty;
        }

        var media = new Media
        {
            Name = title,
            Notes = userNotes,
            Uri = nameUri.Value,
            IsFavorite = isFavorite,
            IsArchived = isArchive
        };

        var (isNew, tag) = MediaService.GetWebsiteTag(nameUri.Value, tags);
        Tag? newTag = null;
        if (tag != null)
        {
            if (isNew)
            {
                newTag = tag;
            }
            
            media.Tags.Add(tag);
        }

        return (media, newTag);
    }
}
