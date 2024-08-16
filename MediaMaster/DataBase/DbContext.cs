using Windows.Foundation;
using Microsoft.EntityFrameworkCore;
using MediaMaster.DataBase.Models;
using Windows.Storage;
using CommunityToolkit.WinUI;
using CommunityToolkit.WinUI.Behaviors;
using MediaMaster.Services;
using Microsoft.Extensions.Logging;

namespace MediaMaster.DataBase;

[Flags]
public enum MediaChangeFlags
{
    MediaAdded = 0x00001,
    MediaRemoved = 0x00002,
    MediaChanged = 0x00004,
    
    NameChanged = 0x00010,
    UriChanged = 0x00100,
    DescriptionChanged = 0x01000,
    TagsChanged = 0x10000,
}

public struct MediaChangeArgs(MediaChangeFlags flags, ICollection<Media> media, ICollection<Tag>? tagsAdded = null, ICollection<Tag>? tagsRemoved = null)
{
    public MediaChangeFlags Flags = flags;
    public HashSet<int> MediaIds = media.Select(m => m.MediaId).ToHashSet();
    public ICollection<Media> Medias = media;
    public ICollection<Tag>? TagsAdded = tagsAdded;
    public ICollection<Tag>? TagsRemoved = tagsRemoved;
}

public partial class MediaDbContext : DbContext
{
    public static Tag? FileTag;
    public static Tag? WebsiteTag;
    public static Tag? FavoriteTag;
    public static Tag? ArchivedTag;

    public static event TypedEventHandler<object?, MediaChangeArgs>? MediasChanged;

    //public static event TypedEventHandler<object, >? TagAdded;
    //public static event TypedEventHandler<object, >? TagRemoved;
    //public static event TypedEventHandler<object, >? TagChanged;

    public DbSet<Media> Medias { get; init; }
    public DbSet<Tag> Tags { get; init; }

    public DbSet<MediaTag> MediaTags { get; init; }
    public DbSet<TagTag> TagTags { get; init; }


    private static readonly string DbPath = Path.Combine(ApplicationData.Current.LocalFolder.Path, "MediaMaster.db");
    //private const string DbPath = "C:\\Users\\Antony\\AppData\\Local\\Packages\\MediaMaster_dqnfd4b7hk63t\\LocalState\\MediaMaster.db";

    protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
    {
        optionsBuilder.UseSqlite($"Data Source={DbPath}");
        //optionsBuilder.UseChangeTrackingProxies();
        optionsBuilder.EnableDetailedErrors();
        optionsBuilder.EnableSensitiveDataLogging();
        //optionsBuilder.UseLazyLoadingProxies();

        optionsBuilder.LogTo(message => Debug.WriteLine(message), LogLevel.Information);

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
            .WithMany(e=> e.Parents)
            .UsingEntity<TagTag>(
                t => t
                    .HasOne<Tag>()
                    .WithMany()
                    .HasForeignKey(e => e.ChildrenTagId),
                t => t
                    .HasOne<Tag>()
                    .WithMany()
                    .HasForeignKey(e => e.ParentsTagId),
                j =>
                {
                    j.HasKey(t => new { t.ChildrenTagId, t.ParentsTagId });
                });
    }

    public async Task InitializeAsync()
    {
        //await Database.MigrateAsync();
        await Database.EnsureDeletedAsync();
        await Database.EnsureCreatedAsync();
        //await Medias.LoadAsync();
        //await Tags.LoadAsync();

        //ChangeTracker.StateChanged += Timestamps.UpdateTimestamps;
        //ChangeTracker.Tracked += Timestamps.UpdateTimestamps;

        ICollection<Tag> tags = Tags.ToList();

        if (tags.FirstOrDefault(t => t.Name == "File") is not { } fileTag)
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

        if (tags.FirstOrDefault(t => t.Name == "Website") is not { } websiteTag)
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

        if (tags.FirstOrDefault(t => t.Name == "Favorite") is not { } favoriteTag)
        {
            favoriteTag = new Tag
            {
                Name = "Favorite",
                Permissions = TagPermissions.CannotChangeParents | TagPermissions.CannotDelete | TagPermissions.CannotChangeColor,
                Aliases = { "Favorited", "Favorites" },
                Argb = -204544
            };
            await Tags.AddAsync(favoriteTag);
        }
        FavoriteTag = favoriteTag;

        if (tags.FirstOrDefault(t => t.Name == "Archived") is not { } archivedTag)
        {
            archivedTag = new Tag
            {
                Name = "Archived",
                Permissions = TagPermissions.CannotChangeParents | TagPermissions.CannotDelete | TagPermissions.CannotChangeColor,
                Aliases = { "Archive" },
                Argb = -3921124
            };
            await Tags.AddAsync(archivedTag);
        }
        ArchivedTag = archivedTag;

        await SaveChangesAsync();
    }

    public static void InvokeMediaChange(object?  sender, MediaChangeFlags flags, ICollection<Media> media, ICollection<Tag>? tagsAdded = null, ICollection<Tag>? tagsRemoved = null)
    {
        var args = new MediaChangeArgs(flags, media, tagsAdded, tagsRemoved);
        App.DispatcherQueue.EnqueueAsync(() => MediasChanged?.Invoke(sender, args));

        if (flags.HasFlag(MediaChangeFlags.MediaAdded) || flags.HasFlag(MediaChangeFlags.MediaRemoved))
        {
            var notification = new Notification()
            {
                Title = $"Notification {DateTimeOffset.Now}",
                Duration = TimeSpan.FromSeconds(5)
            };

            if (flags.HasFlag(MediaChangeFlags.MediaAdded))
            {
                notification.Message = $"{media.Count} Media(s) Added";
            }
            else if (flags.HasFlag(MediaChangeFlags.MediaRemoved))
            {
                notification.Message = $"{media.Count} Media(s) Removed";
            }

            App.GetService<InAppNotificationService>().SendNotification(notification);
        }
    }
}