using Windows.ApplicationModel.DataTransfer;
using MediaMaster.Interfaces.Services;
using MediaMaster.ViewModels;
using MediaMaster.Views.Dialog;
using WinUI3Localizer;

namespace MediaMaster.Views;

public partial class ShellPage
{
    private readonly ShellViewModel ViewModel;

    private readonly ITeachingService TeachingService;

    public ShellPage(ShellViewModel viewModel, ITeachingService teachingService)
    {
        ViewModel = viewModel;
        TeachingService = teachingService;

        InitializeComponent();

        ViewModel.NavigationService.Frame = ContentFrame;
        ViewModel.NavigationViewService.Initialize(NavView);

        TeachingService.Configure(1, TeachingTip1);

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

        ICollection<string> mediaPaths = (await e.DataView.GetStorageItemsAsync()).Select(i => i.Path).ToList();
        await AddMediasDialog.ShowDialogAsync(mediaPaths);
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