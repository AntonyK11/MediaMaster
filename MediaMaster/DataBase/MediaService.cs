using System.Collections.Frozen;
using System.Collections.Immutable;
using System.Diagnostics;
using EFCore.BulkExtensions;
using MediaMaster.DataBase.Models;
using Microsoft.EntityFrameworkCore;

namespace MediaMaster.DataBase;

using System.Collections.Generic;


public class MediaService
{
    private static bool _isRunning;
    public static async Task<bool> AddMediaAsync(string path)
    {
        if (_isRunning)
        {
            return false;
        }

        _isRunning = true;
        try
        {
            IEnumerable<string> mediaPaths = GetFiles(path.Trim());

            await using (MediaDbContext dataBase = new())
            {
                IDictionary<string, Tag> tags = await dataBase.Tags.AsNoTracking().ToDictionaryAsync(t => t.Name);
                IDictionary<string, int> medias = await dataBase.Medias.AsNoTracking().ToDictionaryAsync(m => m.FilePath, m => m.MediaId);

                ICollection<Tag> newTags = [];
                ICollection<Media> newMedias = [];

                mediaPaths = mediaPaths.Where(p => !medias.ContainsKey(p)).ToList();

                foreach (var mediaPath in mediaPaths)
                {
                    GetMedia(mediaPath, ref medias, ref newMedias, ref tags, ref newTags);
                }

                await dataBase.BulkInsertAsync(newMedias, new BulkConfig { SetOutputIdentity = true });
                await dataBase.BulkInsertAsync(newTags, new BulkConfig { SetOutputIdentity = true });
                
                var mediaTags = newMedias.SelectMany(media => media.Tags.Select(tag => new MediaTag
                {
                    MediaId = media.MediaId,
                    TagId = tag.TagId
                })).ToList();

                await dataBase.BulkInsertAsync(mediaTags);

                Debug.WriteLine($"Media: {newMedias.Count}");
                Debug.WriteLine($"Tags: {newTags.Count}");
                Debug.WriteLine($"MediaTags: {mediaTags.Count}");
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

    private static IEnumerable<string> GetFiles(string path)
    {
        if (Directory.Exists(path))
        {
            EnumerationOptions opt = new()
            {
                RecurseSubdirectories = true,
                IgnoreInaccessible = true,
                ReturnSpecialDirectories = true
            };
            return Directory.EnumerateFiles(path, "*", opt);
        }

        if (File.Exists(path))
        {
            return [path];
        }

        // Path does not exist
        return [];
    }

    private static void GetMedia(string path, ref IDictionary<string, int> medias, ref ICollection<Media> newMedia,  ref IDictionary<string, Tag> tags, ref ICollection<Tag> newTags)
    {
        var extension = Path.GetExtension(path);
        Media media = new()
        {
            Name = Path.GetFileNameWithoutExtension(path),
            FilePath = path,
        };

        if (!tags.TryGetValue(extension, out Tag? tag))
        {
            tag = new Tag
            {
                Name = extension
            };
            tags.Add(extension, tag);
            newTags.Add(tag);
        }
        media.Tags.Add(tag);
        newMedia.Add(media);
    }
}
