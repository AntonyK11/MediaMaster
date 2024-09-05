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
public class Media
{
    [Key]
    public int MediaId { get; set; }

    [StringLength(260, MinimumLength = 0)]
    public string Name { get; set; } = "";

    [StringLength(260, MinimumLength = 0)]
    public string Notes { get; set; } = "";

    [StringLength(32767, MinimumLength = 0)]
    public string Uri { get; set; } = "";

    public DateTime Modified { get; set; } = DateTime.UtcNow.GetDateTimeUpToSeconds();

    public DateTime Added { get; set; } = DateTime.UtcNow.GetDateTimeUpToSeconds();

    public ICollection<Tag> Tags { get; } = [];

    public bool IsArchived { get; set; } = false;

    public bool IsFavorite { get; set; } = false;
}