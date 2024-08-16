using DependencyPropertyGenerator;
using Microsoft.UI.Xaml.Automation;
using Microsoft.UI.Xaml.Markup;

namespace MediaMaster.Controls;

// from https://github.com/veler/DevToys/blob/main/src/dev/impl/DevToys/UI/Controls/ExpandableSettingControl.xaml.cs

[ContentProperty(Name = nameof(SettingActionableElement))]
[DependencyProperty("Title", typeof(string), DefaultValue = "")]
[DependencyProperty("Description", typeof(string), DefaultValue = "")]
[DependencyProperty("Icon", typeof(IconElement))]
[DependencyProperty("ExpandableContent", typeof(FrameworkElement))]
[DependencyProperty("IsExpanded", typeof(bool), DefaultValue = false)]

public sealed partial class ExpandableSettingControl
{
    public FrameworkElement? SettingActionableElement { get; set; }

    public ExpandableSettingControl()
    {
        InitializeComponent();
    }

    private void Expander_Loaded(object sender, RoutedEventArgs e)
    {
        AutomationProperties.SetName(Expander, Title);
        AutomationProperties.SetHelpText(Expander, Description);
    }
}