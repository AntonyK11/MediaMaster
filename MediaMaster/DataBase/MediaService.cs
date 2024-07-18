using EFCore.BulkExtensions;
using MediaMaster.DataBase.Models;
using MediaMaster.Extensions;
using Microsoft.EntityFrameworkCore;

namespace MediaMaster.DataBase;

using System.Collections.Generic;
using System.Linq;
using HtmlAgilityPack;


public class MediaService
{
    private static bool _isRunning;

    public static Task<bool> AddMediaAsync(IEnumerable<string> uris)
    {
        return AddMediaAsync(uris.Select(uri => new KeyValuePair<string?, string>(null, uri.Trim())));
    }


    public static async Task<bool> AddMediaAsync(IEnumerable<KeyValuePair<string?, string>> nameUris)
    {
        if (_isRunning)
        {
            return false;
        }

        _isRunning = true;
        try
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
                    mediaNameUris = mediaNameUris.Union(GetFiles(nameUri));
                }
            }

            await using (MediaDbContext dataBase = new())
            {
                IDictionary<string, Tag> tags = await dataBase.Tags.AsNoTracking().Select(t => new Tag{ TagId = t.TagId, Name = t.Name }).GroupBy(t => t.Name).Select(g => g.First()).ToDictionaryAsync(t => t.Name);
                IDictionary<string, int> medias = await dataBase.Medias.AsNoTracking().Select(m => new { m.MediaId, FilePath = m.Uri }).ToDictionaryAsync(m => m.FilePath, m => m.MediaId);

                ICollection<Tag> newTags = [];
                ICollection<Media> newMedias = [];

                mediaNameUris = mediaNameUris.Where(p => !medias.ContainsKey(p.Value)).ToList();

                foreach (var mediaNameUri in mediaNameUris)
                {
                    await GetMedia(mediaNameUri, newMedias, tags, newTags);
                }

                await dataBase.BulkInsertAsync(newMedias, new BulkConfig { SetOutputIdentity = true });
                await dataBase.BulkInsertAsync(newTags, new BulkConfig { SetOutputIdentity = true });
                
                var mediaTags = newMedias.SelectMany(media => media.Tags.Select(tag => new MediaTag
                {
                    MediaId = media.MediaId,
                    TagId = tag.TagId
                })).ToList();

                await dataBase.BulkInsertAsync(mediaTags);

                var tagTags = newTags.SelectMany(tag => tag.Parents.Select(parent => new TagTag
                {
                    ParentsTagId = parent.TagId,
                    ChildrenTagId = tag.TagId
                })).ToList();

                await dataBase.BulkInsertAsync(tagTags);

                Debug.WriteLine($"Media: {newMedias.Count}");
                Debug.WriteLine($"Tags: {newTags.Count}");
                Debug.WriteLine($"MediaTags: {mediaTags.Count}");
                Debug.WriteLine($"MediaTags: {tagTags.Count}");
            }
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

    private static async Task GetMedia(KeyValuePair<string?, string> nameUri, ICollection<Media> newMedia,  IDictionary<string, Tag> tags, ICollection<Tag> newTags)
    {
        if (nameUri.Value.IsWebsite())
        {
            await GetWebPage(nameUri, newMedia, tags, newTags);
        }
        else
        {
            GetFile(nameUri, newMedia, tags, newTags);
        }
    }

    private static async Task GetWebPage(KeyValuePair<string?, string> nameUri, ICollection<Media> newMedia, IDictionary<string, Tag> tags, ICollection<Tag> newTags)
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
        if (Uri.IsWellFormedUriString(nameUri.Value, UriKind.Absolute))
        {
            var domain = new Uri(nameUri.Value).Host;

            if (!tags.TryGetValue(domain, out Tag? tag))
            {
                tag = new Tag
                {
                    Name = domain,
                    Flags = TagFlags.Website,
                    Permissions = TagPermissions.CannotChangeName | TagPermissions.CannotChangeParents | TagPermissions.CannotDelete
                };
                if (MediaDbContext.WebsiteTag != null)
                {
                    tag.Parents.Add(MediaDbContext.WebsiteTag);
                    tag.FirstParentReferenceName = MediaDbContext.WebsiteTag.GetReferenceName();
                }

                tags.Add(domain, tag);
                newTags.Add(tag);
            }

            media.Tags.Add(tag);
        }

        newMedia.Add(media);
    }

    private static void GetFile(KeyValuePair<string?, string> nameUri, ICollection<Media> newMedia, IDictionary<string, Tag> tags, ICollection<Tag> newTags)
    {
        var extension = Path.GetExtension(nameUri.Value);
        Media media = new()
        {
            Name = nameUri.Key ?? Path.GetFileNameWithoutExtension(nameUri.Value),
            Uri = nameUri.Value,
        };

        if (!tags.TryGetValue(extension, out Tag? tag))
        {
            tag = new Tag
            {
                Name = extension,
                Flags = TagFlags.Extension,
                Permissions = TagPermissions.CannotChangeName | TagPermissions.CannotChangeParents | TagPermissions.CannotDelete
            };
            if (MediaDbContext.FileTag != null)
            {
                tag.Parents.Add(MediaDbContext.FileTag);
                tag.FirstParentReferenceName = MediaDbContext.FileTag.GetReferenceName();
            }
            tags.Add(extension, tag);
            newTags.Add(tag);
        }
        media.Tags.Add(tag);
        newMedia.Add(media);
    }
}
