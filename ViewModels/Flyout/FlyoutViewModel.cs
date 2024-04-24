using CommunityToolkit.Mvvm.ComponentModel;

namespace MediaMaster.ViewModels.Flyout;

public partial class FlyoutViewModel : ObservableObject
{
    [ObservableProperty] private bool _windows10 = Environment.OSVersion.Version.Build < 22000;
}