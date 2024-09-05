using DependencyPropertyGenerator;

namespace MediaMaster.Controls;

[DependencyProperty("Text", typeof(string), DefaultValue = "")]
[DependencyProperty("Icon", typeof(IconElement), DefaultValueExpression = "new FontIcon() { Glyph = \"&#xE946;\" }")]
public partial class TipIcon : UserControl
{
    public TipIcon()
    {
        InitializeComponent();
    }

    private void TipIconButton_OnClick(object sender, RoutedEventArgs e)
    {
        TipIconTeachingTip.IsOpen = true;
    }
}