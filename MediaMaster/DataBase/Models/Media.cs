using System.ComponentModel.DataAnnotations;
using Microsoft.EntityFrameworkCore;

namespace MediaMaster.DataBase.Models;

[Index(nameof(Uri), IsUnique = true)]
public class Media : IHasTimestamps
{
    [Key]
    public virtual int MediaId { get; set; }

    public virtual string Name { get; set; } = "";

    public virtual string Description { get; set; } = "";

    public virtual string Uri { get; set; } = "";

    public virtual DateTime Modified { get; set; } = DateTime.UtcNow;

    public virtual DateTime Added { get; set; } = DateTime.UtcNow;

    public virtual ICollection<Tag> Tags { get; } = [];

    public virtual bool IsArchived { get; set; } = false;

    public virtual bool IsFavorite { get; set; } = false;
}