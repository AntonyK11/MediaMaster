using System.Linq.Expressions;
using MediaMaster.DataBase;
using MediaMaster.Services;
using MediaMaster.Views.Dialog;
using Microsoft.EntityFrameworkCore;
using Microsoft.WindowsAPICodePack.Dialogs;

namespace MediaMaster.Views;

public partial class HomePage : Page
{
    public HomePage()
    {
        InitializeComponent();

        MediaItemsView.SimpleFilterFunctions.Add(SearchBox.Filter);
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

    private void MediaItemsView_OnSelectionChanged(object sender, HashSet<int> args)
    {
        MediaViewer.SetMedias(args);
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
                await CreateEditDeleteTagDialog.ShowDialogAsync(this.XamlRoot);
                break;
            case "Manage_Tags":
                await TagsListDialog.ShowDialogAsync(this.XamlRoot);
                break;
            case "Fix_Unlinked_Medias":
                await FixUnlinkedMediasDialog.ShowDialogAsync();
                break;
            case "Open_Database_Locations":
                ProcessStartInfo startInfo = new()
                {
                    Arguments = $"/select, \"{MediaDbContext.DbPath}\"",
                    FileName = "explorer.exe"
                };
                Process.Start(startInfo);
                break;
            case "Export_Search_In_Excel":
                await ExportSearchInExcel();
                break;
        }
    }

    private async Task ExportSearchInExcel()
    {
        (CommonFileDialogResult result, var fileName) = FilePickerService.SaveFilePicker("Search-results.xlsx", "xlsx", new CommonFileDialogFilter("Excel Workbook", "*.xlsl"));

        if (result == CommonFileDialogResult.Ok && fileName != null)
        {
            var sortFunction = MediaItemsView.SortFunction;
            var sortAscending = MediaItemsView.SortAscending;
            var simpleFilterFunctions = MediaItemsView.SimpleFilterFunctions;
            var advancedFilterFunctions = MediaItemsView.AdvancedFilterFunctions;

            await Task.Run(async () =>
            {
                List<Media> medias;
                await using (var database = new MediaDbContext())
                {
                    var mediaQuery = SearchService.GetQuery(database, sortFunction, sortAscending, simpleFilterFunctions, advancedFilterFunctions);

                    medias = await mediaQuery.Include(m => m.Tags).ToListAsync();
                }

                ExcelService.SaveSearchResultsToExcel(medias, fileName);
            });
        }
    }

    private async void FlyoutBase_OnClosed(object? sender, object e)
    {
        var oldCount = MediaItemsView.AdvancedFilterFunctions.Count;
        MediaItemsView.AdvancedFilterFunctions.Clear();
        foreach (Expression<Func<Media, bool>> expression in await AdvancedFilters.GetFilterExpressions(AdvancedFilters.FilterObjects))
        {
            MediaItemsView.AdvancedFilterFunctions.Add(expression);
        }
        var newCount = MediaItemsView.AdvancedFilterFunctions.Count;

        if (oldCount != newCount || newCount != 0)
        {
            await MediaItemsView.SetupMediaCollection();
        }
    }
}