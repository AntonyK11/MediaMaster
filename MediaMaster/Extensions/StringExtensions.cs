namespace MediaMaster.Extensions;

public static class StringExtensions
{
    public static bool IsWebsite(this string uri) => uri.StartsWith("http");

    public static bool IsNullOrEmpty(this string? str) => string.IsNullOrEmpty(str);
}
