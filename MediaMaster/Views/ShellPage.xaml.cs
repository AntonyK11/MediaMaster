using MediaMaster.Services;
using MediaMaster.ViewModels;
using WinUI3Localizer;

namespace MediaMaster.Views;

public sealed partial class ShellPage
{
    private ShellViewModel ViewModel;

    public ShellPage(ShellViewModel viewModel)
    {
        ViewModel = viewModel;
        InitializeComponent();

        NavView.IsPaneOpen = false;

        ViewModel.NavigationService.Frame = ContentFrame;
        ViewModel.NavigationViewService.Initialize(NavView);

        App.GetService<InAppNotificationService>().RegisterStackedNotificationsBehavior(NotificationQueue);

        Loaded += (_, _) =>
        {
            if (NavView.SettingsItem is NavigationViewItem settingsItem)
            {
                Uids.SetUid(settingsItem, "ShellPage_SettingsNavigationItem");
            }
        };
    }

    private void OnDisplayModeChanged(NavigationView sender, NavigationViewDisplayModeChangedEventArgs args)
    {
        AppTitleBar.SetLeftMargin(args.DisplayMode == NavigationViewDisplayMode.Minimal ? 88 : 48);
    }
}