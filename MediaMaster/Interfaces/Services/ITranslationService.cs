using WinUI3Localizer;

namespace MediaMaster.Interfaces.Services;

public record LanguageItem(string Language, string UidKey);

/// <summary>
///     Service responsible for selecting and managing the application language.
/// </summary>
public interface ITranslationService
{
    ILocalizer? Localizer { get; }

    string DefaultLanguage { get; }

    Task SetLanguageAsync(string language);

    Task InitializeAsync();

    string GetCurrentLanguage();

    IEnumerable<string> GetAvailableLanguages();

    public event EventHandler<LanguageChangedEventArgs> LanguageChanged;
}