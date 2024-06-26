using Windows.UI;
using CommunityToolkit.Mvvm.ComponentModel;
using MediaMaster.DataBase.Models;
using Microsoft.UI.Xaml.Media;

namespace MediaMaster.ViewModels.Dialog;

public partial class EditTagDialogViewModel : ObservableObject
{
    [ObservableProperty] private string _name = "";

    [ObservableProperty] private string _shorthand = "";

    [ObservableProperty] private bool _canChangeName = true;

    [ObservableProperty] private bool _canChangeShortHand = true;

    [ObservableProperty] private bool _canChangeAliases = true;

    [ObservableProperty] private bool _canChangeColor = true;

    [ObservableProperty] private bool _canChangeParents = true;

    [ObservableProperty] private SolidColorBrush _colorBrush = new();

    private Color _color;

    public Color Color
    {
        get { return _color; }
        set
        {
            _color = value;
            ColorBrush = new SolidColorBrush(value);
        }
    }

    private TagPermissions _permissions;
    public TagPermissions Permissions
    {
        get { return _permissions; }
        set
        {
            _permissions = value;
            CanChangeName = !value.HasFlag(TagPermissions.CannotChangeName);
            CanChangeShortHand = !value.HasFlag(TagPermissions.CannotChangeShorthand);
            CanChangeAliases = !value.HasFlag(TagPermissions.CannotChangeAliases);
            CanChangeColor = !value.HasFlag(TagPermissions.CannotChangeColor);
            CanChangeParents = !value.HasFlag(TagPermissions.CannotChangeParents);
        }
    }
}