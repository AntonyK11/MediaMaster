using DependencyPropertyGenerator;

namespace MediaMaster.Controls;

[DependencyProperty("DeleteButtonVisibility", typeof(Visibility), DefaultValue = Visibility.Collapsed)]
internal partial class CustomItemContainer : ItemContainer
{
    public CustomItemContainer()
    {
        Loaded += (_, _) =>
        {
            ApplyTemplate();
            VisualStateManager.GoToState(this,
                DeleteButtonVisibility == Visibility.Visible ?
                    "DeleteButtonVisible" :
                    "DeleteButtonCollapsed",
                true);
        };
    }

    partial void OnDeleteButtonVisibilityChanged(Visibility newValue)
    {
        VisualStateManager.GoToState(this,
            newValue == Visibility.Visible ?
                "DeleteButtonVisible" :
                "DeleteButtonCollapsed",
            true);
    }
}