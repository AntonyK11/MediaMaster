using MediaMaster.Services;
using MediaMaster.Views;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Text.Json.Serialization.Metadata;

namespace MediaMaster.Helpers;

[JsonSourceGenerationOptions(WriteIndented = true, Converters = [typeof(FilterObjectConverter), typeof(OperationsConverter)], DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingDefault)]
[JsonSerializable(typeof(ElementTheme))]
[JsonSerializable(typeof(bool))]
[JsonSerializable(typeof(string))]
[JsonSerializable(typeof(List<BrowserData>))]
[JsonSerializable(typeof(List<StoredSearch>))]
[JsonSerializable(typeof(ICollection<FilterObject>))]
[JsonSerializable(typeof(ICollection<FilterType>))]
[JsonSerializable(typeof(Filter))]
[JsonSerializable(typeof(FilterGroup))]
[JsonSerializable(typeof(NameOperations))]
[JsonSerializable(typeof(NotesOperations))]
[JsonSerializable(typeof(PathOperations))]
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
            if (!doc.RootElement.TryGetProperty("FiltersCollection", out _))
            {
                return JsonSerializer.Deserialize(doc.RootElement.GetRawText(), SourceGenerationContext.Default.FilterGroup);
            }

            return JsonSerializer.Deserialize(doc.RootElement.GetRawText(), SourceGenerationContext.Default.Filter);
        }
    }

    public override void Write(Utf8JsonWriter writer, FilterObject value, JsonSerializerOptions options)
    {
        switch (value)
        {
            case FilterGroup filterGroup:
                JsonSerializer.Serialize(writer, filterGroup, SourceGenerationContext.Default.FilterGroup);
                break;

            case Filter filter:
                JsonSerializer.Serialize(writer, filter, SourceGenerationContext.Default.Filter);
                break;
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

            return name.GetRawText() switch
            {
                "\"NameOperations\"" => JsonSerializer.Deserialize(doc.RootElement.GetRawText(), SourceGenerationContext.Default.NameOperations),
                "\"NotesOperations\"" => JsonSerializer.Deserialize(doc.RootElement.GetRawText(), SourceGenerationContext.Default.NotesOperations),
                "\"PathOperations\"" => JsonSerializer.Deserialize(doc.RootElement.GetRawText(), SourceGenerationContext.Default.PathOperations),
                "\"DateOperations\"" => JsonSerializer.Deserialize(doc.RootElement.GetRawText(), SourceGenerationContext.Default.DateOperations),
                "\"TagsOperations\"" => JsonSerializer.Deserialize(doc.RootElement.GetRawText(), SourceGenerationContext.Default.TagsOperations),
                _ => null
            };
        }
    }

    public override void Write(Utf8JsonWriter writer, Operations value, JsonSerializerOptions options)
    {
        switch (value)
        {
            case NameOperations nameOperations:
                JsonSerializer.Serialize(writer, nameOperations, SourceGenerationContext.Default.NameOperations);
                break;

            case NotesOperations notesOperations:
                JsonSerializer.Serialize(writer, notesOperations, SourceGenerationContext.Default.NotesOperations);
                break;

            case PathOperations pathOperations:
                JsonSerializer.Serialize(writer, pathOperations, SourceGenerationContext.Default.PathOperations);
                break;

            case DateOperations dateOperations:
                JsonSerializer.Serialize(writer, dateOperations, SourceGenerationContext.Default.DateOperations);
                break;

            case TagsOperations tagsOperations:
                JsonSerializer.Serialize(writer, tagsOperations, SourceGenerationContext.Default.TagsOperations);
                break;
        }
    }
}
