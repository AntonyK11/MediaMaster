using MediaMaster.Interfaces.Services;

namespace MediaMaster.Services.Navigation;

public class MainNavigationViewService(MainNavigationService navigationService, IPageService pageService) : NavigationViewService(navigationService, pageService);
