﻿using Microsoft.UI.Xaml.Data;

namespace MediaMaster.Helpers;

public sealed partial class NullToVisibilityConverter : IValueConverter
{
    public bool IsInverted { get; set; }

    public object Convert(object? value, Type targetType, object parameter, string language)
    {
        if (value?.GetType() == typeof(string))
        {
            if (IsInverted)
            {
                return string.IsNullOrEmpty((string)value) ? Visibility.Collapsed : Visibility.Visible;
            }

            return string.IsNullOrEmpty((string)value) ? Visibility.Visible : Visibility.Collapsed;
        }

        if (IsInverted)
        {
            return value == null ? Visibility.Collapsed : Visibility.Visible;
        }

        return value == null ? Visibility.Visible : Visibility.Collapsed;
    }

    public object ConvertBack(object value, Type targetType, object parameter, string language)
    {
        throw new NotImplementedException();
    }
}