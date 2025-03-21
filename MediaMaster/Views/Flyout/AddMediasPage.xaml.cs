using MediaMaster.Services;
using MediaMaster.Services.Navigation;
using MediaMaster.ViewModels.Flyout;
using Microsoft.UI.Xaml.Input;

namespace MediaMaster.Views.Flyout;

public sealed partial class AddMediasPage : Page
{
    public AddMediasViewModel ViewModel { get; }

    public AddMediasPage()
    {
        this.InitializeComponent();

        ViewModel = App.GetService<AddMediasViewModel>();

        Loaded += async (_, _) => await FocusManager.TryFocusAsync(AddButton, FocusState.Pointer);
    }

    private void AddButton_OnClick(object? sender = null, RoutedEventArgs? e = null)
    {
        ViewModel.AddMedias(AddMediasDialog);
        App.GetService<TasksService>().FlyoutTaskAdded += NewFlyoutTask;
    }

    private static void NewFlyoutTask(object sender, object? args)
    {
        App.GetService<TasksService>().FlyoutTaskAdded -= NewFlyoutTask;
        if (App.Flyout?.AutoClose == true)
        {
            App.Flyout.CloseFlyout();
        }
        else
        {
            App.GetService<FlyoutNavigationService>().GoBack();
        }
    }
}

