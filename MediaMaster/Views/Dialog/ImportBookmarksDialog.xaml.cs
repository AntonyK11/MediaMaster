using BookmarksManager;
using DependencyPropertyGenerator;
using MediaMaster.DataBase;
using MediaMaster.Extensions;
using MediaMaster.Interfaces.Services;
using MediaMaster.Services;
using Microsoft.UI.Xaml.Media;
using WinUI3Localizer;

namespace MediaMaster.Views.Dialog;

[DependencyProperty("GenerateBookmarkTags", typeof(bool), DefaultValue = true, IsReadOnly = true)]
public sealed partial class ImportBookmarksDialog : Page
{
    private static readonly ICollection<BrowserFolder> BrowserFolders = [];

    public ImportBookmarksDialog()
    {
        InitializeComponent();

        GetBookmarks();
    }

    private ICollection<BrowserFolder> SelectedBrowserFolders { get; set; } = [];

    private async void GetBookmarks()
    {
        BookmarkFolder bookmarks = await BrowserService.GetBookmarks();

        BrowserFolders.Clear();
        foreach (IBookmarkItem? bookmark in bookmarks)
        {
            if (bookmark is BookmarkFolder browser && browser.Count != 0)
            {
                BrowserFolders.Add(new BrowserFolder(browser));
            }
        }

        BookmarksTree.ItemsSource = BrowserFolders;
    }

    public static async Task<(ContentDialogResult, ImportBookmarksDialog?)> ShowDialogAsync()
    {
        if (App.MainWindow == null) return (ContentDialogResult.None, null);

        var importBookmarksDialog = new ImportBookmarksDialog();
        ContentDialog dialog = new()
        {
            XamlRoot = App.MainWindow.Content.XamlRoot,
            DefaultButton = ContentDialogButton.Primary,
            Content = importBookmarksDialog,
            RequestedTheme = App.GetService<IThemeSelectorService>().ActualTheme
        };

        Uids.SetUid(dialog, "ImportBookmarksDialog");
        App.GetService<IThemeSelectorService>().ThemeChanged += (_, theme) => { dialog.RequestedTheme = theme; };

        ContentDialogResult result = await dialog.ShowAndEnqueueAsync();

        while (result == ContentDialogResult.Primary)
        {

            var generateBookmarkTags = importBookmarksDialog.GenerateBookmarkTags;
            (result, _) = await AddMediasDialog.ShowDialogAsync(importBookmarksDialog.SelectedBrowserFolders, generateBookmarkTags);

            if (result == ContentDialogResult.Primary)
            {
                break;
            }

            result = await dialog.ShowAndEnqueueAsync();

        }

        return (result, importBookmarksDialog);
    }

    private void BookmarksTree_OnSelectionChanged(TreeView sender, TreeViewSelectionChangedEventArgs args)
    {
        SelectedBrowserFolders = BookmarksTree.SelectedItems.OfType<BrowserFolder>().ToList();
    }
}

public sealed class BrowserFolder
{
    public BrowserFolder(BookmarkFolder bookmarkFolder)
    {
        BookmarkFolder = bookmarkFolder;

        if (BrowserService.BrowserData != null)
        {
            BrowserData data = BrowserService.BrowserData.FirstOrDefault(b => b.Name == BookmarkFolder.Title);
            Icon = BrowserService.GetBrowserIcon(data.Icon);
        }
    }

    public BookmarkFolder BookmarkFolder { get; set; }

    public ImageSource? Icon { get; set; }
}

public sealed partial class BookmarksTemplateSelector : DataTemplateSelector
{
    public DataTemplate BrowserFolderTemplate { get; set; } = null!;
    public DataTemplate BookmarkFolderTemplate { get; set; } = null!;
    public DataTemplate BookmarkLinkTemplate { get; set; } = null!;

    protected override DataTemplate SelectTemplateCore(object item)
    {
        return item switch
        {
            BrowserFolder => BrowserFolderTemplate,
            BookmarkFolder => BookmarkFolderTemplate,
            _ => BookmarkLinkTemplate
        };
    }
}