using Windows.ApplicationModel.DataTransfer;
using Windows.Storage;
using MediaMaster.Services;
using MediaMaster.Services.Navigation;
using Microsoft.UI.Xaml.Media.Animation;

namespace MediaMaster.Views.Flyout;

public partial class HomePage
{
    private BrowserService BrowserService { get; }

    private readonly DispatcherTimer _timer = new() { Interval = TimeSpan.FromSeconds(1) };

    public HomePage()
    {
        InitializeComponent();
        BrowserService = App.GetService<BrowserService>();
        TabsList.ItemsSource = BrowserService.ActiveBrowserTabs;
        TabsList.ItemContainerTransitions = null;
        App.Flyout!.VisibilityChanged += Flyout_VisibilityChanged;
        _timer.Tick += Timer_Tick;
        _timer.Start();
    }

    ~HomePage()
    {
        _timer.Stop();
    }

    private async void Timer_Tick(object? sender, object e)
    {
        await BrowserService.FindActiveTabs();
    }

    private void Flyout_VisibilityChanged(object sender, bool visible)
    {
        if (visible)
        {
            _timer.Start();
        }
        else
        {
            _timer.Stop();
        }
    }

    private void OnDragOver(object sender, DragEventArgs e)
    {
        if (!e.DataView.Contains(StandardDataFormats.StorageItems)) return;
        e.AcceptedOperation = DataPackageOperation.Link;
    }

    private async void OnDrop(object sender, DragEventArgs e)
    {
        if (!e.DataView.Contains(StandardDataFormats.StorageItems)) return;

        IReadOnlyList<IStorageItem>? items = await e.DataView.GetStorageItemsAsync();
        App.GetService<FlyoutNavigationService>().NavigateTo(typeof(AddMediasPage).FullName!,
            items.Select(i => i.Path).ToList(),
            new SlideNavigationTransitionInfo() { Effect = SlideNavigationTransitionEffect.FromRight });
    }

    private void ButtonBase_OnClick(object sender, RoutedEventArgs e)
    {
        var tab = (BrowserTab?)TabsList.SelectedItem;
        if (tab == null) return;

        ICollection<KeyValuePair<string?, string>> browserTitleUrl = [new(tab.Title, tab.Url.AbsoluteUri)];
        App.GetService<FlyoutNavigationService>().NavigateTo(typeof(AddMediasPage).FullName!, browserTitleUrl,
            new SlideNavigationTransitionInfo() { Effect = SlideNavigationTransitionEffect.FromRight });
    }
}