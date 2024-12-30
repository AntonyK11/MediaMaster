using CommunityToolkit.Mvvm.ComponentModel;
using MediaMaster.Interfaces.Services;
using MediaMaster.Services.Navigation;
using MediaMaster.Views;
using Microsoft.UI.Xaml.Navigation;

namespace MediaMaster.ViewModels;

public sealed partial class ShellViewModel : ObservableObject
{
    [ObservableProperty] public partial bool IsBackEnabled { get; set; }

    [ObservableProperty] public partial object? Selected { get; set; }

    public ShellViewModel(MainNavigationService mainNavigationService,
        MainNavigationViewService mainNavigationViewService)
    {
        NavigationService = mainNavigationService;
        NavigationService.Navigated += OnNavigated;
        NavigationViewService = mainNavigationViewService;
    }

    public INavigationService NavigationService { get; }

    public INavigationViewService NavigationViewService { get; }

    private void OnNavigated(object sender, NavigationEventArgs args)
    {
        IsBackEnabled = NavigationService.CanGoBack;

        if (args.SourcePageType == typeof(SettingsPage))
        {
            Selected = NavigationViewService.SettingsItem;
            return;
        }

        NavigationViewItem? selectedItem = NavigationViewService.GetSelectedItem(args.SourcePageType);
        if (selectedItem != null)
        {
            Selected = selectedItem;
        }
    }
}