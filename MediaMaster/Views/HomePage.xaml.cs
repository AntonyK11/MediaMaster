using Microsoft.UI.Xaml;
using MediaMaster.DataBase;
using MediaMaster.Interfaces.Services;
using MediaMaster.Services;
using BookmarksManager;
using MediaMaster.DataBase.Models;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Input;


// To learn more about WinUI, the WinUI project structure,
// and more about our project templates, see: http://aka.ms/winui-project-info.

namespace MediaMaster.Views;

/// <summary>
///     An empty page that can be used on its own or navigated to within a Frame.
/// </summary>
public sealed partial class HomePage : Page
{
    private ITeachingService TeachingService { get; }

    private BrowserService BrowserService { get; }

    //private readonly AsyncObservableCollection<DbEntities.Media> _collection;

    public HomePage()
    {
        TeachingService = App.GetService<ITeachingService>();
        BrowserService = App.GetService<BrowserService>();

        this.InitializeComponent();

        TeachingService.Configure(1, TeachingTip1);
        TeachingService.Configure(2, TeachingTip2);
        TeachingService.Configure(3, TeachingTip3);

        //var media = new Media 
        //{ 
        //    Name = "Raptor_Dimorphism",
        //    FilePath = @"C:\Users\Antony\Downloads\ovisetup.exe"
        //};

        //for (int i = 0;  i < 50; i++)
        //{
        //    var tag = new Tag
        //    {
        //        Name = $"hello {i}"
        //    };
        //    media.Tags.Add(tag);
        //}

        //MediaViewer.Media = media;

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

        ContentSizer.ManipulationStarted += ContentSizer_ManipulationStarted;
        ContentSizer.ManipulationCompleted += ContentSizer_ManipulationCompleted;
        ContentSizer.PointerEntered += ContentSizer_PointerEntered;
        ContentSizer.PointerExited += ContentSizer_PointerExited;
    }

    private bool _isDragging = false;
    private bool _isHover = false;

    private void ContentSizer_PointerExited(object sender, Microsoft.UI.Xaml.Input.PointerRoutedEventArgs e)
    {
        _isHover = false;
        if (!_isDragging)
        {
            MediaViewer.CornerRadius = new CornerRadius(8, 8, 0, 0);
        }
    }

    private void ContentSizer_PointerEntered(object sender, Microsoft.UI.Xaml.Input.PointerRoutedEventArgs e)
    {
        _isHover = true;
        MediaViewer.CornerRadius = new CornerRadius(0, 8, 0, 0);
    }

    private void ContentSizer_ManipulationCompleted(object sender, Microsoft.UI.Xaml.Input.ManipulationCompletedRoutedEventArgs e)
    {
        _isDragging = false;
        if (!_isHover)
        {
            MediaViewer.CornerRadius = new CornerRadius(8, 8, 0, 0);
        }
    }

    private void ContentSizer_ManipulationStarted(object sender, Microsoft.UI.Xaml.Input.ManipulationStartedRoutedEventArgs e)
    {
        _isDragging = true;
    }

    private async void GetActiveTabs(object sender, RoutedEventArgs e)
    {
        //await BrowserService.FindActiveTabs();
        await using (MediaDbContext dataBase = new())
        {
            MediaItemsView.ItemsSource = dataBase.Medias.ToList();
        }
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

    private void ItemContainer_Tapped(object sender, TappedRoutedEventArgs e)
    {
        MediaViewer.Media = ((Media)((FrameworkElement)sender).DataContext);
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