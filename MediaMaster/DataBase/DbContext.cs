using System.Text.Json.Nodes;
using Windows.Foundation;
using Windows.Storage;
using CommunityToolkit.WinUI;
using EFCore.BulkExtensions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using WinUI3Localizer;
using WinUICommunity;
using DocumentFormat.OpenXml.InkML;
using MediaMaster.Services;

namespace MediaMaster.DataBase;

[Flags]
public enum MediaChangeFlags
{
    MediaAdded = 0x00001,
    MediaRemoved = 0x00002,
    MediaChanged = 0x00004,

    NameChanged = 0x00010,
    UriChanged = 0x00100,
    NotesChanged = 0x01000,
    TagsChanged = 0x10000
}

public struct MediaChangeArgs(
    MediaChangeFlags flags,
    ICollection<Media> media,
    ICollection<Tag>? tagsAdded = null,
    ICollection<Tag>? tagsRemoved = null)
{
    public readonly MediaChangeFlags Flags = flags;
    public readonly HashSet<int> MediaIds = media.Select(m => m.MediaId).ToHashSet();
    public readonly ICollection<Media> Medias = media;
    public readonly ICollection<Tag>? TagsAdded = tagsAdded;
    public readonly ICollection<Tag>? TagsRemoved = tagsRemoved;
}

public sealed partial class MediaDbContext : DbContext
{
    public static Tag? FileTag;
    public static Tag? WebsiteTag;
    public static Tag? FavoriteTag;
    public static Tag? ArchivedTag;


    public static readonly string DbPath = Path.Combine(ApplicationData.Current.LocalFolder.Path, "MediaMaster.db");

    // Used for migrations
    //public const string DbPath = @"C:\Users\Antony\AppData\Local\Packages\AntonyKonstantas.MediaMasterApp_ryx18h009e2z4\LocalState\MediaMaster.db";

    public DbSet<Media> Medias { get; init; }
    public DbSet<Tag> Tags { get; init; }

    public DbSet<MediaTag> MediaTags { get; init; }
    public DbSet<TagTag> TagTags { get; init; }

    public static event TypedEventHandler<object?, MediaChangeArgs>? MediasChanged;

    protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
    {
        optionsBuilder.UseSqlite($"Data Source={DbPath}");
#if DEBUG
        optionsBuilder.EnableDetailedErrors();
        optionsBuilder.EnableSensitiveDataLogging();
        optionsBuilder.LogTo(message => Debug.WriteLine(message), LogLevel.Information);
#endif

        optionsBuilder.UseQueryTrackingBehavior(QueryTrackingBehavior.NoTracking);
    }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Media>()
            .HasMany(e => e.Tags)
            .WithMany(e => e.Medias)
            .UsingEntity<MediaTag>();

        modelBuilder.Entity<Tag>()
            .HasMany(e => e.Children)
            .WithMany(e => e.Parents)
            .UsingEntity<TagTag>(
                t => t
                    .HasOne<Tag>()
                    .WithMany()
                    .HasForeignKey(e => e.ChildrenTagId),
                t => t
                    .HasOne<Tag>()
                    .WithMany()
                    .HasForeignKey(e => e.ParentsTagId),
                j => { j.HasKey(t => new { t.ChildrenTagId, t.ParentsTagId }); });
    }

    public async Task InitializeAsync()
    {
#if DEBUG
        await Database.EnsureDeletedAsync();
        await Database.EnsureCreatedAsync();
#else
        await Database.MigrateAsync();
#endif
        Dictionary<string, Tag> tags = await Tags.GroupBy(t => t.Name).Select(g => g.First()).ToDictionaryAsync(x => x.Name);
        await Transaction.Try(this, async () =>
        {
            await SetupTags(tags);
            await SetupCategories(tags);
        });
    }

    private async Task SetupTags(Dictionary<string, Tag> tags)
    {
        if (!tags.TryGetValue("File", out var fileTag))
        {
            fileTag = new Tag
            {
                Name = "File",
                Shorthand = "file",
                Permissions = TagPermissions.CannotChangeParents | TagPermissions.CannotDelete
            };
            await Tags.AddAsync(fileTag);
        }

        FileTag = fileTag;

        if (!tags.TryGetValue("Website", out var websiteTag))
        {
            websiteTag = new Tag
            {
                Name = "Website",
                Shorthand = "web",
                Permissions = TagPermissions.CannotChangeParents | TagPermissions.CannotDelete
            };
            await Tags.AddAsync(websiteTag);
        }

        WebsiteTag = websiteTag;

        if (!tags.TryGetValue("Favorite", out var favoriteTag))
        {
            favoriteTag = new Tag
            {
                Name = "Favorite",
                Permissions = TagPermissions.CannotChangeParents | TagPermissions.CannotDelete |
                              TagPermissions.CannotChangeColor,
                Argb = -204544
            };
            await Tags.AddAsync(favoriteTag);
        }

        FavoriteTag = favoriteTag;

        if (!tags.TryGetValue("Archived", out var archivedTag))
        {
            archivedTag = new Tag
            {
                Name = "Archived",
                Permissions = TagPermissions.CannotChangeParents | TagPermissions.CannotDelete |
                              TagPermissions.CannotChangeColor,
                Argb = -3921124
            };
            await Tags.AddAsync(archivedTag);
        }

        ArchivedTag = archivedTag;

        await SaveChangesAsync();
    }

    private async Task SetupCategories(Dictionary<string, Tag> tags)
    {
        var defaultCategoriesFilePath = Path.Combine(AppContext.BaseDirectory, "Data", "MediaCategories.json");

        await using FileStream fileStream = File.OpenRead(defaultCategoriesFilePath);
        JsonNode? defaultCategories = await JsonNode.ParseAsync(fileStream);

        ICollection<Tag> newTags = [];

        if (defaultCategories is JsonObject defaultCategoriesObject && FileTag != null)
        {
            foreach ((var key, JsonNode? value) in defaultCategoriesObject)
            {
                if (value is not JsonArray array) continue;

                if (!tags.TryGetValue(key, out var category))
                {
                    category = new Tag
                    {
                        Name = key,
                        Flags = TagFlags.Extension,
                        Permissions = TagPermissions.CannotChangeParents,
                        FirstParentReferenceName = FileTag.ReferenceName
                    };
                    category.Parents.Add(FileTag);
                    newTags.Add(category);
                }

                foreach (JsonNode? extension in array)
                {
                    var name = extension?.ToString();
                    if (name != null && !tags.TryGetValue(name, out var tag))
                    {
                        tag = new Tag
                        {
                            Name = name,
                            Flags = TagFlags.Extension,
                            Permissions = TagPermissions.CannotChangeName | TagPermissions.CannotDelete,
                            FirstParentReferenceName = category.ReferenceName
                        };
                        tag.Parents.Add(category);
                        newTags.Add(tag);
                    }
                }
            }
        }

        await this.BulkInsertAsync(newTags, new BulkConfig { SetOutputIdentity = true });

        List<TagTag> tagTags = newTags.SelectMany(tag => tag.Parents.Select(parent => new TagTag
        {
            ParentsTagId = parent.TagId,
            ChildrenTagId = tag.TagId
        })).ToList();

        await this.BulkInsertAsync(tagTags);
    }

    /// <summary>
    ///     Invokes the media change event.
    /// </summary>
    /// <param name="sender">The event sender.</param>
    /// <param name="flags">The flags indicating the type of media change.</param>
    /// <param name="media">The collection of media affected by the change.</param>
    /// <param name="tagsAdded">The collection of tags added during the change.</param>
    /// <param name="tagsRemoved">The collection of tags removed during the change.</param>
    public static void InvokeMediaChange(object? sender, MediaChangeFlags flags, ICollection<Media> media,
        ICollection<Tag>? tagsAdded = null, ICollection<Tag>? tagsRemoved = null)
    {
        var args = new MediaChangeArgs(flags, media, tagsAdded, tagsRemoved);
        App.DispatcherQueue.EnqueueAsync(() => MediasChanged?.Invoke(sender, args));

        if (flags.HasFlag(MediaChangeFlags.MediaAdded) || flags.HasFlag(MediaChangeFlags.MediaRemoved))
        {
            var growlInfo = new GrowlInfo
            {
                ShowDateTime = true,
                IsClosable = true,
                Title = string.Format("InAppNotification_Title".GetLocalizedString(), DateTimeOffset.Now),
                UseBlueColorForInfo = true
            };

            if (flags.HasFlag(MediaChangeFlags.MediaAdded))
            {
                growlInfo.Message = string.Format("InAppNotification_MediasAdded".GetLocalizedString(), media.Count);
            }
            else if (flags.HasFlag(MediaChangeFlags.MediaRemoved))
            {
                growlInfo.Message = string.Format("InAppNotification_MediasRemoved".GetLocalizedString(), media.Count);
            }


            App.DispatcherQueue.EnqueueAsync(() => { Growl.Info(growlInfo); });
        }
    }
}
