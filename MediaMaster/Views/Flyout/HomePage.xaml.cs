using MediaMaster.DataBase;
using MediaMaster.Services;
using Windows.ApplicationModel.DataTransfer;
using Windows.Storage;

namespace MediaMaster.Views.Flyout;

public sealed partial class HomePage
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
        e.AcceptedOperation = DataPackageOperation.Link;
    }

    private async void OnDrop(object sender, DragEventArgs e)
    {
        if (!e.DataView.Contains(StandardDataFormats.StorageItems)) return;

        IReadOnlyList<IStorageItem>? items = await e.DataView.GetStorageItemsAsync();

        Debug.WriteLine("Adding files");
        DateTime time = DateTime.Now;

        foreach (IStorageItem? item in items)
        {
            Debug.WriteLine(item.Path);
        }

        await Task.Run(() => MediaService.AddMediaAsync(items.Select(i => i.Path)).ConfigureAwait(false));
        Debug.WriteLine(DateTime.Now - time);
    }

    private async void ButtonBase_OnClick(object sender, RoutedEventArgs e)
    {
        var tab = (BrowserTab?)TabsList.SelectedItem;
        if (tab == null) return;

        var title = tab.Title;
        var tabEndingString = tab.Browser.TabEndingString;
        if (title.EndsWith(tabEndingString))
        {
            title = title.Remove(title.Length - tabEndingString.Length, tabEndingString.Length);
        }

        await MediaService.AddMediaAsync([new KeyValuePair<string?, string>(title, tab.Url.AbsoluteUri)]);
    }
}

