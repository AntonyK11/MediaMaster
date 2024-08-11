using Microsoft.UI.Xaml.Data;

namespace MediaMaster.Helpers;

/// <summary>
///     Converts an enum to a <see cref="string" /> value.
/// </summary>
public partial class EnumToStringConverter : IValueConverter
{
    public object? Convert(object? value, Type targetType, object parameter, string language)
    {
        if (value == null) return null;
        var stringValue = value.ToString();
        return stringValue ?? null;
    }

    public object ConvertBack(object? value, Type targetType, object parameter, string language)
    {
        if (value == null) return null;
        return Enum.Parse(targetType, (string)value, true);
    }
}