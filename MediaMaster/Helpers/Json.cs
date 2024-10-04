using MediaMaster.Services;
using MediaMaster.Views;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Text.Json.Serialization.Metadata;

namespace MediaMaster.Helpers;

[JsonSourceGenerationOptions(WriteIndented = true, Converters = [typeof(FilterObjectConverter), typeof(OperationsConverter)])]
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
internal sealed partial class SourceGenerationContext : JsonSerializerContext;

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

public sealed class FilterObjectConverter : JsonConverter<FilterObject>
{
    public override FilterObject? Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        using (JsonDocument doc = JsonDocument.ParseValue(ref reader))
        {
            if (doc.RootElement.TryGetProperty("_orCombination", out _))
            {
                return JsonSerializer.Deserialize(doc.RootElement.GetRawText(), SourceGenerationContext.Default.FilterGroup);
            }
            else
            {
                return JsonSerializer.Deserialize(doc.RootElement.GetRawText(), SourceGenerationContext.Default.Filter);
            }
        }
    }

    public override void Write(Utf8JsonWriter writer, FilterObject value, JsonSerializerOptions options)
    {
        if (value is FilterGroup filterGroup)
        {
            JsonSerializer.Serialize(writer, filterGroup, SourceGenerationContext.Default.FilterGroup);
        }
        else if (value is Filter filter)
        {
            JsonSerializer.Serialize(writer, filter, SourceGenerationContext.Default.Filter);
        }
    }
}

public class OperationsConverter : JsonConverter<Operations>
{
    public override Operations? Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        using (JsonDocument doc = JsonDocument.ParseValue(ref reader))
        {
            doc.RootElement.TryGetProperty("Name", out var name);

            if (name.GetString() == "TextOperations")
            {
                return JsonSerializer.Deserialize(doc.RootElement.GetRawText(), SourceGenerationContext.Default.TextOperations);
            }
            else if (name.GetString() == "DateOperations")
            {
                return JsonSerializer.Deserialize(doc.RootElement.GetRawText(), SourceGenerationContext.Default.DateOperations);
            }
            else
            {
                return JsonSerializer.Deserialize(doc.RootElement.GetRawText(), SourceGenerationContext.Default.TagsOperations);
            }
        }
    }

    public override void Write(Utf8JsonWriter writer, Operations value, JsonSerializerOptions options)
    {
        if (value is TextOperations textOperations)
        {
            JsonSerializer.Serialize(writer, textOperations, SourceGenerationContext.Default.TextOperations);
        }
        else if (value is DateOperations dateOperations)
        {
            JsonSerializer.Serialize(writer, dateOperations, SourceGenerationContext.Default.DateOperations);
        }
        else if (value is TagsOperations tagsOperations)
        {
            JsonSerializer.Serialize(writer, tagsOperations, SourceGenerationContext.Default.TagsOperations);
        }
    }
}
