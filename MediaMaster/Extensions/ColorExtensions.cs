﻿using System.Diagnostics;
using System.Drawing;
using System.Security.Cryptography;
using System.Text;
using MediaMaster.Interfaces.Services;
using Microsoft.UI.Xaml;

namespace MediaMaster.Extensions;

public static class ColorExtensions
{
    public static Windows.UI.Color ToWindowsColor(this Color color)
    {
        return Windows.UI.Color.FromArgb(color.A, color.R, color.G, color.B);
    }


    public static Color ToSystemColor(this Windows.UI.Color color)
    {
        return Color.FromArgb(color.A, color.R, color.G, color.B);
    }

    public static Color CalculateColor(this string name)
    {
        if (string.IsNullOrEmpty(name))
        {
            return new Color();
        }
        var hash = SHA256.HashData(Encoding.UTF8.GetBytes(name));
        var index = 0;

        Color color;
        do
        {
            if (index + 2 >= hash.Length)
            {
                index = 0;
                hash = SHA256.HashData(Encoding.UTF8.GetBytes(Convert.ToBase64String(hash)));
            }

            color = Color.FromArgb(50, hash[index], hash[index + 1], hash[index + 2]);
            index++;

        } while (color.GetBackgroundColor(ElementTheme.Dark).CalculateContrastRatio(color.CalculateColorText(ElementTheme.Dark)) < 4.5 ||
                 color.GetBackgroundColor(ElementTheme.Light).CalculateContrastRatio(color.CalculateColorText(ElementTheme.Light)) < 4.5);

        return color;
    }

    public static Color CalculateColorText(this Color color, ElementTheme? theme = null)
    {
        var backgroundColor = color.GetBackgroundColor(theme);

        var adjustedColor = GetRelativeLuminance(backgroundColor) < 0.5
            ? Color.FromArgb(255, 255, 255, 255)
            : Color.FromArgb(255, 0, 0, 0);

        return adjustedColor;
    }

    public static Color GetBackgroundColor(this Color color, ElementTheme? theme = null)
    {
        theme ??= App.GetService<IThemeSelectorService>().ActualTheme;

        var themeColor = theme == ElementTheme.Light
            ? Color.FromArgb(255, 254, 251, 253)
            : Color.FromArgb(255, 45, 45, 45);

        var alphaPercent = (double)color.A / 255;

        return BlendColor(color, themeColor, alphaPercent);
    }

    public static Color BlendColor(this Color color1, Color color2, double percent)
    {

        var r = color1.R * percent + color2.R * (1 - percent);
        var g = color1.G * percent + color2.G * (1 - percent);
        var b = color1.B * percent + color2.B * (1 - percent);

        return Color.FromArgb(255, (int)r, (int)g, (int)b);
    }

    // https://github.com/microsoft/WinUI-Gallery/blob/main/WinUIGallery/ControlPages/Accessibility/AccessibilityColorContrastPage.xaml.cs#L67-L86
    // Find the contrast ratio: https://www.w3.org/WAI/GL/wiki/Contrast_ratio
    public static double CalculateContrastRatio(this Color first, Color second)
    {
        var relLuminanceOne = GetRelativeLuminance(first);
        var relLuminanceTwo = GetRelativeLuminance(second);
        return (Math.Max(relLuminanceOne, relLuminanceTwo) + 0.05)
               / (Math.Min(relLuminanceOne, relLuminanceTwo) + 0.05);
    }

    // Get relative luminance: https://www.w3.org/WAI/GL/wiki/Relative_luminance
    public static double GetRelativeLuminance(this Color c)
    {
        var rSRGB = c.R / 255.0;
        var gSRGB = c.G / 255.0;
        var bSRGB = c.B / 255.0;

        var r = rSRGB <= 0.04045 ? rSRGB / 12.92 : Math.Pow(((rSRGB + 0.055) / 1.055), 2.4);
        var g = gSRGB <= 0.04045 ? gSRGB / 12.92 : Math.Pow(((gSRGB + 0.055) / 1.055), 2.4);
        var b = bSRGB <= 0.04045 ? bSRGB / 12.92 : Math.Pow(((bSRGB + 0.055) / 1.055), 2.4);
        return 0.2126 * r + 0.7152 * g + 0.0722 * b;
    }
}
