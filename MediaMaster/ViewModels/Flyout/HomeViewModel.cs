using CommunityToolkit.Mvvm.ComponentModel;

namespace MediaMaster.ViewModels.Flyout;

public sealed partial class HomeViewModel : ObservableObject
{
    [ObservableProperty] public partial bool IsBackEnabled { get; set; }

    [ObservableProperty] public partial object? Selected { get; set; }
}