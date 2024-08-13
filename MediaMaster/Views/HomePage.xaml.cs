using System.Linq.Expressions;
using Microsoft.UI.Xaml;
using MediaMaster.DataBase.Models;
using Microsoft.UI.Xaml.Controls;


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
    }

    private void HomePage_OnSizeChanged(object sender, SizeChangedEventArgs e)
    {
        if (ContentColumn.ActualWidth == ContentColumn.MinWidth && e.NewSize.Width > ContentColumn.MinWidth + PaneColumn.MinWidth + ContentGrid.Padding.Left + ContentGrid.Padding.Right)
        {
            var newWidth = PaneColumn.Width.Value + e.NewSize.Width - e.PreviousSize.Width;
            PaneColumn.Width = new GridLength(newWidth < PaneColumn.MinWidth ? PaneColumn.MinWidth : newWidth, GridUnitType.Pixel);
        }
    }

    private void MediaItemsView_OnSelectionChanged(object sender, ICollection<Media> args)
    {
        MediaViewer.Medias = args;
    }

    private void SortBy_MenuFlyoutItem_OnClick(object sender, RoutedEventArgs e)
    {
        var index = (string)((FrameworkElement)sender).Tag;

        KeyValuePair<bool, Expression<Func<Media, object>>> function = index switch
        {
            "Modified" => new (false, m => m.Modified),
            "Added" => new(true, m => m.Added),
            "Archived" => new(false, m => m.IsArchived),
            "Favorite" => new(false, m => m.IsFavorite),
            _ => new(true, m => m.Name)
        };

        MediaItemsView.SortFunction = function;
    }

    private void SortOrder_MenuFlyoutItem_OnClick(object sender, RoutedEventArgs e)
    {
        MediaItemsView.SortAscending = (string)((FrameworkElement)sender).Tag == "Ascending";
    }
}