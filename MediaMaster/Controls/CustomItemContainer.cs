using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;

namespace MediaMaster.Controls;

internal class CustomItemContainer : ItemContainer
{
    public static readonly DependencyProperty DeleteButtonVisibilityProperty
        = DependencyProperty.Register(
            nameof(DeleteButtonVisibilityProperty),
            typeof(Visibility),
            typeof(CustomItemContainer),
            new PropertyMetadata(Visibility.Collapsed));

    public Visibility DeleteButtonVisibility
    {
        get => (Visibility)GetValue(DeleteButtonVisibilityProperty);
        set
        {
            SetValue(DeleteButtonVisibilityProperty, value);
            VisualStateManager.GoToState(this, value == Visibility.Visible ? "DeleteButtonVisible" : "DeleteButtonCollapsed", true);
        }
    }
    
    public CustomItemContainer()
    {
        Loaded += (_, _) =>
        {
            ApplyTemplate();
            VisualStateManager.GoToState(this, DeleteButtonVisibility == Visibility.Visible ? "DeleteButtonVisible" : "DeleteButtonCollapsed", true);
        };
    }
}

