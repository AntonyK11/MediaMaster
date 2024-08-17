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

    private async void GetBookmarks(object sender, RoutedEventArgs e)
    {
        BookmarksTree.ItemsSource = await BrowserService.GetBookmarks();
    }
}

internal partial class BookmarksTemplateSelector : DataTemplateSelector
{
    public DataTemplate BookmarkFolder { get; set; }
    public DataTemplate BookmarkLink { get; set; }

    protected override DataTemplate SelectTemplateCore(object item)
    {
        return item is BookmarksManager.BookmarkFolder ? BookmarkFolder : BookmarkLink;
    }
}