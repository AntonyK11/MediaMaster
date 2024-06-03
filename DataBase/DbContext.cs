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

    //private static readonly string DbPath = Path.Combine(ApplicationData.Current.LocalFolder.Path, "MediaMaster.db");
    private const string DbPath = "C:\\Users\\Antony\\AppData\\Local\\Packages\\MediaMaster_dqnfd4b7hk63t\\LocalState\\MediaMaster.db";

    protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
    {
        optionsBuilder.UseSqlite($"Data Source={DbPath}");
        //optionsBuilder.UseChangeTrackingProxies();
        optionsBuilder.EnableDetailedErrors();
        optionsBuilder.EnableSensitiveDataLogging();
        //optionsBuilder.UseLazyLoadingProxies();

        optionsBuilder.LogTo(message => Debug.WriteLine(message), LogLevel.Information);
    }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Media>()
            .HasMany(e => e.Tags)
            .WithMany(e => e.Medias)
            .UsingEntity<MediaTag>();
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

    public void AddTag(string name, IEnumerable<Tag> parents)
    {
        Tag tag = this.CreateProxy<Tag>(t =>
        {
            t.Name = name;
            foreach (Tag tag in parents)
            {
                t.Parents.Add(tag);
            }
        });
        Tags.Add(tag);
    }

    public void AddMedia(string name, string path, IEnumerable<Tag> tags)
    {
        Media media = this.CreateProxy<Media>(m =>
        {
            m.Name = name;
            m.FilePath = path;
            foreach (Tag tag in tags)
            {
                m.Tags.Add(tag);
            }
        });
        Medias.Add(media);
    }
}