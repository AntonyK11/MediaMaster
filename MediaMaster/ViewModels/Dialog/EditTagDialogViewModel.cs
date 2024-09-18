using Windows.UI;
using CommunityToolkit.Mvvm.ComponentModel;
using Microsoft.UI.Xaml.Media;
using ColorHelper = Microsoft.UI.ColorHelper;

namespace MediaMaster.ViewModels.Dialog;

public partial class EditTagDialogViewModel : ObservableObject
{
    [ObservableProperty] private bool _canChangeAliases = true;
    [ObservableProperty] private bool _canChangeColor = true;
    [ObservableProperty] private bool _canChangeName = true;
    [ObservableProperty] private bool _canChangeParents = true;
    [ObservableProperty] private bool _canChangeShortHand = true;
    
    [ObservableProperty] private SolidColorBrush _colorBrush = new();
    [ObservableProperty] private string _colorName = "";
    [ObservableProperty] private string _name = "";
    [ObservableProperty] private string _shorthand = "";
    
    private Color _color;

    public Color Color
    {
        get => _color;
        set
        {
            _color = value;
            ColorBrush = new SolidColorBrush(value);
            ColorName = ColorHelper.ToDisplayName(value);
        }
    }

    public void SetPermissions(TagPermissions permissions)
    {
        CanChangeName = !permissions.HasFlag(TagPermissions.CannotChangeName);
        CanChangeShortHand = !permissions.HasFlag(TagPermissions.CannotChangeShorthand);
        CanChangeAliases = !permissions.HasFlag(TagPermissions.CannotChangeAliases);
        CanChangeColor = !permissions.HasFlag(TagPermissions.CannotChangeColor);
        CanChangeParents = !permissions.HasFlag(TagPermissions.CannotChangeParents);
    }
}