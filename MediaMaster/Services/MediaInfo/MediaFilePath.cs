using System.Reflection;
using CommunityToolkit.WinUI.Controls;
using EFCore.BulkExtensions;
using MediaMaster.Controls;
using MediaMaster.DataBase;
using MediaMaster.Extensions;
using MediaMaster.Interfaces.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.WindowsAPICodePack.Dialogs;
using WinUI3Localizer;
using WinUIEx;
using HorizontalAlignment = Microsoft.UI.Xaml.HorizontalAlignment;

namespace MediaMaster.Services.MediaInfo;

public class MediaFilePath(DockPanel parent) : MediaInfoTextBlockBase(parent)
{
    protected override string TranslationKey => "MediaFilePath";

    protected override void UpdateControlContent()
    {
        if (EditableTextBlock == null) return;
        EditableTextBlock.Text = Medias.FirstOrDefault()?.Uri ?? "";
    }

    protected override EditableTextBlock GetEditableTextBlock()
    {
        var editableTextBlock = new EditableTextBlock
        {
            HorizontalAlignment = HorizontalAlignment.Stretch,
            ConfirmOnReturn = false,
            EditOnClick = false,
            EditOnDoubleClick = false
        };
        editableTextBlock.EditButtonPressed += (sender, _) =>
        {
            if (Medias.Count == 1)
            {
                PathTextBox_OnEdit(Medias.First(), sender, this);
            }
        };
        return editableTextBlock;
    }

    private static async void PathTextBox_OnEdit(Media media, EditableTextBlock sender, object? updateSender = null)
    {
        if (App.MainWindow == null) return;

        // Cannot use System.Windows.Forms.OpenFileDialog because it makes the app crash if the window is closed after the dialog in certain situations
        using (CommonOpenFileDialog dialog = new())
        {
            dialog.InitialDirectory = Path.GetDirectoryName(sender.Text);
            dialog.DefaultFileName = Path.GetFileNameWithoutExtension(sender.Text);
            dialog.EnsureFileExists = true;
            dialog.EnsurePathExists = true;
            dialog.ShowHiddenItems = true;
            dialog.NavigateToShortcut = false;

            // Use reflection to set the _parentWindow handle without needing to include PresentationFrameWork
            FieldInfo? fi =
                typeof(CommonFileDialog).GetField("_parentWindow", BindingFlags.NonPublic | BindingFlags.Instance);
            if (fi != null)
            {
                var hwnd = App.MainWindow.GetWindowHandle();
                fi.SetValue(dialog, hwnd);
            }

            if (dialog.ShowDialog() == CommonFileDialogResult.Ok)
            {
                var filePath = dialog.FileName;
                if (filePath == null) return;

                await using (var database = new MediaDbContext())
                {
                    if (await database.Medias.Select(m => m.Uri).ContainsAsync(filePath))
                    {
                        ContentDialog errorDialog = new()
                        {
                            XamlRoot = App.MainWindow.Content.XamlRoot,
                            DefaultButton = ContentDialogButton.Primary,
                            RequestedTheme = App.GetService<IThemeSelectorService>().ActualTheme
                        };
                        Uids.SetUid(errorDialog, "/Media/FilePathAlreadyExistsDialog");
                        App.GetService<IThemeSelectorService>().ThemeChanged += (_, theme) =>
                        {
                            errorDialog.RequestedTheme = theme;
                        };

                        ContentDialogResult errorResult = await errorDialog.ShowAndEnqueueAsync();

                        if (errorResult == ContentDialogResult.Primary)
                        {
                            PathTextBox_OnEdit(media, sender);
                        }
                    }
                    else
                    {
                        UpdateMedia(media, filePath, sender.Text, updateSender);
                        sender.Text = filePath;
                    }
                }
            }
        }
    }

    private static async void UpdateMedia(Media media, string newText, string oldText = "", object? updateSender = null)
    {
        if (oldText == newText) return;

        Tag? newTag = null;
        Tag? oldTag = null;
        await using (var database = new MediaDbContext())
        {
            media.Uri = newText;
            media.Modified = DateTime.UtcNow;

            var oldExtension = Path.GetExtension(oldText);
            var newExtension = Path.GetExtension(newText);
            if (oldExtension != newExtension)
            {
                Tag? oldExtensionTag = await database.Medias
                    .Include(m => m.Tags)
                    .Where(m => m.MediaId == media.MediaId)
                    .SelectMany(m => m.Tags)
                    .FirstOrDefaultAsync(t => t.Name == oldExtension && t.Flags.HasFlag(TagFlags.Extension));

                if (oldExtensionTag != null)
                {
                    MediaTag? oldMediaTag = await database.MediaTags.FirstOrDefaultAsync(m =>
                        m.TagId == oldExtensionTag.TagId && m.MediaId == media.MediaId);

                    if (oldMediaTag != null)
                    {
                        oldTag = oldExtensionTag;
                        await database.BulkDeleteAsync([oldMediaTag]);
                    }
                }

                (var isNew, newTag) = await MediaService.GetFileTag(newText, database: database);
                if (newTag != null)
                {
                    if (isNew)
                    {
                        await database.BulkInsertAsync([newTag], new BulkConfig { SetOutputIdentity = true });
                        await MediaService.AddNewTags([newTag], database);
                    }

                    media.Tags.Add(newTag);
                    await MediaService.AddNewMedias([media], database);
                }
            }

            await database.BulkUpdateAsync([media]);
        }

        if (newTag != null || oldTag != null)
        {
            MediaDbContext.InvokeMediaChange(
                updateSender,
                MediaChangeFlags.MediaChanged | MediaChangeFlags.UriChanged | MediaChangeFlags.TagsChanged, 
                [media],
                newTag != null ? [newTag] : [],
                oldTag != null ? [oldTag] : []);
        }
        else
        {
            MediaDbContext.InvokeMediaChange(updateSender, MediaChangeFlags.MediaChanged | MediaChangeFlags.UriChanged,
                [media]);
        }
    }

    protected override bool ShowInfo(ICollection<Media> medias)
    {
        return medias.Count != 0 && !(IsCompact || medias.Count != 1 || medias.First().Uri.IsWebsite());
    }

    protected override void MediaChanged(object? sender, MediaChangeArgs args)
    {
        List<int> mediaIds = Medias.Select(m => m.MediaId).ToList();
        
        if (Medias.Count == 0 ||
            !args.MediaIds.Intersect(mediaIds).Any() ||
            ReferenceEquals(sender, this) ||
            !args.Flags.HasFlag(MediaChangeFlags.UriChanged)) return;
        
        Medias = args.Medias.Where(media => mediaIds.Contains(media.MediaId)).ToList();
        UpdateControlContent();
    }
}