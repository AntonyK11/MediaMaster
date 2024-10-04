using CommunityToolkit.WinUI.Controls;
using EFCore.BulkExtensions;
using MediaMaster.Controls;
using MediaMaster.DataBase;
using MediaMaster.Extensions;
using MediaMaster.Interfaces.Services;
using Microsoft.EntityFrameworkCore;
using WinUI3Localizer;

namespace MediaMaster.Services.MediaInfo;

public sealed class MediaWebUrl(DockPanel parent) : MediaInfoTextBlockBase(parent)
{
    protected override string TranslationKey => "MediaWebUrl";

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
            ConfirmOnReturn = false
        };
        editableTextBlock.TextConfirmed += async (_, args) => await UpdateMediaUri(args.NewText, args.OldText);
        return editableTextBlock;
    }

    private async Task UpdateMediaUri(string newText, string oldText)
    {
        if (oldText == newText || EditableTextBlock == null || App.MainWindow == null) return;

        newText = newText.FormatAsWebsite();

        await using (var database = new MediaDbContext())
        {
            if (await database.Medias.Select(m => m.Uri).ContainsAsync(newText) || newText.IsNullOrEmpty())
            {
                EditableTextBlock.Text = oldText;
                ContentDialog errorDialog = new()
                {
                    XamlRoot = App.MainWindow.Content.XamlRoot,
                    DefaultButton = ContentDialogButton.Primary,
                    RequestedTheme = App.GetService<IThemeSelectorService>().ActualTheme
                };
                Uids.SetUid(errorDialog,
                    newText.IsNullOrEmpty()
                        ? "/Media/MissingWebsiteUrlDialog"
                        : "/Media/WebsiteUrlAlreadyExistsDialog");
                App.GetService<IThemeSelectorService>().ThemeChanged += (_, theme) =>
                {
                    errorDialog.RequestedTheme = theme;
                };

                ContentDialogResult errorResult = await errorDialog.ShowAndEnqueueAsync();

                if (errorResult == ContentDialogResult.Primary)
                {
                    EditableTextBlock.EditText();
                }
            }
            else
            {
                UpdateMedia(newText, oldText);
            }
        }
    }

    protected override async void UpdateMedia(string newText, string oldText = "")
    {
        if (Medias.Count != 1 || oldText == newText) return;
        Media media = Medias.First();

        Tag? newTag = null;
        Tag? oldTag = null;
        await using (var database = new MediaDbContext())
        {
            media.Uri = newText;
            media.Modified = DateTime.UtcNow;

            var oldDomain = new Uri(oldText).Host;
            var newDomain = new Uri(newText).Host;
            if (oldDomain != newDomain)
            {
                Tag? oldDomainTag = await database.Medias
                    .Include(m => m.Tags)
                    .Where(m => m.MediaId == media.MediaId)
                    .SelectMany(m => m.Tags)
                    .FirstOrDefaultAsync(t => t.Name == oldDomain && t.Flags.HasFlag(TagFlags.Website));

                if (oldDomainTag != null)
                {
                    MediaTag? oldMediaTag = await database.MediaTags.FirstOrDefaultAsync(m =>
                        m.TagId == oldDomainTag.TagId && m.MediaId == media.MediaId);

                    if (oldMediaTag != null)
                    {
                        oldTag = oldDomainTag;
                        await database.BulkDeleteAsync([oldMediaTag]);
                    }
                }

                (var isNew, newTag) = await MediaService.GetWebsiteTag(newText, database: database);
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

            await database.BulkUpdateAsync(Medias);
        }

        if (newTag != null || oldTag != null)
        {
            MediaDbContext.InvokeMediaChange(
                this,
                MediaChangeFlags.MediaChanged | MediaChangeFlags.UriChanged | MediaChangeFlags.TagsChanged,
                Medias,
                newTag != null ? [newTag] : [],
                oldTag != null ? [oldTag] : []);
        }
        else
        {
            MediaDbContext.InvokeMediaChange(this, MediaChangeFlags.MediaChanged | MediaChangeFlags.UriChanged, Medias);
        }
    }

    protected override bool ShowInfo(ICollection<Media> medias)
    {
        return medias.Count != 0 && !(medias.Count != 1 || !medias.First().Uri.IsWebsite());
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