using MediaMaster.Services;
using MediaMaster.Views;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Text.Json.Serialization.Metadata;

namespace MediaMaster.Helpers;

[JsonSourceGenerationOptions(WriteIndented = true)]
[JsonSerializable(typeof(ElementTheme))]
[JsonSerializable(typeof(bool))]
[JsonSerializable(typeof(string))]
[JsonSerializable(typeof(List<BrowserData>))]
[JsonSerializable(typeof(List<StoredSearch>))]
[JsonSerializable(typeof(ICollection<FilterObject>))]
[JsonSerializable(typeof(ICollection<FilterType>))]
[JsonSerializable(typeof(Filter))]
[JsonSerializable(typeof(FilterGroup))]
[JsonSerializable(typeof(TextOperations))]
[JsonSerializable(typeof(DateOperations))]
[JsonSerializable(typeof(TagsOperations))]
internal partial class SourceGenerationContext : JsonSerializerContext;

public static class Json
{
    public static async Task<T?> ToObjectAsync<T>(string value, JsonTypeInfo<T> typeInfo)
    {
        if (typeof(T) == typeof(string))
        {
            return (T)(object)value;
        }
        return await Task.Run(() => JsonSerializer.Deserialize(value, typeInfo));
    }

    public static async Task<string> StringifyAsync<T>(T value, JsonTypeInfo<T> typeInfo)
    {
        if (value is string str)
        {
            return str;
        }
        return await Task.Run(() => JsonSerializer.Serialize(value, typeInfo));
    }
}
