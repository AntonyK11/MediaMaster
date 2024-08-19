using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Drawing;
using MediaMaster.Extensions;
using MediaMaster.Interfaces.Services;
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

    public virtual string FirstParentReferenceName { get; set; } = "";

    public virtual TagFlags Flags { get; set; }

    public virtual TagPermissions Permissions { get; set; }

    public virtual IList<string> Aliases { get; set; } = [];

    public virtual ICollection<Media> Medias { get; set; } = [];

    public virtual ICollection<Tag> Parents { get; set; } = [];

    public virtual ICollection<Tag> Children { get; set; } = [];

    public int? Argb { get; set; }

    public string GetReferenceName()
    {
        return Shorthand.IsNullOrEmpty() ? Name.ToLower() : Shorthand;
    }

    private Color? _color;
    private Color? _textColor;
    private string _oldName = "";
    private int? _oldargb;
    private Color? _oldcolor;
    private ElementTheme? _oldTheme;
    private ElementTheme? _oldTextTheme;
    private static ElementTheme CurrentTheme => App.GetService<IThemeSelectorService>().ActualTheme;

    [NotMapped]
    public string DisplayName
    {
        get
        {
            return FirstParentReferenceName.IsNullOrEmpty() ? Name : $"{Name} ({FirstParentReferenceName})";
        }
    }

    [NotMapped]
    public Color Color
    {
        get
        {
            var currentTheme = CurrentTheme;
            if (_color != null && _oldargb == Argb && (Argb != null || _oldName == Name) && _oldTheme == currentTheme)
            {
                return (Color)_color;
            }

            _oldName = Name;
            _oldargb = Argb;
            _oldTheme = currentTheme;
            _color = Argb == null ? Name.CalculateColor() : Color.FromArgb((int)Argb);
            return (Color)_color;
        }
        set
        {
            Argb = value.ToArgb();
        }
    }



    [NotMapped] public virtual Color TextColor
    {
        get
        {
            var currentTheme = CurrentTheme;
            if (_textColor != null && _oldcolor == Color && _oldTextTheme == currentTheme)
            {
                return (Color)_textColor;
            }

            _oldcolor = Color;
            _oldTextTheme = currentTheme;
            _textColor = Color.CalculateColorText();
            return (Color)_textColor;
        }
    }

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