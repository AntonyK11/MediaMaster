using System.Windows.Input;
using Windows.ApplicationModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using MediaMaster.Interfaces.Services;
using WinUI3Localizer;

namespace MediaMaster.ViewModels;

public sealed partial class SettingsViewModel : ObservableObject
{
    private readonly ITranslationService _translationService;

    public readonly ICommand SwitchThemeCommand;

    [ObservableProperty] public partial ICollection<LanguageItem> AvailableLanguages { get; set; }

    [ObservableProperty] public partial bool CanOpenOnWindowsStartup { get; set; }

    [ObservableProperty] public partial ElementTheme ElementTheme { get; set; }

    [ObservableProperty] public partial bool OpenOnWindowsStartup { get; set; }

    [ObservableProperty] public partial LanguageItem SelectedLanguage { get; set; }

    [ObservableProperty] public partial string VersionDescription { get; set; }

    public SettingsViewModel(IThemeSelectorService themeSelectorService, ITranslationService translationService)
    {
        _translationService = translationService;

        ElementTheme = themeSelectorService.Theme;
        VersionDescription = GetVersionDescription();

        IEnumerable<string> availableLanguages = _translationService.GetAvailableLanguages();

        AvailableLanguages = availableLanguages
            .Select(language => new LanguageItem(language, $"/Settings/{language}"))
            .ToList();

        var currentLanguage = _translationService.GetCurrentLanguage();

        SelectedLanguage = new LanguageItem(currentLanguage, $"/Settings/{currentLanguage}");

        SwitchThemeCommand = new RelayCommand<ElementTheme>(
            param =>
            {
                ElementTheme = param;
                themeSelectorService.SetThemeAsync(param);
            });

        _ = DetectOpenFilesAtStartupAsync();
    }

    private static string GetVersionDescription()
    {
        PackageVersion packageVersion = Package.Current.Id.Version;
        Version version = new(
            packageVersion.Major,
            packageVersion.Minor,
            packageVersion.Build,
            packageVersion.Revision);

        return
            $"{"AppDisplayName".GetLocalizedString()} - {version.Major}.{version.Minor}.{version.Build}.{version.Revision} {AppDomain.CurrentDomain.BaseDirectory}";
    }

    public async void SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (e.AddedItems.FirstOrDefault() is not LanguageItem languageItem ||
            _translationService.GetCurrentLanguage() == languageItem.Language)
        {
            return;
        }

        await _translationService.SetLanguageAsync(languageItem.Language);

        VersionDescription = GetVersionDescription();
    }


    // https://github.com/files-community/Files/blob/main/src/Files.App/ViewModels/Settings/AdvancedViewModel.cs#L314
    public async void OpenOnWindowsStartupCommand(object sender, RoutedEventArgs e)
    {
        StartupTask startupTask = await StartupTask.GetAsync("F54B70A2-2258-4043-BF53-7BDB05A35A3B");

        var state = startupTask.State switch
        {
            StartupTaskState.Enabled => true,
            StartupTaskState.EnabledByPolicy => true,
            StartupTaskState.DisabledByPolicy => false,
            StartupTaskState.DisabledByUser => false,
            _ => false
        };

        if (state == OpenOnWindowsStartup) return;


        if (OpenOnWindowsStartup)
        {
            await startupTask.RequestEnableAsync();
        }
        else
        {
            startupTask.Disable();
        }

        await DetectOpenFilesAtStartupAsync(startupTask);
    }

    private async Task DetectOpenFilesAtStartupAsync(StartupTask? startupTask = null)
    {
        if (startupTask == null)
        {
            startupTask = await StartupTask.GetAsync("F54B70A2-2258-4043-BF53-7BDB05A35A3B");
        }

        StartupTaskState stateMode = startupTask.State;

        switch (stateMode)
        {
            case StartupTaskState.Disabled:
                CanOpenOnWindowsStartup = true;
                OpenOnWindowsStartup = false;
                break;
            case StartupTaskState.Enabled:
                CanOpenOnWindowsStartup = true;
                OpenOnWindowsStartup = true;
                break;
            case StartupTaskState.DisabledByPolicy:
                CanOpenOnWindowsStartup = false;
                OpenOnWindowsStartup = false;
                break;
            case StartupTaskState.DisabledByUser:
                CanOpenOnWindowsStartup = false;
                OpenOnWindowsStartup = false;
                break;
            case StartupTaskState.EnabledByPolicy:
                CanOpenOnWindowsStartup = false;
                OpenOnWindowsStartup = true;
                break;
        }
    }
}