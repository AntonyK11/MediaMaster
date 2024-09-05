using Windows.Storage;
using MediaMaster.Helpers;
using MediaMaster.Interfaces.Services;
using Microsoft.Windows.Globalization;
using WinUI3Localizer;

namespace MediaMaster.Services;

/// <summary>
///     Service responsible for selecting and managing the application language.
/// </summary>
public class TranslationService : ITranslationService
{
    private const string SettingsKey = "Language";

    public ILocalizer? Localizer { get; private set; }

    public string DefaultLanguage => "en-US";

    public async Task InitializeAsync()
    {
        if (Localizer is not null) return;

        // Initialize a "Strings" folder in the executables folder.
        var stringsFolderPath = Path.Combine(AppContext.BaseDirectory, "Strings");
        StorageFolder stringsFolder = await StorageFolder.GetFolderFromPathAsync(stringsFolderPath);

        Localizer = await new LocalizerBuilder()
            .AddStringResourcesFolderForLanguageDictionaries(stringsFolder.Path)
            .SetOptions(options => { options.DefaultLanguage = DefaultLanguage; })
            .Build();

        var lang = await SettingsService.ReadSettingAsync(SettingsKey, SourceGenerationContext.Default.String);
        if (lang is null)
        {
            await SettingsService.SaveSettingAsync(SettingsKey, DefaultLanguage, SourceGenerationContext.Default.String);
            lang = DefaultLanguage;
        }

        ApplicationLanguages.PrimaryLanguageOverride = lang;
        await Localizer.SetLanguage(lang);

        Localizer.LanguageChanged += (_, args) => { OnLanguageChanged(args); };
    }

    public async Task SetLanguageAsync(string language)
    {
        if (Localizer is null) return;

        await Localizer.SetLanguage(language);
        ApplicationLanguages.PrimaryLanguageOverride = language;

        await SettingsService.SaveSettingAsync(SettingsKey, language, SourceGenerationContext.Default.String);
    }

    public string GetCurrentLanguage()
    {
        return Localizer is null ? DefaultLanguage : Localizer.GetCurrentLanguage();
    }

    public IEnumerable<string> GetAvailableLanguages()
    {
        return Localizer is null ? [] : Localizer.GetAvailableLanguages();
    }

    public event EventHandler<LanguageChangedEventArgs>? LanguageChanged;

    private void OnLanguageChanged(LanguageChangedEventArgs args)
    {
        LanguageChanged?.Invoke(this, args);
    }
}