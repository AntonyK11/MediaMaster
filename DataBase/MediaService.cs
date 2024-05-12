using System.Diagnostics;
using EFCore.BulkExtensions;
using MediaMaster.DataBase.Models;
using Microsoft.EntityFrameworkCore;

namespace MediaMaster.DataBase;
using System.Collections.Generic;

public class MediaService
{
    public static async Task AddMediaAsync(string path)
    {
        IEnumerable<string> mediaPaths = GetFiles(path.Trim());

        await using (MediaDbContext dataBase = new())
        {
            ICollection<Tag> tags = dataBase.Tags.ToList();
            ICollection<Tag> newTags = [];
            ICollection<Media> medias = mediaPaths.Select(path => GetMedia(path, ref tags, ref newTags)).ToList();

            await dataBase.BulkInsertAsync(medias, new BulkConfig { SetOutputIdentity = true });
            await dataBase.BulkInsertAsync(newTags, new BulkConfig { SetOutputIdentity = true });

            ICollection<MediaTag> mediaTags = [];
            foreach (var media in medias)
            {
                foreach (var tag in media.Tags)
                {
                    mediaTags.Add(new MediaTag
                    {
                        MediaId = media.MediaId,
                        TagId = tag.TagId
                    });
                }
            }

            await dataBase.BulkInsertAsync(mediaTags);
        }
    }

    private static IEnumerable<string> GetFiles(string path)
    {
        if (Directory.Exists(path))
        {
            EnumerationOptions opt = new()
            {
                RecurseSubdirectories = true,
                IgnoreInaccessible = true
            };
            return Directory.EnumerateFileSystemEntries(path, "*.*", opt);
        }

        if (File.Exists(path))
        {
            return [path];
        }

        // Path does not exist
        return [];
    }
    
    private static Media GetMedia(string path, ref ICollection<Tag> tags, ref ICollection<Tag> newTags)
    {
        var extension = Path.GetExtension(path);
        Media media = new()
        {
            Name = Path.GetFileNameWithoutExtension(path),
            FilePath = path,
        };

        Tag? tag = tags.FirstOrDefault(t => t.Name == extension);
        if (tag is null)
        {
            tag = new Tag
            {
                Name = extension
            };
            tags.Add(tag);
            newTags.Add(tag);
        }
        media.Tags.Add(tag);

        return media;
    }
}
