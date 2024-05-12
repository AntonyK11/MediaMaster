using Microsoft.UI.Xaml;
using MediaMaster.DataBase;
using MediaMaster.Interfaces.Services;
using MediaMaster.Services;
using BookmarksManager;
using Microsoft.UI.Xaml.Controls;


// To learn more about WinUI, the WinUI project structure,
// and more about our project templates, see: http://aka.ms/winui-project-info.

namespace MediaMaster.Views;

/// <summary>
///     An empty page that can be used on its own or navigated to within a Frame.
/// </summary>
public sealed partial class HomePage
{
    private ITeachingService TeachingService { get; }

    private BrowserService BrowserService { get; }

    //private readonly AsyncObservableCollection<DbEntities.Media> _collection;

    public HomePage()
    {
        TeachingService = App.GetService<ITeachingService>();
        BrowserService = App.GetService<BrowserService>();

        InitializeComponent();

        TeachingService.Configure(1, TeachingTip1);
        TeachingService.Configure(2, TeachingTip2);
        TeachingService.Configure(3, TeachingTip3);

        //_collection = new(DataBase.Medias.Local);
        //DataBase.Medias.Local.CollectionChanged += (_, args) =>
        //{
        //    if (args.Action == NotifyCollectionChangedAction.Remove)
        //    {
        //        _collection.Remove((DbEntities.Media)args.OldItems[0]!);
        //    }
        //    else
        //    {
        //        _collection.Add((DbEntities.Media)args.NewItems[0]!);
        //    }
        //};

        //MediasDataGrid.ItemsSource = DataBase.Medias.Local.ToObservableCollection();
        //CategoriesDataGrid.ItemsSource = DataBase.Categories.Local.ToObservableCollection();
        //ExtensionsDataGrid.ItemsSource = DataBase.Extensions.Local.ToObservableCollection();
    }

    private async void GetActiveTabs(object sender, RoutedEventArgs e)
    {
        await BrowserService.FindActiveTabs();
    }

    private async void GetBookmarks(object sender, RoutedEventArgs e)
    {
        BookmarksTree.ItemsSource = await BrowserService.GetBookmarks();
    }

    private async void AddData(object sender, RoutedEventArgs e)
    {
        //Media media = DataBase.CreateProxy<Media>(
        //    p =>
        //    {
        //        p.Name = TitleInputBox.Text;
        //        p.Type = "";
        //        p.FilePath = "";
        //    });
        //if (TagsList.SelectedItem != null)
        //{
        //    media.Tags.Add((Tag)TagsList.SelectedItem);
        //}

        //DataBase.Add(media);

        //var titleText = TitleInputBox.Text;

        //DateTime time = DateTime.Now;

        ////await Task.Run(async () =>
        ////{
        ////    await Media.AddMediaAsync(titleText);
        ////    //await DataBase.SaveChangesAsync();
        ////});
        //await Media.AddMediaAsync(titleText);

        //Debug.WriteLine(DateTime.Now - time);
        //MediasList.ItemsSource = DataBase.Medias.Local.ToArray();
    }

    private async void RemoveData(object sender, RoutedEventArgs e)
    {
        //if (MediasList.SelectedItem == null)
        //{
        //    return;
        //}
        await using (MediaDbContext dataBase = new())
        {
            dataBase.Medias.RemoveRange(dataBase.Medias);
            await dataBase.SaveChangesAsync();
        }
    }

    private async void Button_Click(object sender, RoutedEventArgs e)
    {
         //await DataBase.SetupMediaCategories();
    }
}

class BookmarksTemplateSelector : DataTemplateSelector
{
    public DataTemplate BookmarkFolder { get; set; }
    public DataTemplate BookmarkLink { get; set; }

    protected override DataTemplate SelectTemplateCore(object item)
    {
        return item is BookmarkFolder ? BookmarkFolder : BookmarkLink;
    }
}