using System.Diagnostics.CodeAnalysis;

namespace MediaMaster.Extensions;

public static class StringExtensions
{
    public static bool IsWebsite(this string uri) => uri.StartsWith("http");

    public static bool IsNullOrEmpty([NotNullWhen(false)] this string? str) => string.IsNullOrEmpty(str);

    public static string FormatAsWebsite(this string url)
    {
        if (!url.IsNullOrEmpty())
        {
            url = url.Trim();

            if (!url.StartsWith("http", StringComparison.OrdinalIgnoreCase))
            {
                url = url.StartsWith("www.", StringComparison.OrdinalIgnoreCase)
                    ? url[4..]
                    : url;

                url = "https://" + url;
            }
            else
            {
                url = url.Replace("://www.", "://");
            }

            if (url.EndsWith('/'))
            {
                url = url.TrimEnd('/');
            }
        }

        return url;
    }
}
