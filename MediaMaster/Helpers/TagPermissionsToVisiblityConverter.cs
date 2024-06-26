using MediaMaster.DataBase.Models;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Data;

namespace MediaMaster.Helpers;

/// <summary>
///     Converts a <see cref="TagPermissions" /> value to a <see cref="Visibility" /> value.
/// </summary>
public sealed class TagPermissionsToVisibilityConverter : IValueConverter
{
    public bool IsInverted { get; set; }

    public object Convert(object? value, Type targetType, object parameter, string language)
    {
        var hasFlag = value is TagPermissions permissions && Enum.TryParse(parameter.ToString(), true, out TagPermissions permission) && permissions.HasFlag(permission);

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