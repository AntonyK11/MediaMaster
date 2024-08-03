using System.Numerics;
using Microsoft.UI.Xaml;
using MediaMaster.DataBase;
using MediaMaster.Interfaces.Services;
using MediaMaster.Services;
using BookmarksManager;
using MediaMaster.DataBase.Models;
using Microsoft.UI.Composition;
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

        ContentSizer.ManipulationStarted += ContentSizer_ManipulationStarted;
        ContentSizer.ManipulationCompleted += ContentSizer_ManipulationCompleted;
        ContentSizer.PointerEntered += ContentSizer_PointerEntered;
        ContentSizer.PointerExited += ContentSizer_PointerExited;
    }

    private SpringVector3NaturalMotionAnimation? _springAnimation;

    private void CreateOrUpdateSpringAnimation(float finalValue)
    {
        if (_springAnimation == null)
        {
            _springAnimation = App.MainWindow!.Compositor.CreateSpringVector3Animation();
            _springAnimation.Target = "Translation";
            _springAnimation.Period = TimeSpan.FromMilliseconds(32);
        }

        _springAnimation.FinalValue = new Vector3(0, finalValue, 0);
    }

    private void element_PointerEntered(object sender, PointerRoutedEventArgs e)
    {
        CreateOrUpdateSpringAnimation(-2);

        ((FrameworkElement)sender).StartAnimation(_springAnimation);
    }

    private void element_PointerExited(object sender, PointerRoutedEventArgs e)
    {
        CreateOrUpdateSpringAnimation(0);

        ((FrameworkElement)sender).StartAnimation(_springAnimation);
    }

    private void element_PointerPressed(object sender, PointerRoutedEventArgs e)
    {
        CreateOrUpdateSpringAnimation(0);

        ((FrameworkElement)sender).StartAnimation(_springAnimation);
    }

    private void element_PointerReleased(object sender, TappedRoutedEventArgs tappedRoutedEventArgs)
    {
        CreateOrUpdateSpringAnimation(-2);

        ((FrameworkElement)sender).StartAnimation(_springAnimation);

        MediaViewer.Media = ((Media)((FrameworkElement)sender).DataContext);
    }

    private bool _isDragging;
    private bool _isHover;

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
        //        p.Uri = "";
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

    private void HomePage_OnSizeChanged(object sender, SizeChangedEventArgs e)
    {
        if (ContentColumn.ActualWidth == ContentColumn.MinWidth && e.NewSize.Width > ContentColumn.MinWidth + PaneColumn.MinWidth + ContentGrid.Padding.Left + ContentGrid.Padding.Right)
        {
            var newWidth = PaneColumn.Width.Value + e.NewSize.Width - e.PreviousSize.Width;
            PaneColumn.Width = new GridLength(newWidth < PaneColumn.MinWidth ? PaneColumn.MinWidth : newWidth, GridUnitType.Pixel);
        }
    }
}

internal class BookmarksTemplateSelector : DataTemplateSelector
{
    public DataTemplate BookmarkFolder { get; set; }
    public DataTemplate BookmarkLink { get; set; }

    protected override DataTemplate SelectTemplateCore(object item)
    {
        return item is BookmarkFolder ? BookmarkFolder : BookmarkLink;
    }
}