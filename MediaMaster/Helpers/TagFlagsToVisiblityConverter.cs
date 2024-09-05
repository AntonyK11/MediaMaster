using Microsoft.UI.Xaml.Data;

namespace MediaMaster.Helpers;

public partial class TagFlagsToVisibilityConverter : IValueConverter
{
    public bool IsInverted { get; set; }
    
    public object Convert(object? value, Type targetType, object parameter, string language)
    {
        var hasFlag = value is TagFlags flags &&
                      Enum.TryParse(parameter.ToString(), true, out TagFlags flag) &&
                      flags.HasFlag(flag);

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