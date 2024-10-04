using MediaMaster.Interfaces.Services;

namespace MediaMaster.Services.Navigation;

public sealed class MainNavigationViewService(MainNavigationService navigationService, IPageService pageService)
    : NavigationViewService(navigationService, pageService);