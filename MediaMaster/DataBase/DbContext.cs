using System.Collections;
using System.Diagnostics;
using Microsoft.EntityFrameworkCore;
using MediaMaster.DataBase.Models;
using Windows.Storage;
using Microsoft.Extensions.Logging;

namespace MediaMaster.DataBase;

public class MediaDbContext : DbContext
{
    public DbSet<Media> Medias { get; init; }
    public DbSet<Tag> Tags { get; init; }

    public DbSet<MediaTag> MediaTags { get; init; }
    public DbSet<TagTag> TagTags { get; init; }

    public static Tag? FileTag;
    public static Tag? WebsiteTag;
    public static Tag? FavoriteTag;
    public static Tag? ArchivedTag;

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

        //optionsBuilder.UseQueryTrackingBehavior(QueryTrackingBehavior.NoTracking);
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

        ICollection<Tag> tags = Tags.AsNoTracking().ToList();

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

        //for (var i = 0; i < 10; i++)
        //{
        //    Tag media = this.CreateProxy<Tag>(t =>
        //    {
        //        t.Name = "Media";
        //        foreach (Tag tag in Tags)
        //        {
        //            t.Children.Add(tag);
        //        }
        //    });
        //    Tags.Add(media);
        //    await SaveChangesAsync();
        //}
    }

    //public void AddTag(string name, IEnumerable<Tag> parents)
    //{
    //    Tag tag = this.CreateProxy<Tag>(t =>
    //    {
    //        t.Name = name;
    //        foreach (Tag tag in parents)
    //        {
    //            t.Parents.Add(tag);
    //        }
    //    });
    //    Tags.Add(tag);
    //}

    //public void AddMedia(string name, string path, IEnumerable<Tag> tags)
    //{
    //    Media media = this.CreateProxy<Media>(m =>
    //    {
    //        m.Name = name;
    //        m.FilePath = path;
    //        foreach (Tag tag in tags)
    //        {
    //            m.Tags.Add(tag);
    //        }
    //    });
    //    Medias.Add(media);
    //}
}