using MediaMaster.ViewModels;
using MediaMaster.Services;

namespace MediaMaster.Views;

public sealed partial class SettingsPage
{
    public SettingsViewModel ViewModel { get; }
    public SettingsService Settings { get; }

    public SettingsPage()
    {
        ViewModel = App.GetService<SettingsViewModel>();
        Settings = App.GetService<SettingsService>();
        InitializeComponent();
    }
}
