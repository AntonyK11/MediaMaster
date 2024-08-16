using System.Linq.Expressions;
using MediaMaster.Views.Dialog;

namespace MediaMaster.Views;

/// <summary>
///     An empty page that can be used on its own or navigated to within a Frame.
/// </summary>
public sealed partial class HomePage : Page
{
    private string _textBoxText = "";

    public HomePage()
    {
        this.InitializeComponent();

        MediaItemsView.FilterFunctions.Add(m => m.Name.Contains(_textBoxText));
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
        var tag = (string)((FrameworkElement)sender).Tag;

        KeyValuePair<bool, Expression<Func<Media, object>>> function = tag switch
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

    private async void TextBox_OnTextChanged(object sender, TextChangedEventArgs e)
    {
        _textBoxText = TextBox.Text;
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
                // TODO
                break;
            case "New_Media":
                // TODO
                break;
            case "New_Tag":
                await CreateEditDeleteTagDialog.ShowDialogAsync();
                break;
            case "Manage_Tags":
                await TagsListDialog.ShowDialogAsync();
                break;
            case "Manage_Extensions":
                // TODO
                break;
            case "About":
                // TODO
                break;
        }
    }
}