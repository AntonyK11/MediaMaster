using CommunityToolkit.Mvvm.ComponentModel;

namespace MediaMaster.ViewModels.Flyout;

public sealed partial class HomeViewModel : ObservableObject
{
    [ObservableProperty] private bool _isBackEnabled;

    [ObservableProperty] private object? _selected;
}