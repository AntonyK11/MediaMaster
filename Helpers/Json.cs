using System.Text.Json;

namespace MediaMaster.Helpers;

public static class Json
{
    public static async Task<T?> ToObjectAsync<T>(string value)
    {
        if (typeof(T) == typeof(string))
        {
            return (T)(object)value;
        }
        return await Task.Run(() => JsonSerializer.Deserialize<T>(value));
    }

    public static async Task<string> StringifyAsync(object? value)
    {
        if (value is string str)
        {
            return str;
        }
        return await Task.Run(() => JsonSerializer.Serialize(value));
    }
}
