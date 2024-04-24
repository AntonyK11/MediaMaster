using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Automation;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Markup;

namespace MediaMaster.Controls;

// from https://github.com/veler/DevToys/blob/main/src/dev/impl/DevToys/UI/Controls/ExpandableSettingControl.xaml.cs

[ContentProperty(Name = nameof(SettingActionableElement))]
public sealed partial class ExpandableSettingControl
{
    public FrameworkElement? SettingActionableElement { get; set; }

    public static readonly DependencyProperty TitleProperty
        = DependencyProperty.Register(
            nameof(Title),
            typeof(string),
            typeof(ExpandableSettingControl),
            new(string.Empty));

    public string Title
    {
        get => (string)GetValue(TitleProperty);
        set => SetValue(TitleProperty, value);
    }

    public static readonly DependencyProperty DescriptionProperty
        = DependencyProperty.Register(
            nameof(Description),
            typeof(string),
            typeof(ExpandableSettingControl),
            new(string.Empty));

    public string Description
    {
        get => (string)GetValue(DescriptionProperty);
        set => SetValue(DescriptionProperty, value);
    }

    public static readonly DependencyProperty IconProperty
        = DependencyProperty.Register(
            nameof(Icon),
            typeof(IconElement),
            typeof(ExpandableSettingControl),
            new(null));

    public IconElement Icon
    {
        get => (IconElement)GetValue(IconProperty);
        set => SetValue(IconProperty, value);
    }

    public static readonly DependencyProperty ExpandableContentProperty
        = DependencyProperty.Register(
            nameof(ExpandableContent),
            typeof(FrameworkElement),
            typeof(ExpandableSettingControl),
            new(null));

    public FrameworkElement ExpandableContent
    {
        get => (FrameworkElement)GetValue(ExpandableContentProperty);
        set => SetValue(ExpandableContentProperty, value);
    }

    public static readonly DependencyProperty IsExpandedProperty
        = DependencyProperty.Register(
            nameof(IsExpanded),
            typeof(bool),
            typeof(ExpandableSettingControl),
            new(false));

    public bool IsExpanded
    {
        get => (bool)GetValue(IsExpandedProperty);
        set => SetValue(IsExpandedProperty, value);
    }

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