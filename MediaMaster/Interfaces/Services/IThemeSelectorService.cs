using Windows.Foundation;

namespace MediaMaster.Interfaces.Services;

/// <summary>
///     Service responsible for selecting and managing the application theme.
/// </summary>
public interface IThemeSelectorService
{
    /// <summary>
    ///     Gets the current system theme (Dark or Light).
    /// </summary>
    ElementTheme SystemTheme { get; }

    /// <summary>
    ///     Gets the current theme of the service (Dark, Light or Default).
    /// </summary>
    ElementTheme Theme { get; }

    /// <summary>
    ///     Gets the actual theme of the service (Dark or Light).
    /// </summary>
    ElementTheme ActualTheme { get; }

    Task InitializeAsync();

    Task SetThemeAsync(ElementTheme theme);

    /// <summary>
    ///     Event handler for when the actual theme is changed. Passes the new actual theme as an argument (Dark or Light).
    /// </summary>
    public event TypedEventHandler<object, ElementTheme> ThemeChanged;
}