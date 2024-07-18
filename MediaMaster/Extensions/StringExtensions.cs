namespace MediaMaster.Extensions;

public static class StringExtensions
{
    public static bool IsWebsite(this string uri) => uri.StartsWith("http");
}
