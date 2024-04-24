using CommunityToolkit.Mvvm.ComponentModel;
using MediaMaster.Interfaces.Services;
using MediaMaster.Services.Navigation;
using Microsoft.UI.Xaml.Navigation;

namespace MediaMaster.ViewModels.Flyout;

public partial class ShellViewModel : ObservableObject
{
    [ObservableProperty] private bool _isBackEnabled;

    [ObservableProperty] private object? _selected;

    [ObservableProperty] private bool _isFocused;

    public INavigationService NavigationService { get; }

    public ShellViewModel(FlyoutNavigationService flyoutNavigationService)
    {
        NavigationService = flyoutNavigationService;
        NavigationService.Navigated += OnNavigated;
    }

    private void OnNavigated(object sender, NavigationEventArgs args)
    {
        IsBackEnabled = NavigationService.CanGoBack;
    }
}