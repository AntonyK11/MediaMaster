using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Automation;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Markup;

namespace MediaMaster.Controls;

// from https://github.com/veler/DevToys/blob/main/src/dev/impl/DevToys/UI/Controls/ExpandableSettingHeaderControl.xaml.cs

[ContentProperty(Name = nameof(SettingActionableElement))]
public sealed partial class ExpandableSettingHeaderControl
{
    public FrameworkElement? SettingActionableElement { get; set; }

    public static readonly DependencyProperty TitleProperty
        = DependencyProperty.Register(
            nameof(Title),
            typeof(string),
            typeof(ExpandableSettingHeaderControl),
            new(
                string.Empty,
                (d, e) => { AutomationProperties.SetName(d, (string)e.NewValue); }));

    public string Title
    {
        get => (string)GetValue(TitleProperty);
        set => SetValue(TitleProperty, value);
    }


    public static readonly DependencyProperty DescriptionProperty
        = DependencyProperty.Register(
            nameof(Description),
            typeof(string),
            typeof(ExpandableSettingHeaderControl),
            new(
                string.Empty,
                (d, e) => { AutomationProperties.SetHelpText(d, (string)e.NewValue); }));

    public string Description
    {
        get => (string)GetValue(DescriptionProperty);
        set => SetValue(DescriptionProperty, value);
    }

    public static readonly DependencyProperty IconProperty
        = DependencyProperty.Register(
            nameof(Icon),
            typeof(IconElement),
            typeof(ExpandableSettingHeaderControl),
            new(null));

    public IconElement Icon
    {
        get => (IconElement)GetValue(IconProperty);
        set => SetValue(IconProperty, value);
    }

    public ExpandableSettingHeaderControl()
    {
        InitializeComponent();
    }
}