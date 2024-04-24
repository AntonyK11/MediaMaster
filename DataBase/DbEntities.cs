using Microsoft.EntityFrameworkCore.ChangeTracking;
using Microsoft.EntityFrameworkCore;
using System.Collections.ObjectModel;
using System.ComponentModel.DataAnnotations;

namespace MediaMaster.DataBase;

public static class DbEntities
{
    public static void UpdateTimestamps(object? sender, EntityEntryEventArgs e)
    {
        if (e.Entry is { Entity: IHasTimestamps entityWithTimestamps, State: EntityState.Modified })
        {
            entityWithTimestamps.Modified = DateTime.UtcNow;
        }
    }
}

public interface IHasTimestamps
{
    DateTime Modified { set; }
}

public class Media : IHasTimestamps
{
    [Key]
    public virtual int MediaId { get; init; }

    public virtual required string Name { get; set; }

    public virtual string? Description { get; set; } = null;

    public virtual required string FilePath { get; set; }

    public virtual DateTime Modified { get; set; } = DateTime.UtcNow;

    public virtual DateTime Added { get; set; } = DateTime.UtcNow;

    public virtual ICollection<Tag> Tags { get; } = [];
}

public class Tag
{
    [Key]
    public virtual int TagId { get; init; }

    public virtual required string Name { get; set; }

    public virtual ObservableCollection<Media> Medias { get; } = [];

    public virtual ICollection<Tag> Parents { get; } = [];

    public virtual ICollection<Tag> Children { get; } = [];
}

//public class Category
//{
//    [Key]
//    public virtual int CategoryId { get; init; }

//    public virtual required string Name { get; set; }

//    public virtual ObservableCollection<Extension> Extensions { get; } = [];
//}

//public class Extension
//{
//    [Key]
//    public virtual int ExtensionId { get; init; }

//    public virtual required string Name { get; set; }

//    public virtual ObservableCollection<Category> Categories { get; } = [];
//}
