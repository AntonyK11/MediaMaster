using System.Drawing;
using System.Reflection;
using System.Security.Policy;
using EFCore.BulkExtensions;
using MediaMaster.Controls;
using MediaMaster.DataBase;
using MediaMaster.Extensions;
using MediaMaster.Interfaces.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.WindowsAPICodePack.Dialogs;
using WinUI3Localizer;
using WinUIEx;

namespace MediaMaster.Views.Dialog;

public sealed partial class CreateMediaDialog : Page
{
    public CreateMediaDialog()
    {
        InitializeComponent();
        TagView.MediaIds = [-1];
    }

    private void FileUriTextBox_OnEditButtonPressed(EditableTextBlock sender, string args)
    {
        // Cannot use System.Windows.Forms.OpenFileDialog because it makes the app crash if the window is closed after the dialog in certain situations
        using (CommonOpenFileDialog dialog = new())
        {
            dialog.InitialDirectory = Path.GetDirectoryName(FileUriTextBox.Text);
            dialog.DefaultFileName = Path.GetFileName(FileUriTextBox.Text);
            dialog.EnsureFileExists = true;
            dialog.EnsurePathExists = true;
            dialog.ShowHiddenItems = true;

            // Use reflection to set the _parentWindow handle without needing to include PresentationFrameWork
            FieldInfo? fi = typeof(CommonFileDialog).GetField("_parentWindow", BindingFlags.NonPublic | BindingFlags.Instance);
            if (fi != null && App.MainWindow != null)
            {
                var hwnd = App.MainWindow.GetWindowHandle();
                fi.SetValue(dialog, hwnd);
            }

            if (dialog.ShowDialog() == CommonFileDialogResult.Ok && dialog.FileName != null)
            {
                FileUriTextBox.Text = dialog.FileName;
            }
        }
    }

    public static async Task<(ContentDialogResult, CreateMediaDialog?)> ShowDialogAsync()
    {
        if (App.MainWindow == null) return (ContentDialogResult.None, null);

        var mediaDialog = new CreateMediaDialog();
        ContentDialog dialog = new()
        {
            XamlRoot = App.MainWindow.Content.XamlRoot,
            DefaultButton = ContentDialogButton.Primary,
            Content = mediaDialog
        };

        Uids.SetUid(dialog, "/Media/CreateMediaDialog");

        dialog.RequestedTheme = App.GetService<IThemeSelectorService>().ActualTheme;
        App.GetService<IThemeSelectorService>().ThemeChanged += (_, theme) => { dialog.RequestedTheme = theme; };

        ContentDialogResult result;
        while (true)
        {
            result = await dialog.ShowAndEnqueueAsync();

            if (result == ContentDialogResult.Primary)
            {
                var validation = await mediaDialog.ValidateMedia();

                if (validation != null)
                {
                    ContentDialog errorDialog = new()
                    {
                        XamlRoot = App.MainWindow.Content.XamlRoot,
                        DefaultButton = ContentDialogButton.Primary,
                        RequestedTheme = App.GetService<IThemeSelectorService>().ActualTheme,
                    };
                    App.GetService<IThemeSelectorService>().ThemeChanged += (_, theme) => { errorDialog.RequestedTheme = theme; };

                    switch (validation)
                    {
                        case "MissingFilePath":
                            Uids.SetUid(errorDialog, "/Media/MissingFilePathDialog");
                            break;
                        case "FilePathAlreadyExists":
                            Uids.SetUid(errorDialog, "/Media/FilePathAlreadyExistsDialog");
                            break;
                        case "MissingWebsiteUrl":
                            Uids.SetUid(errorDialog, "/Media/MissingWebsiteUrlDialog");
                            break;
                        case "WebsiteUrlAlreadyExists":
                            Uids.SetUid(errorDialog, "/Media/WebsiteUrlAlreadyExistsDialog");
                            break;
                    }

                    ContentDialogResult errorResult = await errorDialog.ShowAndEnqueueAsync();
                    if (errorResult == ContentDialogResult.None)
                    {
                        break;
                    }
                }
                else
                {
                    await mediaDialog.SaveChangesAsync();
                    break;
                }
            }
            else
            {
                break;
            }
        }

        return (result, mediaDialog);
    }

    public async Task<string?> ValidateMedia()
    {
        switch (SelectorBar.SelectedItem.Tag)
        {
            case "File":
                var filePath = FileUriTextBox.Text;
                if (filePath.IsNullOrEmpty())
                {
                    return "MissingFilePath";
                }

                await using (var database = new MediaDbContext())
                {
                    if (await database.Medias.Select(m => m.Uri).ContainsAsync(filePath))
                    {
                        return "FilePathAlreadyExists";
                    }
                }
                break;

            case "Website":
                var websiteUrl = WebsiteUriTextBox.Text;

                if (websiteUrl.IsNullOrEmpty())
                {
                    return "MissingWebsiteUrl";
                }

                if (websiteUrl.StartsWith("http"))
                {
                    websiteUrl = websiteUrl.Replace("://www.", "://");
                }
                else
                {
                    if (websiteUrl.StartsWith("www."))
                    {
                        websiteUrl = websiteUrl.Replace("www.", "");
                    }
                    websiteUrl = "https://" + websiteUrl;
                }

                await using (var database = new MediaDbContext())
                {
                    if (await database.Medias.Select(m => m.Uri).ContainsAsync(websiteUrl))
                    {
                        return "WebsiteUrlAlreadyExists";
                    }
                }
                break;
        }

        return null;
    }

    public async Task SaveChangesAsync()
    {
        await using (MediaDbContext dataBase = new())
        {
            
            Media media = new()
            {
                Name = NameTextBox.Text,
                Notes = NotesTextBox.Text
            };

            switch (SelectorBar.SelectedItem.Tag)
            {
                case "File":
                    media.Uri = FileUriTextBox.Text;
                    break;

                case "Website":
                    var websiteUrl = WebsiteUriTextBox.Text;
                    if (websiteUrl.StartsWith("http"))
                    {
                        websiteUrl = websiteUrl.Replace("://www.", "://");
                    }
                    else
                    {
                        if (websiteUrl.StartsWith("www."))
                        {
                            websiteUrl = websiteUrl.Replace("www.", "");
                        }
                        websiteUrl = "https://" + websiteUrl;
                    }
                    media.Uri = websiteUrl;
                    break;
            }

            await dataBase.Medias.AddAsync(media);
            await dataBase.SaveChangesAsync();

            HashSet<int> currentTagIds = media.Tags.Select(t => t.TagId).ToHashSet();
            HashSet<int> selectedTagIds = TagView.GetItemSource().Select(t => t.TagId).ToHashSet();

            List<int> tagIdsToAdd = selectedTagIds.Except(currentTagIds).ToList();
            List<int> tagIdsToRemove = currentTagIds.Except(selectedTagIds).ToList();

            if (tagIdsToAdd.Count != 0 || tagIdsToRemove.Count != 0)
            {
                // Bulk add new tags
                if (tagIdsToAdd.Count != 0)
                {
                    List<MediaTag> newMediaTags = tagIdsToAdd.Select(tagId => new MediaTag { MediaId = media.MediaId, TagId = tagId }).ToList();
                    await dataBase.BulkInsertOrUpdateAsync(newMediaTags);
                }

                // Bulk remove old tags
                if (tagIdsToRemove.Count != 0)
                {
                    List<MediaTag> mediaTagsToRemove = await dataBase.MediaTags
                        .Where(mt => mt.MediaId == media.MediaId && tagIdsToRemove.Contains(mt.TagId))
                        .ToListAsync();
                    await dataBase.BulkDeleteAsync(mediaTagsToRemove);
                }

                if (MediaDbContext.ArchivedTag != null)
                {
                    if (tagIdsToAdd.Contains(MediaDbContext.ArchivedTag.TagId))
                    {
                        media.IsArchived = true;
                    }
                    else if (tagIdsToRemove.Contains(MediaDbContext.ArchivedTag.TagId))
                    {
                        media.IsArchived = false;
                    }
                }

                if (MediaDbContext.FavoriteTag != null)
                {
                    if (tagIdsToAdd.Contains(MediaDbContext.FavoriteTag.TagId))
                    {
                        media.IsFavorite = true;
                    }
                    else if (tagIdsToRemove.Contains(MediaDbContext.FavoriteTag.TagId))
                    {
                        media.IsFavorite = false;
                    }
                }
            }

            MediaDbContext.InvokeMediaChange(this, MediaChangeFlags.MediaAdded, [media]);
        }
    }
}