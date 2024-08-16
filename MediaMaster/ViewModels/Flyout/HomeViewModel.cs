using CommunityToolkit.Mvvm.ComponentModel;

namespace MediaMaster.ViewModels.Flyout;

public partial class HomeViewModel : ObservableObject
{
    [ObservableProperty] private bool _isBackEnabled;

    [ObservableProperty] private object? _selected;
}