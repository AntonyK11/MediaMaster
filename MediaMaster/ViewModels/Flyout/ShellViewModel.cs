using CommunityToolkit.Mvvm.ComponentModel;
using MediaMaster.Interfaces.Services;
using MediaMaster.Services.Navigation;
using Microsoft.UI.Xaml.Navigation;

namespace MediaMaster.ViewModels.Flyout;

public partial class ShellViewModel : ObservableObject
{
    [ObservableProperty] private bool _isBackEnabled;

    [ObservableProperty] private bool _isFocused;

    [ObservableProperty] private object? _selected;

    public ShellViewModel(FlyoutNavigationService flyoutNavigationService)
    {
        NavigationService = flyoutNavigationService;
        NavigationService.Navigated += OnNavigated;
    }

    public INavigationService NavigationService { get; }

    private void OnNavigated(object sender, NavigationEventArgs args)
    {
        IsBackEnabled = NavigationService.CanGoBack;
    }
}