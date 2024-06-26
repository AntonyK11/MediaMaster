using MediaMaster.DataBase.Models;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Data;

namespace MediaMaster.Helpers;

/// <summary>
///     Converts a <see cref="TagFlags" /> value to a <see cref="Visibility" /> value.
/// </summary>
public sealed class TagFlagsToVisibilityConverter : IValueConverter
{
    public bool IsInverted { get; set; }

    public Visibility? Force { get; set; } = null;

    public object Convert(object? value, Type targetType, object parameter, string language)
    {
        if (Force != null) return Force;

        var hasFlag = value is TagFlags flags && Enum.TryParse(parameter.ToString(), true, out TagFlags flag) && flags.HasFlag(flag);

        if (IsInverted)
        {
            return hasFlag ? Visibility.Visible : Visibility.Collapsed;
        }

        return hasFlag ? Visibility.Collapsed : Visibility.Visible;
    }

    public object ConvertBack(object value, Type targetType, object parameter, string language)
    {
        throw new NotImplementedException();
    }
}