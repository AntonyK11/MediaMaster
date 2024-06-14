using Windows.UI;
using CommunityToolkit.Mvvm.ComponentModel;
using Microsoft.UI.Xaml.Media;

namespace MediaMaster.ViewModels.Dialog;

public partial class EditTagDialogViewModel : ObservableObject
{
    [ObservableProperty] private string _name = "";

    [ObservableProperty] private string _shorthand = "";

    [ObservableProperty] private SolidColorBrush _colorBrush = new();

    private Color _color;

    public Color Color
    {
        get { return _color; }
        set
        {
            _color = value;
            ColorBrush = new SolidColorBrush(Color);
        }
    }
}