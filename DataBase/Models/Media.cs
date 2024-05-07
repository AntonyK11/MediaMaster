using System.ComponentModel.DataAnnotations;

namespace MediaMaster.DataBase.Models;

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