using System.ComponentModel.DataAnnotations;
using MediaMaster.Extensions;
using Microsoft.EntityFrameworkCore;

namespace MediaMaster.DataBase.Models;

[Index(nameof(Uri), IsUnique = true)]
[Index(nameof(Name))]
[Index(nameof(Notes))]
[Index(nameof(Modified))]
[Index(nameof(Added))]
[Index(nameof(IsArchived))]
[Index(nameof(IsFavorite))]
public sealed class Media
{
    [Key]
    public int MediaId { get; set; }

    public string Name { get; set; } = "";

    public string Notes { get; set; } = "";

    public string Uri { get; set; } = "";

    public DateTime Modified { get; set; } = DateTime.UtcNow.GetDateTimeUpToSeconds();

    public DateTime Added { get; set; } = DateTime.UtcNow.GetDateTimeUpToSeconds();

    public ICollection<Tag> Tags { get; } = [];

    public bool IsArchived { get; set; } = false;

    public bool IsFavorite { get; set; } = false;
}