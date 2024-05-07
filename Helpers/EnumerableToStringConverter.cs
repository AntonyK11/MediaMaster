using Microsoft.UI.Xaml.Data;
using System.Collections;
using MediaMaster.DataBase;
using MediaMaster.DataBase.Models;

namespace MediaMaster.Helpers;

public class EnumerableToStringConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, string language)
    {
        if (value is not IEnumerable enumerable)
        {
            throw new ArgumentException("The provided value should be an IEnumerable");
        }
        
        if (enumerable.OfType<Media>().Any())
        {
            IEnumerable<string> names = enumerable.OfType<Media>()
                .Select(media => media.Name);
            return string.Join(", ", names);
        }

        //if (enumerable.OfType<Extension>().Any())
        //{
        //    IEnumerable<string> names = enumerable.OfType<Extension>()
        //        .Select(extension => extension.Name);
        //    return string.Join(", ", names);
        //}

        //if (enumerable.OfType<Category>().Any())
        //{
        //    IEnumerable<string> names = enumerable.OfType<Category>()
        //        .Select(category => category.Name);
        //    return string.Join(", ", names);
        //}

        return "null";

    }

    public object ConvertBack(object value, Type targetType, object parameter, string language)
    {
        throw new NotImplementedException();
    }
}