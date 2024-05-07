using System.ComponentModel.DataAnnotations;

namespace MediaMaster.DataBase.Models;

public class Tag
{
    [Key]
    public virtual int TagId { get; init; }

    public virtual required string Name { get; set; }

    public virtual ICollection<Media> Medias { get; } = [];

    public virtual ICollection<Tag> Parents { get; } = [];

    public virtual ICollection<Tag> Children { get; } = [];
}