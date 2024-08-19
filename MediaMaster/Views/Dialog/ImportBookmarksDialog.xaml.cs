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
    public ICollection<BrowserFolder> SelectedBrowserFolders { get; set; } = [];

    public ImportBookmarksDialog()
    {
        InitializeComponent();

        GetBookmarks();
    }

    public static readonly ICollection<BrowserFolder> BrowserFolders = [];

    private async void GetBookmarks()
    {
        var bookmarks = await BrowserService.GetBookmarks();
        
        BrowserFolders.Clear();
        foreach (var bookmark in bookmarks)
        {
            if (bookmark is BookmarkFolder browser)
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

        if (result == ContentDialogResult.Primary)
        {
            var generateBookmarkTags = importBookmarksDialog.GenerateBookmarkTags;
            _ = Task.Run(() => MediaService.AddMediaAsync(browserFolders: importBookmarksDialog.SelectedBrowserFolders, generateBookmarkTags: generateBookmarkTags));
        }

        return (result, importBookmarksDialog);
    }

    private void BookmarksTree_OnSelectionChanged(TreeView sender, TreeViewSelectionChangedEventArgs args)
    {
        SelectedBrowserFolders = BookmarksTree.SelectedItems.OfType<BrowserFolder>().ToList();
    }
}

public class BrowserFolder
{
    public BookmarkFolder BookmarkFolder { get; set; }

    public ImageSource Icon { get; set; }

    public BrowserFolder(BookmarkFolder bookmarkFolder)
    {
        BookmarkFolder = bookmarkFolder;

        if (BrowserService.BrowserData != null)
        {
            var data = BrowserService.BrowserData.FirstOrDefault(b => b.Name == BookmarkFolder.Title);
            Icon = BrowserService.GetBrowserIcon(data.Icon);
        }
    }
}

internal partial class BookmarksTemplateSelector : DataTemplateSelector
{
    public DataTemplate BrowserFolder { get; set; }
    public DataTemplate BookmarkFolder { get; set; }
    public DataTemplate BookmarkLink { get; set; }

    protected override DataTemplate SelectTemplateCore(object item)
    {
        if (item is BrowserFolder)
        {
            return BrowserFolder;
        }

        if (item is BookmarkFolder)
        {
            return BookmarkFolder;
        }

        return BookmarkLink;
    }
}