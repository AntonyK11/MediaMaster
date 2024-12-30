using DependencyPropertyGenerator;
using MediaMaster.DataBase;
using MediaMaster.Extensions;
using MediaMaster.Interfaces.Services;
using Microsoft.UI.Xaml.Input;
using WinUI3Localizer;

namespace MediaMaster.Views.Dialog;

[DependencyProperty("Tags", typeof(ICollection<Tag>), DefaultValueExpression = "new List<Tag>()")]
[DependencyProperty("Notes", typeof(string), DefaultValue = "")]
public sealed partial class AddMediasDialog : Page
{
    public AddMediasDialog()
    {
        InitializeComponent();
        TagView.MediaIds = [-1];

        _ = FocusManager.TryFocusAsync(this, FocusState.Keyboard);
    }

    public static async Task<(ContentDialogResult, AddMediasDialog?)> ShowDialogAsync(ICollection<string> mediaPaths)
    {
        if (App.MainWindow == null) return (ContentDialogResult.None, null);

        var mediaDialog = new AddMediasDialog();
        ContentDialog dialog = new()
        {
            XamlRoot = App.MainWindow.Content.XamlRoot,
            DefaultButton = ContentDialogButton.Primary,
            Content = mediaDialog
        };

        Uids.SetUid(dialog, "/Media/AddMediasDialog");

        dialog.RequestedTheme = App.GetService<IThemeSelectorService>().ActualTheme;
        App.GetService<IThemeSelectorService>().ThemeChanged += (_, theme) => dialog.RequestedTheme = theme;

        ContentDialogResult result = await dialog.ShowAndEnqueueAsync();

        if (result == ContentDialogResult.Primary)
        {
            var tagIds = mediaDialog.Tags.Select(t => t.TagId).ToHashSet();
            var notes = mediaDialog.Notes;
            await Task.Run(() => FileWebsiteService.AddMediaAsync(mediaPaths, tagIds, notes));
        }

        return (result, mediaDialog);
    }

    public static async Task<(ContentDialogResult, AddMediasDialog?)> ShowDialogAsync(ICollection<BrowserFolder> browserFolders, bool generateBookmarkTags)
    {
        if (App.MainWindow == null) return (ContentDialogResult.None, null);

        var mediaDialog = new AddMediasDialog();
        ContentDialog dialog = new()
        {
            XamlRoot = App.MainWindow.Content.XamlRoot,
            DefaultButton = ContentDialogButton.Primary,
            Content = mediaDialog
        };

        Uids.SetUid(dialog, "/Media/AddMediasDialog");

        dialog.RequestedTheme = App.GetService<IThemeSelectorService>().ActualTheme;
        App.GetService<IThemeSelectorService>().ThemeChanged += (_, theme) => dialog.RequestedTheme = theme;

        ContentDialogResult result = await dialog.ShowAndEnqueueAsync();

        if (result == ContentDialogResult.Primary)
        {
            var tagIds = mediaDialog.Tags.Select(t => t.TagId).ToHashSet();
            var notes = mediaDialog.Notes;
            await Task.Run(() =>BookmarkService.AddBookmarksAsync(
                browserFolders, tagIds, notes, generateBookmarkTags));
        }

        return (result, mediaDialog);
    }
}
