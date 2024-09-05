using System.Linq.Expressions;
using MediaMaster.Views.Dialog;

namespace MediaMaster.Views;

public partial class HomePage : Page
{
    public HomePage()
    {
        InitializeComponent();

        MediaItemsView.AdvancedFilterFunctions.Add(SearchBox.Filter);
    }

    private void HomePage_OnSizeChanged(object sender, SizeChangedEventArgs e)
    {
        if (ContentColumn.ActualWidth == ContentColumn.MinWidth && e.NewSize.Width > ContentColumn.MinWidth +
            PaneColumn.MinWidth + ContentGrid.Padding.Left + ContentGrid.Padding.Right)
        {
            var newWidth = PaneColumn.Width.Value + e.NewSize.Width - e.PreviousSize.Width;
            PaneColumn.Width = new GridLength(newWidth < PaneColumn.MinWidth ? PaneColumn.MinWidth : newWidth,
                GridUnitType.Pixel);
        }
    }

    private void MediaItemsView_OnSelectionChanged(object sender, ICollection<Media> args)
    {
        MediaViewer.Medias = args;
    }

    private void SortBy_MenuFlyoutItem_OnClick(object sender, RoutedEventArgs e)
    {
        var tag = (string)((FrameworkElement)sender).Tag;

        KeyValuePair<bool, Expression<Func<Media, object>>>? function = tag switch
        {
            "Modified" => new KeyValuePair<bool, Expression<Func<Media, object>>>(false, m => m.Modified),
            "Added" => new KeyValuePair<bool, Expression<Func<Media, object>>>(false, m => m.Added),
            "Archived" => new KeyValuePair<bool, Expression<Func<Media, object>>>(false, m => m.IsArchived),
            "Favorite" => new KeyValuePair<bool, Expression<Func<Media, object>>>(false, m => m.IsFavorite),
            _ => null
        };

        MediaItemsView.SortFunction = function;
    }

    private void SortOrder_MenuFlyoutItem_OnClick(object sender, RoutedEventArgs e)
    {
        MediaItemsView.SortAscending = (string)((FrameworkElement)sender).Tag == "Ascending";
    }

    private async void SearchBox_OnFilterChanged(object sender, Expression<Func<Media, bool>> args)
    {
        await MediaItemsView.SetupMediaCollection();
    }

    private void Selection_MenuFlyoutItem_OnClick(object sender, RoutedEventArgs e)
    {
        var tag = (string)((FrameworkElement)sender).Tag;

        switch (tag)
        {
            case "All":
                MediaItemsView.SelectAll();
                break;
            case "Clear":
                MediaItemsView.ClearSelection();
                break;
        }
    }

    private async void MenuBar_MenuFlyoutItem_OnClick(object sender, RoutedEventArgs e)
    {
        var tag = (string)((FrameworkElement)sender).Tag;

        switch (tag)
        {
            case "Import_Bookmarks":
                await ImportBookmarksDialog.ShowDialogAsync();
                break;
            case "New_Media":
                await CreateMediaDialog.ShowDialogAsync();
                break;
            case "New_Tag":
                await CreateEditDeleteTagDialog.ShowDialogAsync();
                break;
            case "Manage_Tags":
                await TagsListDialog.ShowDialogAsync();
                break;
            case "Fix_Unlinked_Medias":
                await FixUnlinkedMediasDialog.ShowDialogAsync();
                break;
        }
    }

    private async void FlyoutBase_OnClosed(object? sender, object e)
    {
        MediaItemsView.AdvancedFilterFunctions.Clear();
        foreach (Expression<Func<Media, bool>> expression in await AdvancedFilters.GetFilterExpressions(AdvancedFilters
                     .FilterObjects))
        {
            MediaItemsView.AdvancedFilterFunctions.Add(expression);
        }

        await MediaItemsView.SetupMediaCollection();
    }
}