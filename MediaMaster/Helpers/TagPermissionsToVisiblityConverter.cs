using Microsoft.UI.Xaml.Data;

namespace MediaMaster.Helpers;

public partial class TagPermissionsToVisibilityConverter : IValueConverter
{
    public bool IsInverted { get; set; }

    public object Convert(object? value, Type targetType, object parameter, string language)
    {
        var hasFlag = value is TagPermissions permissions &&
                      Enum.TryParse(parameter.ToString(), true, out TagPermissions permission) &&
                      permissions.HasFlag(permission);

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