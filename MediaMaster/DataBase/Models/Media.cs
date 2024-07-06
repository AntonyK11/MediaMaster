using System.ComponentModel.DataAnnotations;
using Microsoft.EntityFrameworkCore;

namespace MediaMaster.DataBase.Models;

[Index(nameof(FilePath), IsUnique = true)]
public class Media : IHasTimestamps
{
    [Key]
    public virtual int MediaId { get; set; }

    public virtual string Name { get; set; } = "";

    public virtual string Description { get; set; } = "";

    public virtual string FilePath { get; set; } = "";

    public virtual DateTime Modified { get; set; } = DateTime.UtcNow;

    public virtual DateTime Added { get; set; } = DateTime.UtcNow;

    public virtual ICollection<Tag> Tags { get; } = [];
}