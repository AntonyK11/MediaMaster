using BookmarksManager;
using CommunityToolkit.WinUI;
using EFCore.BulkExtensions;
using HtmlAgilityPack;
using MediaMaster.Extensions;
using MediaMaster.Services;
using MediaMaster.Views.Dialog;
using Microsoft.EntityFrameworkCore;
using Microsoft.Windows.AppNotifications;
using WinUI3Localizer;
using WinUICommunity;

namespace MediaMaster.DataBase;

public class MediaService
{
    public bool IsRunning;
    private readonly Stopwatch _watch = new();

    public Task<int> AddMediaAsync(IEnumerable<string> uris, HashSet<int>? userTagsId = null, string userNotes = "")
    {
        return AddMediaAsync(uris.Select(uri => new KeyValuePair<string?, string>(null, uri.Trim())), userTagsId: userTagsId, userNotes: userNotes);
    }


    public async Task<int> AddMediaAsync(IEnumerable<KeyValuePair<string?, string>>? nameUris = null,
        ICollection<BrowserFolder>? browserFolders = null, HashSet<int>? userTagsId = null, string userNotes = "", bool generateBookmarkTags = true)
    {
        if (IsRunning)
        {
            await App.DispatcherQueue.EnqueueAsync(CannotAddMedias);
            return 0;
        }

        IsRunning = true;
        int mediaAddedCount;
        App.GetService<TasksService>().AddGlobalTak();
        //await App.DispatcherQueue.EnqueueAsync(AddingMedias);
        try
        {
            await using (MediaDbContext database = new())
            {
                _watch.Restart();

                Dictionary<string, Tag> tags = await database.Tags
                    .Select(t => new Tag { TagId = t.TagId, Name = t.Name })
                    .GroupBy(t => t.Name).Select(g => g.First()).ToDictionaryAsync(t => t.Name);
                HashSet<string> medias = database.Medias.Select(m => m.Uri).ToHashSet();

                ICollection<Tag> newTags = [];
                ICollection<Media> newMedias = [];

                if (nameUris != null)
                {
                    await GetMedias(nameUris, medias, newMedias, tags, newTags, userNotes);
                }

                if (browserFolders != null)
                {
                    foreach (BrowserFolder browserFolder in browserFolders)
                    {
                        await GetBookmarks(browserFolder.BookmarkFolder, null, medias, newMedias, tags, newTags,
                            generateBookmarkTags ? 0 : -1);
                    }
                }

                await database.BulkInsertAsync(newMedias, new BulkConfig { SetOutputIdentity = true });
                await database.BulkInsertAsync(newTags, new BulkConfig { SetOutputIdentity = true });

                ICollection<MediaTag> mediaTags = await AddNewMedias(newMedias, database);
                ICollection<TagTag> tagTags = await AddNewTags(newTags, database);

                mediaAddedCount = newMedias.Count;
                Debug.WriteLine($"Media: {mediaAddedCount}");
                Debug.WriteLine($"Tags: {newTags.Count}");
                Debug.WriteLine($"MediaTags: {mediaTags.Count}");

                if (userTagsId != null)
                {
                    var mediaUserTags = await AddUserTagsToMedias(newMedias, userTagsId, database);
                    Debug.WriteLine($"mediaUserTags: {mediaUserTags.Count}");
                    mediaUserTags.Clear();
                }

                Debug.WriteLine($"tagTags: {tagTags.Count}");
                Debug.WriteLine($"Added in {_watch.Elapsed}");
                _watch.Stop();

                await App.DispatcherQueue.EnqueueAsync(() => MediasAdded(mediaAddedCount));
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
            return -1;
        }
        finally
        {
            IsRunning = false;
            App.GetService<TasksService>().RemoveGlobalTak();
        }

        return mediaAddedCount;
    }

    private static void CannotAddMedias()
    {
        if (App.MainWindow?.Visible == true)

        {
            Growl.Error(new GrowlInfo
            {
                ShowDateTime = true,
                IsClosable = true,
                Title = string.Format("InAppNotification_Title".GetLocalizedString(), DateTimeOffset.Now),
                Message = "InAppNotification_CannotAddMedias".GetLocalizedString()
            });
        }
        else
        {
            var xmlPayload = $"""
                              <toast launch="action=edit">
                                  <visual>
                                      <binding template="ToastGeneric">
                                          <text>{"InAppNotification_CannotAddMedias".GetLocalizedString()}</text>
                                      </binding>
                                  </visual>
                                  <actions>
                                      <action activationType="system" arguments="dismiss" content="{"dismiss_toast_button".GetLocalizedString()}"/>
                                  </actions>
                              </toast>
                              """;

            var notificationManager = AppNotificationManager.Default;
            var toast = new AppNotification(xmlPayload)
            {
                ExpiresOnReboot = true
            };

            notificationManager.Show(toast);
        }
    }

    private static void MediasAdded(int mediaAddedCount)
    {
        if (App.MainWindow?.Visible == false)
        {
            var xmlPayload = $"""
                              <toast launch="action=edit">
                                  <visual>
                                      <binding template="ToastGeneric">
                                          <text>{string.Format("medias_added_toast_text".GetLocalizedString(), mediaAddedCount)}</text>
                                      </binding>
                                  </visual>
                                  <actions>
                                      <action content='{"view_toast_button".GetLocalizedString()}' arguments='action=view'/>
                                      <action activationType="system" arguments="dismiss" content="{"dismiss_toast_button".GetLocalizedString()}"/>
                                  </actions>
                              </toast>
                              """;

            var notificationManager = AppNotificationManager.Default;
            var toast = new AppNotification(xmlPayload)
            {
                ExpiresOnReboot = true
            };

            notificationManager.Show(toast);
        }
    }

    public static async Task<ICollection<MediaTag>> AddNewMedias(ICollection<Media> newMedias, MediaDbContext database)
    {
        List<MediaTag> mediaTags = newMedias.SelectMany(media => media.Tags.Select(tag => new MediaTag
        {
            MediaId = media.MediaId,
            TagId = tag.TagId
        })).ToList();

        await database.BulkInsertOrUpdateAsync(mediaTags);
        return mediaTags;
    }

    public static async Task<ICollection<TagTag>> AddNewTags(ICollection<Tag> newTags, MediaDbContext database)
    {
        List<TagTag> tagTags = newTags.SelectMany(tag => tag.Parents.Select(parent => new TagTag
        {
            ParentsTagId = parent.TagId,
            ChildrenTagId = tag.TagId
        })).ToList();

        await database.BulkInsertOrUpdateAsync(tagTags);
        return tagTags;
    }

    public static async Task<ICollection<MediaTag>> AddUserTagsToMedias(ICollection<Media> newMedias, HashSet<int> userTagsId, MediaDbContext database)
    {
        List<MediaTag> mediaTags = newMedias.SelectMany(m => userTagsId.Select(tagId => new MediaTag
        {
            MediaId = m.MediaId,
            TagId = tagId
        })).ToList();

        await database.BulkInsertOrUpdateAsync(mediaTags);
        return mediaTags;
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
            return Directory.EnumerateFiles(nameUris.Value, "*", opt)
                .Select(f => new KeyValuePair<string?, string>(null, f));
        }

        if (File.Exists(nameUris.Value))
        {
            return [nameUris];
        }

        // Path does not exist
        return [];
    }

    private static async Task GetMedias(IEnumerable<KeyValuePair<string?, string>> nameUris, HashSet<string> medias,
        ICollection<Media> newMedias, IDictionary<string, Tag> tags, ICollection<Tag> newTags, string userNotes = "")
    {
        IEnumerable<KeyValuePair<string?, string>> mediaNameUris = [];
        foreach (KeyValuePair<string?, string> nameUri in nameUris)
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

        foreach (KeyValuePair<string?, string> mediaNameUri in mediaNameUris)
        {
            if (mediaNameUri.Value.IsWebsite())
            {
                await GetWebPage(mediaNameUri, newMedias, tags, newTags, userNotes);
            }
            else
            {
                await GetFile(mediaNameUri, newMedias, tags, newTags, userNotes);
            }
        }
    }

    private static async Task GetBookmarks(BookmarkFolder bookmarkFolder, Tag? parentTag, HashSet<string> medias,
        ICollection<Media> newMedias, IDictionary<string, Tag> tags, ICollection<Tag> newTags, int depth)
    {
        if (depth > 1)
        {
            if (!tags.TryGetValue(bookmarkFolder.Title, out Tag? tag))
            {
                tag = new Tag
                {
                    Name = bookmarkFolder.Title,
                    Flags = TagFlags.UserTag
                };

                if (parentTag != null)
                {
                    tag.Parents.Add(parentTag);
                    tag.FirstParentReferenceName = parentTag.ReferenceName;
                }

                tags.Add(bookmarkFolder.Title, tag);
                newTags.Add(tag);
            }

            parentTag = tag;
        }

        foreach (IBookmarkItem? bookmarkItem in bookmarkFolder)
        {
            switch (bookmarkItem)
            {
                case BookmarkFolder newBookmarkFolder:
                    await GetBookmarks(newBookmarkFolder, parentTag, medias, newMedias, tags, newTags,
                        depth != -1 ? depth + 1 : -1);
                    break;

                case BookmarkLink bookmarkLink when !medias.Contains(bookmarkLink.Url) &&
                                                    !newMedias.Select(m => m.Uri).Contains(bookmarkLink.Url):
                    await GetBookmark(bookmarkLink, parentTag, newMedias, tags, newTags);
                    break;
            }
        }
    }

    private static async Task GetWebPage(KeyValuePair<string?, string> nameUri, ICollection<Media> newMedias,
        IDictionary<string, Tag> tags, ICollection<Tag> newTags, string userNotes = "")
    {
        var title = nameUri.Key;
        if (title == null)
        {
            var webGet = new HtmlWeb();
            HtmlDocument? document = await webGet.LoadFromWebAsync(nameUri.Value);
            title = document.DocumentNode.SelectSingleNode("html/head/title").InnerText;
        }

        Media media = new()
        {
            Name = title ?? "",
            Notes = userNotes,
            Uri = nameUri.Value
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

    private static async Task GetFile(KeyValuePair<string?, string> nameUri, ICollection<Media> newMedias,
        IDictionary<string, Tag> tags, ICollection<Tag> newTags, string userNotes = "")
    {
        Media media = new()
        {
            Name = nameUri.Key ?? Path.GetFileNameWithoutExtension(nameUri.Value),
            Notes = userNotes,
            Uri = nameUri.Value
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

    private static async Task GetBookmark(BookmarkLink bookBookmarkLink, Tag? parentTag, ICollection<Media> newMedias,
        IDictionary<string, Tag> tags, ICollection<Tag> newTags)
    {
        Media media = new()
        {
            Name = bookBookmarkLink.Title,
            Notes = bookBookmarkLink.Description ?? "",
            Uri = bookBookmarkLink.Url
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

    public static async Task<(bool, Tag?)> GetWebsiteTag(string url, IDictionary<string, Tag>? tags = null,
        MediaDbContext? database = null)
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
                    tag.FirstParentReferenceName = MediaDbContext.WebsiteTag.ReferenceName;
                }

                tags.Add(domain, tag);
                return (true, tag);
            }

            return (false, tag);
        }

        return (false, null);
    }

    public static async Task<(bool, Tag?)> GetFileTag(string path, IDictionary<string, Tag>? tags = null,
        MediaDbContext? database = null)
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
                tag.FirstParentReferenceName = MediaDbContext.FileTag.ReferenceName;
            }

            tags.Add(extension, tag);
            return (true, tag);
        }

        return (false, tag);
    }
}