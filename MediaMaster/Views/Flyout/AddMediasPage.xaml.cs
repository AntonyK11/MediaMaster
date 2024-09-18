using MediaMaster.Services.Navigation;
using MediaMaster.ViewModels.Flyout;

namespace MediaMaster.Views.Flyout;

public sealed partial class AddMediasPage : Page
{
    public AddMediasViewModel ViewModel { get; }

    public AddMediasPage()
    {
        this.InitializeComponent();

        ViewModel = App.GetService<AddMediasViewModel>();
    }

    private void AddButton_OnClick(object sender, RoutedEventArgs e)
    {
        ViewModel.AddMedias(AddMediasDialog);
        App.GetService<FlyoutNavigationService>().GoBack();
    }
}

