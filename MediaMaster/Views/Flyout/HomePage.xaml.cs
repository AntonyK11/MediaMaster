using Windows.ApplicationModel.DataTransfer;
using Windows.Storage;
using MediaMaster.DataBase;
using MediaMaster.Services;

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
        await Task.Run(() => MediaService.AddMediaAsync(items.Select(i => i.Path)).ConfigureAwait(false));
    }

    private async void ButtonBase_OnClick(object sender, RoutedEventArgs e)
    {
        var tab = (BrowserTab?)TabsList.SelectedItem;
        if (tab == null) return;

        await MediaService.AddMediaAsync([new KeyValuePair<string?, string>(tab.Title, tab.Url.AbsoluteUri)]);
    }
}