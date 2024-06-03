using System.Diagnostics;
using EFCore.BulkExtensions;
using MediaMaster.DataBase.Models;

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
                IDictionary<string, Tag> tags = dataBase.Tags.ToDictionary(t => t.Name);
                ICollection<Tag> newTags = [];
                ICollection<Media> medias = mediaPaths.Select(mediaPath => GetMedia(mediaPath, ref tags, ref newTags)).ToList();

                await dataBase.BulkInsertAsync(medias, new BulkConfig
                {
                    SetOutputIdentity = true,
                    PropertiesToIncludeOnUpdate = [string.Empty], // do nothing if exists
                    UpdateByProperties = [nameof(Media.FilePath)], // conflict target
                });
                await dataBase.BulkInsertAsync(newTags, new BulkConfig { SetOutputIdentity = true });

                Debug.WriteLine($"Media: {medias.Count}");
                Debug.WriteLine($"Tags: {newTags.Count}");

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

    private static Media GetMedia(string path,  ref IDictionary<string, Tag> tags, ref ICollection<Tag> newTags)
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

        return media;
    }
}
