using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;

namespace MediaMaster.Controls;

public sealed partial class TipIcon : UserControl
{
    public static readonly DependencyProperty TextProperty
        = DependencyProperty.Register(
            nameof(Text),
            typeof(string),
            typeof(EditableTextBlock),
            new PropertyMetadata(""));

    public string Text
    {
        get => (string)GetValue(TextProperty);
        set => SetValue(TextProperty, value);
    }

    public TipIcon()
    {
        this.InitializeComponent();
    }

    private void TipIconButton_OnClick(object sender, RoutedEventArgs e)
    {
        TipIconTeachingTip.IsOpen = true;
    }
}
