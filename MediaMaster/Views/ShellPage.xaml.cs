using MediaMaster.DataBase;
using MediaMaster.Services;
using MediaMaster.ViewModels;
using Windows.ApplicationModel.DataTransfer;
using Windows.Storage;
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

    private void OnDragOver(object sender, DragEventArgs e)
    {
        if (!e.DataView.Contains(StandardDataFormats.StorageItems)) return;
        e.AcceptedOperation = DataPackageOperation.Link;
    }

    private async void OnDrop(object sender, DragEventArgs e)
    {
        if (!e.DataView.Contains(StandardDataFormats.StorageItems)) return;
        DropGrid.Visibility = Visibility.Collapsed;

        IReadOnlyList<IStorageItem>? items = await e.DataView.GetStorageItemsAsync();
        await Task.Run(() => MediaService.AddMediaAsync(items.Select(i => i.Path)).ConfigureAwait(false));
    }

    private async void UIElement_OnDragEnter(object sender, DragEventArgs e)
    {
        if (!e.DataView.Contains(StandardDataFormats.StorageItems)) return;
        await Task.Yield();
        DropGrid.Visibility = Visibility.Visible;
    }

    private async void UIElement_OnDragLeave(object sender, DragEventArgs e)
    {
        await Task.Yield();
        DropGrid.Visibility = Visibility.Collapsed;
    }
}