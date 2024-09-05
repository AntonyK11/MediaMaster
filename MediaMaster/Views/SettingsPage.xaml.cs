using Windows.ApplicationModel;
using MediaMaster.Services;
using MediaMaster.ViewModels;

namespace MediaMaster.Views;

public partial class SettingsPage
{
    public SettingsPage()
    {
        ViewModel = App.GetService<SettingsViewModel>();
        Settings = App.GetService<SettingsService>();
        InitializeComponent();

        VersionSettingsControl.Description = string.Format("Version {0}.{1}.{2}.{3}",
            Package.Current.Id.Version.Major,
            Package.Current.Id.Version.Minor,
            Package.Current.Id.Version.Build,
            Package.Current.Id.Version.Revision);
    }

    public SettingsViewModel ViewModel { get; }

    public SettingsService Settings { get; }
}