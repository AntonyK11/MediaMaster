using MediaMaster.Extensions;
using Microsoft.UI.Xaml.Data;
using Microsoft.UI.Xaml.Media;

namespace MediaMaster.Helpers;

public sealed partial class ColorToThemeConverter : IValueConverter
{
    public bool IsInverted { get; set; }

    public object Convert(object value, Type targetType, object parameter, string language)
    {
        var color = ((SolidColorBrush)value).Color.ToSystemColor();
        var isDarkTheme = color.GetRelativeLuminance() > 0.5;

        if (IsInverted)
        {
            return isDarkTheme ? ElementTheme.Light : ElementTheme.Dark;
        }
        return isDarkTheme ? ElementTheme.Dark : ElementTheme.Light;
    }

    public object ConvertBack(object value, Type targetType, object parameter, string language)
    {
        throw new NotImplementedException();
    }
}