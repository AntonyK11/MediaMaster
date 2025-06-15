using DevWinUI;
using EFCore.BulkExtensions;
using MediaMaster.Extensions;
using MediaMaster.Interfaces.Services;
using MediaMaster.Services;
using Microsoft.Windows.AppNotifications;
using WinUI3Localizer;

namespace MediaMaster.DataBase;

public static class MediaService
{
    public const int BatchSize = 250000;
    public static bool IsRunning { get; set; }
    public static readonly Stopwatch Watch = new();

    public static async Task DeleteMedias(object sender, ICollection<Media> medias)
    {
        if (App.MainWindow == null) return;

        var dialog = new ContentDialog
        {
            XamlRoot = App.MainWindow.Content.XamlRoot,
            DefaultButton = ContentDialogButton.Close,
            RequestedTheme = App.GetService<IThemeSelectorService>().ActualTheme
        };
        Uids.SetUid(dialog, "/Media/DeleteDialog");
        
        App.GetService<IThemeSelectorService>().ThemeChanged += (_, theme) => dialog.RequestedTheme = theme;
        
        ContentDialogResult result = await dialog.ShowAndEnqueueAsync();

        if (result == ContentDialogResult.Primary)
        {
            await using (var database = new MediaDbContext())
            {
                var transactionSuccessful = await Transaction.Try(database, () => database.BulkDeleteAsync(medias));

                if (transactionSuccessful)
                {
                    MediaDbContext.InvokeMediaChange(sender, MediaChangeFlags.MediaRemoved, medias);
                }
            }
        }
    }

    public static void CannotAddMedias()
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
                                      <action activationType="system" arguments="dismiss" content="{"Dismiss_ToastButton".GetLocalizedString()}"/>
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

    public static void MediasAdded(int mediaAddedCount)
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
                                      <action content='{"View_ToastButton".GetLocalizedString()}' arguments='action=view'/>
                                      <action activationType="system" arguments="dismiss" content="{"Dismiss_ToastButton".GetLocalizedString()}"/>
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

    public static async Task ProcessBatch(
        List<Media> mediaBatch,
        List<Tag> tagBatch,
        HashSet<int>? userTagsId,
        MediaDbContext database)
    {
        await database.BulkInsertAsync(mediaBatch, new BulkConfig { SetOutputIdentity = true });
        await database.BulkInsertAsync(tagBatch, new BulkConfig { SetOutputIdentity = true });
        
        await AddNewMediaTags(mediaBatch, database);
        await AddNewTagTags(tagBatch, database);
        
        if (userTagsId != null)
        {
            var mediaTags = mediaBatch
                .SelectMany(m => userTagsId.Select(tagId => new MediaTag
                {
                    MediaId = m.MediaId,
                    TagId = tagId
                })).ToList();
            await database.BulkInsertOrUpdateAsync(mediaTags);
        }
        
        mediaBatch.Clear();
        tagBatch.Clear();
    }
    
    public static async Task AddNewMediaTags(ICollection<Media> newMedias, MediaDbContext database)
    {
        var mediaTags = newMedias
            .SelectMany(media => media.Tags.Select(tag => new MediaTag
            {
                MediaId = media.MediaId,
                TagId = tag.TagId
            })).ToList();
        await database.BulkInsertOrUpdateAsync(mediaTags);
    }

    public static async Task AddNewTagTags(ICollection<Tag> newTags, MediaDbContext database)
    {
        var tagTags = newTags
            .SelectMany(tag => tag.Parents.Select(parent => new TagTag
            {
                ParentsTagId = parent.TagId,
                ChildrenTagId = tag.TagId
            })).ToList();
        await database.BulkInsertOrUpdateAsync(tagTags);
    }

    public static (bool, Tag?) GetWebsiteTag(string url, IDictionary<string, Tag> tags)
    {
        if (!Uri.IsWellFormedUriString(url, UriKind.Absolute)) return (false, null);
        
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
    
    public static (bool, Tag?) GetFileTag(string path, IDictionary<string, Tag> tags)
    {
        var extension = Path.GetExtension(path).ToLowerInvariant();
        if (!tags.TryGetValue(extension, out var tag))
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