using System.Windows.Input;
using Windows.ApplicationModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using MediaMaster.Interfaces.Services;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using WinUI3Localizer;

namespace MediaMaster.ViewModels;

public partial class SettingsViewModel : ObservableObject
{
    private readonly ITranslationService _translationService;

    public readonly ICommand SwitchThemeCommand;

    [ObservableProperty] private List<LanguageItem> _availableLanguages;

    [ObservableProperty] private bool _canOpenOnWindowsStartup;

    [ObservableProperty] private ElementTheme _elementTheme;

    [ObservableProperty] private bool _openOnWindowsStartup;

    [ObservableProperty] private LanguageItem _selectedLanguage;

    [ObservableProperty] private string _versionDescription;

    public SettingsViewModel(IThemeSelectorService themeSelectorService, ITranslationService translationService)
    {
        _translationService = translationService;

        ElementTheme = themeSelectorService.Theme;
        VersionDescription = GetVersionDescription();

        IEnumerable<string> availableLanguages = _translationService.GetAvailableLanguages();

        AvailableLanguages = availableLanguages
            .Select(x => new LanguageItem(x, $"{nameof(MainWindow)}_{x}"))
            .ToList();

        var currentLanguage = _translationService.GetCurrentLanguage();

        SelectedLanguage = new LanguageItem(currentLanguage,
            $"{nameof(MainWindow)}_{currentLanguage}");

        SwitchThemeCommand = new RelayCommand<ElementTheme>(
            async param =>
            {
                await themeSelectorService.SetThemeAsync(param);
                ElementTheme = themeSelectorService.Theme;
            });

        _ = DetectOpenFilesAtStartupAsync();
    }

    private static string GetVersionDescription()
    {
        PackageVersion packageVersion = Package.Current.Id.Version;
        Version version = new(packageVersion.Major, packageVersion.Minor, packageVersion.Build,
            packageVersion.Revision);

        return
            $"{"AppDisplayName".GetLocalizedString()} - {version.Major}.{version.Minor}.{version.Build}.{version.Revision} {AppDomain.CurrentDomain.BaseDirectory}";
    }

    public async void SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (e.AddedItems.FirstOrDefault() is not LanguageItem languageItem)
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

    public async Task DetectOpenFilesAtStartupAsync(StartupTask? startupTask = null)
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