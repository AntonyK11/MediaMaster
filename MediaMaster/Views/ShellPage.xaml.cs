using Microsoft.UI.Xaml.Controls;
using MediaMaster.ViewModels;

namespace MediaMaster.Views;

public sealed partial class ShellPage
{
    private ShellViewModel ViewModel;

    //public TaskbarIcon TrayIcon;

    public ShellPage(ShellViewModel viewModel)
    {
        ViewModel = viewModel;
        InitializeComponent();

        NavView.PaneDisplayMode = NavView.PaneDisplayMode; // fixes the TogglePaneButton being cut off

        ViewModel.NavigationService.Frame = ContentFrame;
        ViewModel.NavigationViewService.Initialize(NavView);

        //TrayIcon = TrayIconView.TrayIcon;
    }

    private void OnDisplayModeChanged(NavigationView sender, NavigationViewDisplayModeChangedEventArgs args)
    {
        AppTitleBar.SetLeftMargin(args.DisplayMode == NavigationViewDisplayMode.Minimal ? 88 : 48);
    }

    //private void BackRequested(NavigationView sender, NavigationViewBackRequestedEventArgs args)
    //{
    //    if (ContentFrame.CanGoBack)
    //    {
    //        ContentFrame.GoBack();
    //    }
    //}

    //private new void Loaded(object sender, RoutedEventArgs e)
    //{
    //    AddMenuItem("SettingsPage_PrivacyTermsLink", Symbol.Home, "MediaMaster.Views.HomePage");
    //    NavView.MenuItems.Add(new NavigationViewItemSeparator());
    //    AddMenuItem("SettingsPage_PrivacyTermsLink", Symbol.AllApps, "MediaMaster.Views.CategoriesPage");
    //    NavView.MenuItems.Add(new NavigationViewItem
    //    {
    //        Content = "Apps",
    //        Icon = new SymbolIcon((Symbol)0xEB3C),
    //        Tag = "MediaMaster.AppsPage"
    //    });
    //    NavView.MenuItems.Add(new NavigationViewItem
    //    {
    //        Content = "Games",
    //        Icon = new SymbolIcon((Symbol)0xE7FC),
    //        Tag = "MediaMaster.GamesPage"
    //    });
    //    NavView.MenuItems.Add(new NavigationViewItem
    //    {
    //        Content = "Music",
    //        Icon = new SymbolIcon(Symbol.Audio),
    //        Tag = "MediaMaster.MusicPage"
    //    });
    //    NavView.MenuItems.Add(new NavigationViewItemSeparator());
    //    NavView.MenuItems.Add(new NavigationViewItem
    //    {
    //        Content = "My content",
    //        Icon = new SymbolIcon((Symbol)0xF1AD),
    //        Tag = "MediaMaster.MyContentPage"
    //    });

    //    ViewModel.SelectedItem = NavView.MenuItems[0]; // set the first item as the selected item
    //    NavView.PaneDisplayMode = NavView.PaneDisplayMode; // fixes the TogglePaneButton being cut off

    //    Uids.SetUid((NavigationViewItem)NavView.SettingsItem, "SettingsPage_PrivacyTermsLink");
    //}

    //private void AddMenuItem(string udi, Symbol symbol, string tag)
    //{
    //    NavigationViewItem item = new()
    //    {
    //        Icon = new SymbolIcon(symbol),
    //        Tag = tag
    //    };
    //    Uids.SetUid(item, udi);
    //    NavView.MenuItems.Add(item);
    //}

    //private void SelectionChanged(NavigationView sender, NavigationViewSelectionChangedEventArgs args)
    //{
    //    Type? navPageType = null;
    //    Type preNavPageType = ContentFrame.CurrentSourcePageType;

    //    if (args.IsSettingsSelected)
    //    {
    //        navPageType = typeof(SettingsPage);
    //    }
    //    else if (args.SelectedItemContainer != null)
    //    {
    //        navPageType = Type.GetType(args.SelectedItemContainer.Tag.ToString()!);
    //    }


    //    if (navPageType is not null && preNavPageType != navPageType)
    //    {
    //        ContentFrame.Navigate(navPageType, null, args.RecommendedNavigationTransitionInfo);
    //    }
    //}

    //private void On_Navigated(object sender, NavigationEventArgs e)
    //{
    //    ViewModel.IsBackEnabled = ContentFrame.CanGoBack;

    //    if (e.SourcePageType == typeof(SettingsPage))
    //    {
    //        ViewModel.SelectedItem = (NavigationViewItem)NavView.SettingsItem;
    //    }
    //    else if (e.SourcePageType != null)
    //    {
    //        ViewModel.SelectedItem = NavView.MenuItems
    //            .OfType<NavigationViewItem>()
    //            .First(i => i.Tag.Equals(e.SourcePageType.FullName!));
    //    }
    //}
}