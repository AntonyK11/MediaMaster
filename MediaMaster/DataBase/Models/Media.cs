using System.ComponentModel.DataAnnotations;
using MediaMaster.Extensions;
using Microsoft.EntityFrameworkCore;

namespace MediaMaster.DataBase.Models;

[Index(nameof(Uri), IsUnique = true)]
[Index(nameof(Name))]
[Index(nameof(Modified))]
[Index(nameof(Added))]
[Index(nameof(IsArchived))]
[Index(nameof(IsFavorite))]
public class Media : IHasTimestamps
{
    [Key]
    public virtual int MediaId { get; set; }

    public virtual string Name { get; set; } = "";

    public virtual string Description { get; set; } = "";

    public virtual string Uri { get; set; } = "";

    public virtual DateTime Modified { get; set; } = DateTime.UtcNow.GetDateTimeUpToSeconds();

    public virtual DateTime Added { get; init; } = DateTime.UtcNow.GetDateTimeUpToSeconds();

    public virtual ICollection<Tag> Tags { get; } = [];

    public virtual bool IsArchived { get; set; } = false;

    public virtual bool IsFavorite { get; set; } = false;
}