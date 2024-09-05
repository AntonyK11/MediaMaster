using Microsoft.UI.Xaml.Data;

namespace MediaMaster.Helpers;

public partial class GetTopCornerRadiusConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, string language)
    {
        if (value is CornerRadius cornerRadius)
        {
            return new CornerRadius(cornerRadius.TopLeft, cornerRadius.TopRight, 0, 0);
        }

        return value;
    }

    public object ConvertBack(object value, Type targetType, object parameter, string language)
    {
        throw new NotImplementedException();
    }
}