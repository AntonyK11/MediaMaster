using System.Security.Cryptography;
using System.Text;
using Windows.UI;
using MediaMaster.Interfaces.Services;

namespace MediaMaster.Extensions;

public static class ColorExtensions
{
    public static Color ToWindowsColor(this System.Drawing.Color color)
    {
        return Color.FromArgb(color.A, color.R, color.G, color.B);
    }


    public static System.Drawing.Color ToSystemColor(this Color color)
    {
        return System.Drawing.Color.FromArgb(color.A, color.R, color.G, color.B);
    }

    public static System.Drawing.Color CalculateColor(this string name)
    {
        if (string.IsNullOrEmpty(name))
        {
            return new System.Drawing.Color();
        }

        var hash = SHA256.HashData(Encoding.UTF8.GetBytes(name));
        var index = 0;

        System.Drawing.Color color;
        do
        {
            if (index + 2 >= hash.Length)
            {
                index = 0;
                hash = SHA256.HashData(Encoding.UTF8.GetBytes(Convert.ToBase64String(hash)));
            }

            color = System.Drawing.Color.FromArgb(50, hash[index], hash[index + 1], hash[index + 2]);
            index++;
        } while (color.GetBackgroundColor(ElementTheme.Dark)
                     .CalculateContrastRatio(color.CalculateColorText(ElementTheme.Dark)) < 4.5 ||
                 color.GetBackgroundColor(ElementTheme.Light)
                     .CalculateContrastRatio(color.CalculateColorText(ElementTheme.Light)) < 4.5);

        return color;
    }

    public static System.Drawing.Color CalculateColorText(this System.Drawing.Color color, ElementTheme? theme = null)
    {
        System.Drawing.Color backgroundColor = color.GetBackgroundColor(theme);

        System.Drawing.Color adjustedColor = GetRelativeLuminance(backgroundColor) < 0.5
            ? System.Drawing.Color.FromArgb(255, 255, 255, 255)
            : System.Drawing.Color.FromArgb(255, 0, 0, 0);

        return adjustedColor;
    }

    public static System.Drawing.Color GetBackgroundColor(this System.Drawing.Color color, ElementTheme? theme = null)
    {
        theme ??= App.GetService<IThemeSelectorService>().ActualTheme;

        System.Drawing.Color themeColor = theme == ElementTheme.Light
            ? System.Drawing.Color.FromArgb(255, 254, 251, 253)
            : System.Drawing.Color.FromArgb(255, 45, 45, 45);

        var alphaPercent = (double)color.A / 255;

        return BlendColor(color, themeColor, alphaPercent);
    }

    private static System.Drawing.Color BlendColor(this System.Drawing.Color color1, System.Drawing.Color color2,
        double percent)
    {
        var r = color1.R * percent + color2.R * (1 - percent);
        var g = color1.G * percent + color2.G * (1 - percent);
        var b = color1.B * percent + color2.B * (1 - percent);

        return System.Drawing.Color.FromArgb(255, (int)r, (int)g, (int)b);
    }

    // https://github.com/microsoft/WinUI-Gallery/blob/main/WinUIGallery/ControlPages/Accessibility/AccessibilityColorContrastPage.xaml.cs#L67-L86
    // Find the contrast ratio: https://www.w3.org/WAI/GL/wiki/Contrast_ratio
    public static double CalculateContrastRatio(this System.Drawing.Color first, System.Drawing.Color second)
    {
        var relLuminanceOne = GetRelativeLuminance(first);
        var relLuminanceTwo = GetRelativeLuminance(second);
        return (Math.Max(relLuminanceOne, relLuminanceTwo) + 0.05)
               / (Math.Min(relLuminanceOne, relLuminanceTwo) + 0.05);
    }

    // Get relative luminance: https://www.w3.org/WAI/GL/wiki/Relative_luminance
    public static double GetRelativeLuminance(this System.Drawing.Color c)
    {
        var rSRGB = c.R / 255.0;
        var gSRGB = c.G / 255.0;
        var bSRGB = c.B / 255.0;

        var r = rSRGB <= 0.04045 ? rSRGB / 12.92 : Math.Pow((rSRGB + 0.055) / 1.055, 2.4);
        var g = gSRGB <= 0.04045 ? gSRGB / 12.92 : Math.Pow((gSRGB + 0.055) / 1.055, 2.4);
        var b = bSRGB <= 0.04045 ? bSRGB / 12.92 : Math.Pow((bSRGB + 0.055) / 1.055, 2.4);
        return 0.2126 * r + 0.7152 * g + 0.0722 * b;
    }
}