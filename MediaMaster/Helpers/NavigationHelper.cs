﻿namespace MediaMaster.Helpers;

public sealed class NavigationHelper
{
    public static readonly DependencyProperty NavigateToProperty =
        DependencyProperty.RegisterAttached(
            "NavigateTo",
            typeof(string),
            typeof(NavigationHelper),
            new PropertyMetadata(null));

    public static string GetNavigateTo(NavigationViewItem item)
    {
        return (string)item.GetValue(NavigateToProperty);
    }

    public static void SetNavigateTo(NavigationViewItem item, string value)
    {
        item.SetValue(NavigateToProperty, value);
    }
}