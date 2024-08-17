using MediaMaster.Interfaces.Services;
using MediaMaster.Services;

namespace MediaMaster.Views;

public sealed partial class CategoriesPage
{
    private ITeachingService TeachingService { get; }

    private BrowserService BrowserService { get; }

    public CategoriesPage()
    {
        TeachingService = App.GetService<ITeachingService>();
        BrowserService = App.GetService<BrowserService>();

        this.InitializeComponent();

        TeachingService.Configure(1, TeachingTip1);
        TeachingService.Configure(2, TeachingTip2);
        TeachingService.Configure(3, TeachingTip3);
    }

    private async void GetActiveTabs(object sender, RoutedEventArgs e)
    {
        await BrowserService.FindActiveTabs();
    }
}