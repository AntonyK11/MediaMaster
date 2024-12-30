using CommunityToolkit.Mvvm.ComponentModel;
using MediaMaster.Interfaces.Services;
using MediaMaster.Services.Navigation;
using Microsoft.UI.Xaml.Navigation;

namespace MediaMaster.ViewModels.Flyout;

public sealed partial class ShellViewModel : ObservableObject
{
    [ObservableProperty] public partial bool IsBackEnabled { get; set; }

    [ObservableProperty] public partial bool IsFocused { get; set; }

    [ObservableProperty] public partial object? Selected { get; set; }

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