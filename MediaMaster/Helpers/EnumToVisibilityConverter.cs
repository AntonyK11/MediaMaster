using Microsoft.UI.Xaml.Data;

namespace MediaMaster.Helpers;

public partial class EnumToVisibilityConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, string language)
    {
        if (parameter is not string enumString)
        {
            throw new ArgumentException("ExceptionEnumToVisibilityConverterParameterMustBeAnEnumName");
        }

        if (!Enum.IsDefined(value.GetType(), value))
        {
            throw new ArgumentException("ExceptionEnumToVisibilityConverterValueMustBeAnEnum");
        }

        var enumValue = Enum.Parse(value.GetType(), enumString);

        return enumValue.Equals(value) ? Visibility.Visible : Visibility.Collapsed;
    }

    public object ConvertBack(object value, Type targetType, object parameter, string language)
    {
        if (parameter is not string enumString)
        {
            throw new ArgumentException("ExceptionEnumToVisibilityConverterParameterMustBeAnEnumName");
        }

        return Enum.Parse(targetType, enumString);
    }
}