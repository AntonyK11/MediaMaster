using DependencyPropertyGenerator;

namespace MediaMaster.Controls;

[DependencyProperty("Text", typeof(string))]
public sealed partial class TipIcon : UserControl
{
    public TipIcon()
    {
        this.InitializeComponent();
    }

    private void TipIconButton_OnClick(object sender, RoutedEventArgs e)
    {
        TipIconTeachingTip.IsOpen = true;
    }
}
