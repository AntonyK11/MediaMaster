using Microsoft.UI.Xaml.Data;

namespace MediaMaster.Helpers;

/// <summary>
///     Converts a null value to a <see cref="Visibility" /> value.
/// </summary>
public sealed partial class NullToVisibilityConverter : IValueConverter
{
    public bool IsInverted { get; set; }

    public object Convert(object? value, Type targetType, object parameter, string language)
    {

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