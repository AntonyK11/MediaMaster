using MediaMaster.Interfaces.Services;
using MediaMaster.Services;

namespace MediaMaster.Views;

public partial class CategoriesPage
{
    public CategoriesPage()
    {
        TeachingService = App.GetService<ITeachingService>();
        BrowserService = App.GetService<BrowserService>();

        InitializeComponent();

        TeachingService.Configure(1, TeachingTip1);
        TeachingService.Configure(2, TeachingTip2);
        TeachingService.Configure(3, TeachingTip3);
    }

    private ITeachingService TeachingService { get; }

    private BrowserService BrowserService { get; }

    private async void GetActiveTabs(object sender, RoutedEventArgs e)
    {
        await BrowserService.FindActiveTabs();
    }
}