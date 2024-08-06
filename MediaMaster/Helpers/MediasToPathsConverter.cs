using MediaMaster.DataBase.Models;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Data;

namespace MediaMaster.Helpers;

/// <summary>
///     Converts a null value to a <see cref="Visibility" /> value.
/// </summary>
public sealed class MediasToPathsConverter : IValueConverter
{
    public object Convert(object? value, Type targetType, object parameter, string language)
    {
        if (value is ICollection<Media> medias)
        {
            return medias.Select(m => m.Uri).ToList();
        }

        ICollection<string> emptyCollection = [];
        return emptyCollection;
    }

    public object ConvertBack(object value, Type targetType, object parameter, string language)
    {
        throw new NotImplementedException();
    }
}