using MediaMaster.Helpers;
using MediaMaster.Interfaces.Services;
using MediaMaster.Views;

namespace MediaMaster.Services.Navigation;

public class NavigationViewService(INavigationService navigationService, IPageService pageService)
    : INavigationViewService
{
    private NavigationView? _navigationView;

    public IList<object>? MenuItems => _navigationView?.MenuItems;

    public object? SettingsItem => _navigationView?.SettingsItem;

    public void Initialize(NavigationView navigationView)
    {
        _navigationView = navigationView;
        _navigationView.BackRequested += OnBackRequested;
        _navigationView.ItemInvoked += OnItemInvoked;
    }

    public void UnregisterEvents()
    {
        if (_navigationView == null) return;

        _navigationView.BackRequested -= OnBackRequested;
        _navigationView.ItemInvoked -= OnItemInvoked;
    }

    public NavigationViewItem? GetSelectedItem(Type pageType)
    {
        if (_navigationView == null) return null;

        return GetSelectedItem(_navigationView.MenuItems, pageType) ??
               GetSelectedItem(_navigationView.FooterMenuItems, pageType);
    }

    private void OnBackRequested(NavigationView sender, NavigationViewBackRequestedEventArgs args)
    {
        navigationService.GoBack();
    }

    private void OnItemInvoked(NavigationView sender, NavigationViewItemInvokedEventArgs args)
    {
        if (args.IsSettingsInvoked)
        {
            navigationService.NavigateTo(typeof(SettingsPage).FullName);
        }
        else
        {
            var selectedItem = args.InvokedItemContainer as NavigationViewItem;

            if (selectedItem?.GetValue(NavigationHelper.NavigateToProperty) is string pageKey)
            {
                navigationService.NavigateTo(pageKey);
            }
        }
    }

    private NavigationViewItem? GetSelectedItem(IEnumerable<object> menuItems, Type pageType)
    {
        foreach (NavigationViewItem item in menuItems.OfType<NavigationViewItem>())
        {
            if (IsMenuItemForPageType(item, pageType))
            {
                return item;
            }

            NavigationViewItem? selectedChild = GetSelectedItem(item.MenuItems, pageType);
            if (selectedChild != null)
            {
                return selectedChild;
            }
        }

        return null;
    }

    private bool IsMenuItemForPageType(NavigationViewItem menuItem, Type sourcePageType)
    {
        if (menuItem.GetValue(NavigationHelper.NavigateToProperty) is string pageKey)
        {
            return pageService.GetPageType(pageKey) == sourcePageType;
        }

        return false;
    }
}