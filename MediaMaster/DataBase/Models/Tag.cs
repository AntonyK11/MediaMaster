using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Drawing;
using MediaMaster.Extensions;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Microsoft.UI.Xaml.Media;

namespace MediaMaster.DataBase.Models;


[Flags]
public enum TagFlags
{
    UserTag = 0x000,
    Extension = 0x001,
    Website = 0x002,
    Favorite = 0x010,
    Archived = 0x100
}

[Flags]
public enum TagPermissions
{
    CannotChangeName = 0x000001,
    CannotChangeShorthand = 0x000010,
    CannotChangeAliases = 0x000100,
    CannotChangeColor = 0x001000,
    CannotChangeParents = 0x010000,
    CannotDelete = 0x100000
}

public class Tag
{
    [Key]
    public virtual int TagId { get; set; }

    public virtual string Name { get; set; } = "";

    public virtual string Shorthand { get; set; } = "";

    public virtual TagFlags Flags { get; set; }

    public virtual TagPermissions Permissions { get; set; }

    public virtual IList<string> Aliases { get; set; } = [];

    public virtual ICollection<Media> Medias { get; set; } = [];

    public virtual ICollection<Tag> Parents { get; set; } = [];

    public virtual ICollection<Tag> Children { get; set; } = [];

    public int? Argb { get; set; }

    private string? _displayName;
    private string _oldName = "";

    [NotMapped]
    public string DisplayName
    {
        get
        {

            if (_displayName != null && _oldName == Name) return _displayName;
            _oldName = Name;

            using (var database = new MediaDbContext())
            {
                //var parentId = database.TagTags.AsNoTracking().FirstOrDefault(t => t.ChildrenTagId == TagId)?.ParentsTagId;
                var parent = database.Tags.AsNoTracking().Select(t => new { t.TagId, Parents = t.Parents.Select(p => new { p.Name, p.Shorthand }) }).FirstOrDefault(t => t.TagId == TagId)?.Parents.MinBy(t => t.Name);
                //if (parentId != null)
                //{
                    //var parent = database.Tags.AsNoTracking().Select(t => new { t.TagId, t.Name, t.Shorthand }).FirstOrDefault(t => t.TagId == (int)parentId);
                    if (parent != null)
                    {
                        var parentShortHand = parent.Shorthand;
                        if (parentShortHand.IsNullOrEmpty())
                        {
                            parentShortHand = parent.Name;
                        }

                        _displayName = $"{Name} ({parentShortHand})";
                        return _displayName;
                    }
                //}
                _displayName = Name;
            }
            return _displayName;
        }
    }

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