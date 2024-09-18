using Microsoft.UI.Xaml.Data;

namespace MediaMaster.Helpers;

public partial class UriToListOfStringConverter : IValueConverter
{
    public object Convert(object? value, Type targetType, object parameter, string language)
    {
        if (value is Uri uri)
        {
            ICollection<string> collection = [uri.AbsoluteUri];
            return collection;
        }

        if (value is string str)
        {
            ICollection<string> collection = [str];
            return collection;
        }

        ICollection<string> emptyCollection = [];
        return emptyCollection;
    }

    public object ConvertBack(object value, Type targetType, object parameter, string language)
    {
        throw new NotImplementedException();
    }
}