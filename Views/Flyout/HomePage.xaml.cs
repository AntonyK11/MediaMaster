using MediaMaster.Services;
using Microsoft.UI.Xaml;
using System.Diagnostics;
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

        foreach (IStorageItem? item in items)
        {
            Debug.WriteLine(item.Path);
        }
    }

}

