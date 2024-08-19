using BookmarksManager;
using EFCore.BulkExtensions;
using MediaMaster.Extensions;
using MediaMaster.Views.Dialog;
using Microsoft.EntityFrameworkCore;

namespace MediaMaster.DataBase;

using System.Collections.Generic;
using System.Linq;
using HtmlAgilityPack;


public static class MediaService
{
    private static bool _isRunning;

    public static Task<bool> AddMediaAsync(IEnumerable<string> uris)
    {
        return AddMediaAsync(uris.Select(uri => new KeyValuePair<string?, string>(null, uri.Trim())));
    }


    public static async Task<bool> AddMediaAsync(IEnumerable<KeyValuePair<string?, string>>? nameUris = null, ICollection<BrowserFolder>? browserFolders = null, bool generateBookmarkTags = true)
    {
        if (_isRunning)
        {
            return false;
        }

        _isRunning = true;
        try
        {
            await using (MediaDbContext database = new())
            {
                var tags = await database.Tags.Select(t => new Tag{ TagId = t.TagId, Name = t.Name }).GroupBy(t => t.Name).Select(g => g.First()).ToDictionaryAsync(t => t.Name);
                var medias = database.Medias.Select(m => m.Uri).ToHashSet();

                ICollection<Tag> newTags = [];
                ICollection<Media> newMedias = [];

                if (nameUris != null)
                {
                    await GetMedias(nameUris, medias, newMedias, tags, newTags);
                }

                if (browserFolders != null)
                {
                    foreach (var browserFolder in browserFolders)
                    {
                        await GetBookmarks(browserFolder.BookmarkFolder, null, medias, newMedias, tags, newTags, generateBookmarkTags ? 0 : -1);
                    }
                }

                await database.BulkInsertAsync(newMedias, new BulkConfig { SetOutputIdentity = true });
                await database.BulkInsertAsync(newTags, new BulkConfig { SetOutputIdentity = true });

                var mediaTags = await AddNewMedias(newMedias, database);
                var tagTags = await AddNewTags(newTags, database);

                Debug.WriteLine($"Media: {newMedias.Count}");
                Debug.WriteLine($"Tags: {newTags.Count}");
                Debug.WriteLine($"MediaTags: {mediaTags.Count}");
                Debug.WriteLine($"MediaTags: {tagTags.Count}");

                MediaDbContext.InvokeMediaChange(null, MediaChangeFlags.MediaAdded, newMedias);

                tags.Clear();
                medias.Clear();
                newTags.Clear();
                newMedias.Clear();
                mediaTags.Clear();
            }

            GC.Collect();
            GC.WaitForPendingFinalizers();
        }
        catch (Exception e)
        {
            Debug.WriteLine(e);
            _isRunning = false;
            return false;
        }

        _isRunning = false;
        return true;
    }

    public static async Task<ICollection<MediaTag>> AddNewMedias(ICollection<Media> newMedias, MediaDbContext database)
    {
        var mediaTags = newMedias.SelectMany(media => media.Tags.Select(tag => new MediaTag
        {
            MediaId = media.MediaId,
            TagId = tag.TagId
        })).ToList();

        await database.BulkInsertOrUpdateAsync(mediaTags);
        return mediaTags;
    }

    public static async Task<ICollection<TagTag>> AddNewTags(ICollection<Tag> newTags, MediaDbContext database)
    {
        var tagTags = newTags.SelectMany(tag => tag.Parents.Select(parent => new TagTag
        {
            ParentsTagId = parent.TagId,
            ChildrenTagId = tag.TagId
        })).ToList();

        await database.BulkInsertOrUpdateAsync(tagTags);
        return tagTags;
    }

    private static IEnumerable<KeyValuePair<string?, string>> GetFiles(KeyValuePair<string?, string> nameUris)
    {
        if (Directory.Exists(nameUris.Value))
        {
            EnumerationOptions opt = new()
            {
                RecurseSubdirectories = true,
                IgnoreInaccessible = true,
                ReturnSpecialDirectories = true
            };
            return Directory.EnumerateFiles(nameUris.Value, "*", opt).Select(f => new KeyValuePair<string?, string>(null , f));
        }

        if (File.Exists(nameUris.Value))
        {
            return [nameUris];
        }

        // Path does not exist
        return [];
    }

    private static async Task GetMedias(IEnumerable<KeyValuePair<string?, string>> nameUris, HashSet<string> medias, ICollection<Media> newMedias, IDictionary<string, Tag> tags, ICollection<Tag> newTags)
    {
        IEnumerable<KeyValuePair<string?, string>> mediaNameUris = [];
        foreach (var nameUri in nameUris)
        {
            if (nameUri.Value.IsWebsite())
            {
                mediaNameUris = mediaNameUris.Append(nameUri);
            }
            else
            {
                mediaNameUris = mediaNameUris.UnionBy(GetFiles(nameUri), k => k.Value);
            }
        }

        mediaNameUris = mediaNameUris.Where(p => !medias.Contains(p.Value)).ToList();

        foreach (var mediaNameUri in mediaNameUris)
        {
            if (mediaNameUri.Value.IsWebsite())
            {
                await GetWebPage(mediaNameUri, newMedias, tags, newTags);
            }
            else
            {
                await GetFile(mediaNameUri, newMedias, tags, newTags);
            }
        }
    }

    private static async Task GetBookmarks(BookmarkFolder bookmarkFolder, Tag? parentTag, HashSet<string> medias, ICollection<Media> newMedias, IDictionary<string, Tag> tags, ICollection<Tag> newTags, int depth)
    {
        if (depth > 1)
        {
            if (!tags.TryGetValue(bookmarkFolder.Title, out Tag? tag))
            {
                tag = new Tag
                {
                    Name = bookmarkFolder.Title,
                    Flags = TagFlags.UserTag,
                };

                if (parentTag != null)
                {
                    tag.Parents.Add(parentTag);
                    tag.FirstParentReferenceName = parentTag.GetReferenceName();
                }

                tags.Add(bookmarkFolder.Title, tag);
                newTags.Add(tag);
            }

            parentTag = tag;
        }

        foreach (var bookmarkItem in bookmarkFolder)
        {
            switch (bookmarkItem)
            {
                case BookmarkFolder newBookmarkFolder:
                    await GetBookmarks(newBookmarkFolder, parentTag, medias, newMedias, tags, newTags, depth != -1 ? depth + 1 : -1);
                    break;

                case BookmarkLink bookmarkLink when !medias.Contains(bookmarkLink.Url) && !newMedias.Select(m => m.Uri).Contains(bookmarkLink.Url):
                    await GetBookmark(bookmarkLink, parentTag, newMedias, tags, newTags);
                    break;
            }
        }
    }

    private static async Task GetWebPage(KeyValuePair<string?, string> nameUri, ICollection<Media> newMedias, IDictionary<string, Tag> tags, ICollection<Tag> newTags)
    {
        var title = nameUri.Key;
        if (title == null)
        {
            var webGet = new HtmlWeb();
            var document = await webGet.LoadFromWebAsync(nameUri.Value);
            title = document.DocumentNode.SelectSingleNode("html/head/title").InnerText;
        }

        Media media = new()
        {
            Name = title ?? "",
            Uri = nameUri.Value,
        };

        (var isNew, Tag? tag) = await GetWebsiteTag(nameUri.Value, tags);
        if (tag != null)
        {
            if (isNew)
            {
                newTags.Add(tag);
            }
            media.Tags.Add(tag);
        }

        newMedias.Add(media);
    }

    private static async Task GetFile(KeyValuePair<string?, string> nameUri, ICollection<Media> newMedias, IDictionary<string, Tag> tags, ICollection<Tag> newTags)
    {
        Media media = new()
        {
            Name = nameUri.Key ?? Path.GetFileNameWithoutExtension(nameUri.Value),
            Uri = nameUri.Value,
        };

        (var isNew, Tag? tag) = await GetFileTag(nameUri.Value, tags);
        if (tag != null)
        {
            if (isNew)
            {
                newTags.Add(tag);
            }
            media.Tags.Add(tag);
        }
        newMedias.Add(media);
    }

    private static async Task GetBookmark(BookmarkLink bookBookmarkLink, Tag? parentTag, ICollection<Media> newMedias, IDictionary<string, Tag> tags, ICollection<Tag> newTags)
    {
        Media media = new()
        {
            Name = bookBookmarkLink.Title,
            Notes = bookBookmarkLink.Description ?? "",
            Uri = bookBookmarkLink.Url,
        };

        (var isNew, Tag? tag) = await GetWebsiteTag(bookBookmarkLink.Url, tags);
        if (tag != null)
        {
            if (isNew)
            {
                newTags.Add(tag);
            }
            media.Tags.Add(tag);
        }

        if (parentTag != null) 
        {
            media.Tags.Add(parentTag);
        }

        newMedias.Add(media);
    }

    public static async Task<(bool, Tag?)> GetWebsiteTag(string url, IDictionary<string, Tag>? tags = null, MediaDbContext? database = null)
    {
        if (tags == null)
        {
            if (database == null)
            {
                return (false, null);
            }
            tags = await database.Tags.GroupBy(t => t.Name).Select(g => g.First()).ToDictionaryAsync(t => t.Name);
        }

        if (Uri.IsWellFormedUriString(url, UriKind.Absolute))
        {
            var domain = new Uri(url).Host;

            if (!tags.TryGetValue(domain, out Tag? tag))
            {
                tag = new Tag
                {
                    Name = domain,
                    Flags = TagFlags.Website,
                    Permissions = TagPermissions.CannotChangeName | TagPermissions.CannotDelete
                };
                if (MediaDbContext.WebsiteTag != null)
                {
                    tag.Parents.Add(MediaDbContext.WebsiteTag);
                    tag.FirstParentReferenceName = MediaDbContext.WebsiteTag.GetReferenceName();
                }

                tags.Add(domain, tag);
                return (true, tag);
            }

            return (false, tag);
        }
        return (false, null);
    }

    public static async Task<(bool, Tag?)> GetFileTag(string path, IDictionary<string, Tag>? tags = null, MediaDbContext? database = null)
    {
        if (tags == null)
        {
            if (database == null)
            {
                return (false, null);
            }
            tags = await database.Tags.GroupBy(t => t.Name).Select(g => g.First()).ToDictionaryAsync(t => t.Name);
        }

        var extension = Path.GetExtension(path).ToLower();
        if (!tags.TryGetValue(extension, out Tag? tag))
        {
            tag = new Tag
            {
                Name = extension,
                Flags = TagFlags.Extension,
                Permissions = TagPermissions.CannotChangeName | TagPermissions.CannotDelete
            };

            if (MediaDbContext.FileTag != null)
            {
                tag.Parents.Add(MediaDbContext.FileTag);
                tag.FirstParentReferenceName = MediaDbContext.FileTag.GetReferenceName();
            }
            tags.Add(extension, tag);
            return (true, tag);
        }
        return (false, tag);
    }
}
