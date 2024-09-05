using Microsoft.UI.Xaml.Data;

namespace MediaMaster.Helpers;

public partial class EnumToBooleanConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, string language)
    {
        if (parameter is not string enumString)
        {
            throw new ArgumentException("ExceptionEnumToBooleanConverterParameterMustBeAnEnumName");
        }

        if (!Enum.IsDefined(value.GetType(), value))
        {
            throw new ArgumentException("ExceptionEnumToBooleanConverterValueMustBeAnEnum");
        }

        var enumValue = Enum.Parse(value.GetType(), enumString);

        return enumValue.Equals(value);
    }

    public object ConvertBack(object value, Type targetType, object parameter, string language)
    {
        if (parameter is not string enumString)
        {
            throw new ArgumentException("ExceptionEnumToBooleanConverterParameterMustBeAnEnumName");
        }

        return Enum.Parse(targetType, enumString);
    }
}