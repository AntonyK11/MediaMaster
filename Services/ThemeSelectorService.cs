using Windows.Foundation;
using Windows.UI;
using Windows.UI.ViewManagement;
using CommunityToolkit.WinUI;
using MediaMaster.Interfaces.Services;
using Microsoft.UI.Xaml;

namespace MediaMaster.Services;

/// <summary>
///     Service responsible for selecting and managing the application theme.
/// </summary>
public class ThemeSelectorService : IThemeSelectorService
{
    public const string SettingsKey = "AppBackgroundTheme";

    private readonly UISettings _settings = new();

    public ThemeSelectorService()
    {
        _settings.ColorValuesChanged += (_, _) => Listener_SystemThemeChanged();
    }

    /// <summary>
    ///     Gets the current system theme (Dark or Light).
    /// </summary>
    public ElementTheme SystemTheme { get; private set; } = ElementTheme.Dark;

    /// <summary>
    ///     Gets the current theme of the service (Dark, Light or Default).
    /// </summary>
    public ElementTheme Theme { get; private set; } = ElementTheme.Default;

    /// <summary>
    ///     Gets the actual theme of the service (Dark or Light).
    /// </summary>
    public ElementTheme ActualTheme { get; private set; } = ElementTheme.Dark;

    public async Task InitializeAsync()
    {
        SystemTheme = GetSystemTheme();
        Theme = await LoadThemeFromSettingsAsync();
        ActualTheme = GetActualTheme();
    }

    /// <summary>
    ///     Event handler for when the actual theme is changed. Passes the new actual theme as an argument (Dark or Light).
    /// </summary>
    public event TypedEventHandler<object, ElementTheme>? ThemeChanged;

    public async Task SetThemeAsync(ElementTheme theme)
    {
        if (Theme == theme) return;

        Theme = theme;
        ActualTheme = GetActualTheme();
        await SettingsService.SaveSettingAsync(SettingsKey, theme);

        OnThemeChange();
    }

    /// <summary>
    ///     Determines whether the specified color is considered "light".
    /// </summary>
    /// <param name="clr"> The color to check. </param>
    /// <returns>
    ///     True if the color is considered "light"; otherwise, false.
    /// </returns>
    private static bool IsColorLight(Color clr)
    {
        return 5 * clr.G + 2 * clr.R + clr.B > 8 * 128;
    }

    /// <summary>
    ///     Gets the system theme based on the background color (Dark or Light).
    /// </summary>
    /// <returns> The system theme (Dark or Light). </returns>
    private ElementTheme GetSystemTheme()
    {
        var isLight = IsColorLight(_settings.GetColorValue(UIColorType.Background));
        return isLight ? ElementTheme.Light : ElementTheme.Dark;
    }

    /// <summary>
    ///     Gets the actual theme based on the current theme setting (Dark or Light).
    /// </summary>
    /// <returns> The actual theme of the service (Dark or Light). </returns>
    private ElementTheme GetActualTheme()
    {
        return Theme == ElementTheme.Default ? SystemTheme : Theme;
    }

    private static async Task<ElementTheme> LoadThemeFromSettingsAsync()
    {
        return await SettingsService.ReadSettingAsync<ElementTheme>(SettingsKey);
    }

    private void Listener_SystemThemeChanged()
    {
        SystemTheme = GetSystemTheme();
        ElementTheme nextActualTheme = GetActualTheme();

        if (Theme != ElementTheme.Default || ActualTheme == nextActualTheme) return;

        ActualTheme = nextActualTheme;
        OnThemeChange();
    }

    /// <summary>
    ///     Invokes the theme changed event.
    /// </summary>
    /// <param name="actualTheme"> The actual theme to be used (Dark or Light). </param>
    private void OnThemeChange()
    {
        App.DispatcherQueue.EnqueueAsync(() => ThemeChanged?.Invoke(this, ActualTheme));
    }
}