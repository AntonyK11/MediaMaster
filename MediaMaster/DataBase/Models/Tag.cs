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
    Website = 0x002
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

public sealed class Tag
{
    private Color? _color;
    private int? _oldArgb;
    private Color? _oldColor;
    private string _oldName = "";
    private ElementTheme? _oldTextTheme;
    private ElementTheme? _oldTheme;
    private Color? _textColor;

    [Key]
    public int TagId { get; set; }

    public string Name { get; set; } = "";

    public string Shorthand { get; set; } = "";

    public string FirstParentReferenceName { get; set; } = "";

    public TagFlags Flags { get; set; }

    public TagPermissions Permissions { get; set; }

    public IList<string> Aliases { get; set; } = [];

    public ICollection<Media> Medias { get; set; } = [];

    public ICollection<Tag> Parents { get; set; } = [];

    public ICollection<Tag> Children { get; set; } = [];

    public int? Argb { get; set; }
    
    [NotMapped]
    private static ElementTheme CurrentTheme => App.GetService<IThemeSelectorService>().ActualTheme;

    [NotMapped]
    public string ReferenceName => Shorthand.IsNullOrEmpty() ? Name.ToLower() : Shorthand;
    
    [NotMapped]
    public string DisplayName => FirstParentReferenceName.IsNullOrEmpty() ? Name : $"{Name} ({FirstParentReferenceName})";

    [NotMapped]
    public Color Color
    {
        get
        {
            ElementTheme currentTheme = CurrentTheme;
            if (_color != null && _oldArgb == Argb && (Argb != null || _oldName == Name) && _oldTheme == currentTheme)
            {
                return (Color)_color;
            }

            _oldName = Name;
            _oldArgb = Argb;
            _oldTheme = currentTheme;
            _color = Argb == null ? Name.CalculateColor() : Color.FromArgb((int)Argb);
            return (Color)_color;
        }
        set => Argb = value.ToArgb();
    }
    
    [NotMapped]
    public Color TextColor
    {
        get
        {
            ElementTheme currentTheme = CurrentTheme;
            if (_textColor != null && _oldColor == Color && _oldTextTheme == currentTheme)
            {
                return (Color)_textColor;
            }

            _oldColor = Color;
            _oldTextTheme = currentTheme;
            _textColor = Color.CalculateColorText();
            return (Color)_textColor;
        }
    }

    [NotMapped]
    public SolidColorBrush ColorBrush
    {
        get
        {
            Windows.UI.Color color = Color.ToWindowsColor();
            return new SolidColorBrush(color);
        }
    }

    [NotMapped]
    public SolidColorBrush TextColorBrush
    {
        get
        {
            Windows.UI.Color color = TextColor.ToWindowsColor();
            return new SolidColorBrush(color);
        }
    }
}