using DependencyPropertyGenerator;
using Microsoft.UI.Xaml.Markup;

namespace MediaMaster.Controls;

// from https://github.com/veler/DevToys/blob/main/src/dev/impl/DevToys/UI/Controls/ExpandableSettingHeaderControl.xaml.cs

[ContentProperty(Name = nameof(SettingActionableElement))]
[DependencyProperty("Title", typeof(string), DefaultValue = "")]
[DependencyProperty("Description", typeof(string), DefaultValue = "")]
[DependencyProperty("Icon", typeof(IconElement))]
public partial class ExpandableSettingHeaderControl
{
    public ExpandableSettingHeaderControl()
    {
        InitializeComponent();
    }

    public FrameworkElement? SettingActionableElement { get; set; }
}