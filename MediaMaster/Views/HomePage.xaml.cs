using System.Numerics;
using Microsoft.UI.Xaml;
using MediaMaster.DataBase;
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
    public HomePage()
    {
        this.InitializeComponent();

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

    private void HomePage_OnSizeChanged(object sender, SizeChangedEventArgs e)
    {
        if (ContentColumn.ActualWidth == ContentColumn.MinWidth && e.NewSize.Width > ContentColumn.MinWidth + PaneColumn.MinWidth + ContentGrid.Padding.Left + ContentGrid.Padding.Right)
        {
            var newWidth = PaneColumn.Width.Value + e.NewSize.Width - e.PreviousSize.Width;
            PaneColumn.Width = new GridLength(newWidth < PaneColumn.MinWidth ? PaneColumn.MinWidth : newWidth, GridUnitType.Pixel);
        }
    }

    private async void ButtonBase_OnClick(object sender, RoutedEventArgs e)
    {
        await using (MediaDbContext dataBase = new())
        {
            MediaItemsView.ItemsSource = dataBase.Medias.ToList();
        }
    }

    private void MediaItemsView_OnSelectionChanged(ItemsView sender, ItemsViewSelectionChangedEventArgs args)
    {
        MediaViewer.Medias = MediaItemsView.SelectedItems.OfType<Media>().ToList();
    }
}