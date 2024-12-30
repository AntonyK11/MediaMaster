using Windows.UI;
using CommunityToolkit.Mvvm.ComponentModel;
using Microsoft.UI.Xaml.Media;
using ColorHelper = Microsoft.UI.ColorHelper;

namespace MediaMaster.ViewModels.Dialog;

public sealed partial class EditTagDialogViewModel : ObservableObject
{
    [ObservableProperty] public partial bool CanChangeAliases { get; set; } = true;
    [ObservableProperty] public partial bool CanChangeColor { get; set; } = true;
    [ObservableProperty] public partial bool CanChangeName { get; set; } = true;
    [ObservableProperty] public partial bool CanChangeShortHand { get; set; } = true;
    
    [ObservableProperty] public partial SolidColorBrush ColorBrush { get; set; } = new();
    [ObservableProperty] public partial string ColorName { get; set; } = "";
    [ObservableProperty] public partial string Name { get; set; } = "";
    [ObservableProperty] public partial string Shorthand { get; set; } = "";
    
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
    }
}