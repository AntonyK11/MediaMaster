using CommunityToolkit.Mvvm.ComponentModel;
using MediaMaster.Interfaces.Services;

namespace MediaMaster.ViewModels.Flyout;

public partial class HomeViewModel : ObservableObject
{
    [ObservableProperty] private bool _isBackEnabled;

    [ObservableProperty] private object? _selected;

    public INavigationService NavigationService { get; }
}