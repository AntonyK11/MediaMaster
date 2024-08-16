using Microsoft.UI.Xaml.Data;

namespace MediaMaster.Helpers;

/// <summary>
///     Converts a null value to a <see cref="Visibility" /> value.
/// </summary>
public sealed partial class UriToListOfStringConverter : IValueConverter
{
    public object Convert(object? value, Type targetType, object parameter, string language)
    {
        if (value is Uri uri)
        {
            ICollection<string> collection = [uri.AbsoluteUri];
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