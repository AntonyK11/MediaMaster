using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Drawing;
using MediaMaster.Extensions;
using Microsoft.UI.Xaml.Media;

namespace MediaMaster.DataBase.Models;

public class Tag
{
    [Key]
    public virtual int TagId { get; set; }

    //public virtual required string Name { get; set; }

    public virtual string Name { get; set; } = "";

    public virtual string? Shorthand { get; set; } = "";

    public virtual ICollection<string> Aliases { get; } = [];

    public virtual ICollection<Media> Medias { get; } = [];

    public virtual ICollection<Tag> Parents { get; } = [];

    public virtual ICollection<Tag> Children { get; } = [];

    public int? Argb { get; set; }

    [NotMapped]
    public Color Color
    {
        get
        {
            return Argb == null ? Name.CalculateColor() : Color.FromArgb((int)Argb);
        }
        set
        {
            Argb = value.ToArgb();
        }
    }

    [NotMapped] public virtual Color TextColor => Color.CalculateColorText();

    [NotMapped] 
    public virtual SolidColorBrush ColorBrush
    {
        get
        {
            var color = Color.ToWindowsColor();
            return new SolidColorBrush(color);
        }
    }

    [NotMapped]
    public virtual SolidColorBrush TextColorBrush
    {
        get
        {
            var color = TextColor.ToWindowsColor();
            return new SolidColorBrush(color);
        }
    }
}