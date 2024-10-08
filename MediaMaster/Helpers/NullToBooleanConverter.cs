﻿using Microsoft.UI.Xaml.Data;

namespace MediaMaster.Helpers;

// https://github.com/veler/DevToys/blob/main/src/dev/impl/DevToys/UI/Converters/NullToBooleanConverter.cs
public sealed partial class NullToBooleanConverter : IValueConverter
{
    public bool IsInverted { get; set; }

    public bool EnforceNonWhiteSpaceString { get; set; }

    public object Convert(object? value, Type targetType, object parameter, string language)
    {
        if (value?.GetType() == typeof(string))
        {
            if (IsInverted)
            {
                if (EnforceNonWhiteSpaceString)
                {
                    return !string.IsNullOrWhiteSpace((string)value);
                }

                return !string.IsNullOrEmpty((string)value);
            }

            if (EnforceNonWhiteSpaceString)
            {
                return !string.IsNullOrWhiteSpace((string)value);
            }

            return string.IsNullOrEmpty((string)value);
        }

        if (IsInverted)
        {
            return value != null;
        }

        return value == null;
    }

    public object ConvertBack(object value, Type targetType, object parameter, string language)
    {
        throw new NotImplementedException();
    }
}