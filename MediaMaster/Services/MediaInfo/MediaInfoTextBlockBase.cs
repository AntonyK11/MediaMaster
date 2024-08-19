using CommunityToolkit.WinUI.Controls;
using EFCore.BulkExtensions;
using MediaMaster.Controls;
using MediaMaster.DataBase;
using MediaMaster.Extensions;
using WinUI3Localizer;

namespace MediaMaster.Services.MediaInfo;

public abstract class MediaInfoTextBlockBase(DockPanel parent) : MediaInfoControlBase(parent)
{
    public EditableTextBlock? EditableTextBlock;
    public StackPanel? StackPanel;

    public override void Setup()
    {
        StackPanel = new StackPanel
        {
            Spacing = 4
        };
        StackPanel.SetValue(DockPanel.DockProperty, Dock.Top);

        Title = GetTitle();
        EditableTextBlock = GetEditableTextBlock();
        StackPanel.Children.Add(Title);
        StackPanel.Children.Add(EditableTextBlock);
        Parent.Children.Add(StackPanel);
    }

    public override void SetupTranslations()
    {
        if (Title != null && EditableTextBlock != null && !TranslationKey.IsNullOrEmpty())
        {
            Uids.SetUid(Title, $"/Media/{TranslationKey}_Title");
            Uids.SetUid(EditableTextBlock, $"/Media/{TranslationKey}_TextBlock");
        }
    }

    public virtual EditableTextBlock GetEditableTextBlock()
    {
        var editableTextBlock = new EditableTextBlock
        {
            HorizontalAlignment = HorizontalAlignment.Stretch,
            ConfirmOnReturn = false
        };
        editableTextBlock.TextConfirmed += (_, args) => UpdateMedia(args.NewText, args.OldText);
        return editableTextBlock;
    }

    public override void Show()
    {
        if (StackPanel != null)
        {
            StackPanel.Visibility = Visibility.Visible;
        }
    }

    public override void Hide()
    {
        if (StackPanel != null)
        {
            StackPanel.Visibility = Visibility.Collapsed;
        }
    }

    public virtual async void UpdateMedia(string newText, string oldText = "")
    {
        if (Medias.Count == 0 || oldText == newText) return;

        foreach (var media in Medias)
        {
            UpdateMediaProperty(media, newText);
            media.Modified = DateTime.UtcNow;
        }

        await using (var database = new MediaDbContext())
        {
            await database.BulkUpdateAsync(Medias);
        }

        InvokeMediaChange();
    }

    public virtual void UpdateMediaProperty(Media medias, string text) { }
}

