using MediaMaster.Controls;
using MediaMaster.DataBase;
using MediaMaster.DataBase.Models;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using WinUI3Localizer;

namespace MediaMaster.Services.MediaInfo;

public abstract class MediaInfoTextBlockBase(StackPanel parent) : MediaInfoControlBase(parent)
{
    public EditableTextBlock? EditableTextBlock;

    public override void Setup()
    {
        var stackPanel = new StackPanel
        {
            Spacing = 4
        };
        Title = GetTitle();
        EditableTextBlock = GetEditableTextBlock();
        stackPanel.Children.Add(Title);
        stackPanel.Children.Add(EditableTextBlock);
        Parent.Children.Add(stackPanel);
    }

    public override void SetupTranslations()
    {
        if (Title != null && EditableTextBlock != null)
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
        editableTextBlock.TextConfirmed += (_, args) => TextConfirmed(args);
        return editableTextBlock;
    }

    public override void Show()
    {
        base.Show();
        if (EditableTextBlock != null)
        {
            EditableTextBlock.Visibility = Visibility.Visible;
        }
    }

    public override void Hide()
    {
        base.Hide();
        if (EditableTextBlock != null)
        {
            EditableTextBlock.Visibility = Visibility.Collapsed;
        }
    }

    public virtual void TextConfirmed(TextConfirmedArgs args)
    {
        UpdateMedia(args);
    }

    public virtual async void UpdateMedia(TextConfirmedArgs args)
    {
        if (Media == null || args.OldText == args.NewText) return;

        await using (var database = new MediaDbContext())
        {
            var media = await database.Medias.FindAsync(Media.MediaId);

            if (media == null) return;

            UpdateMediaProperty(ref media, args.NewText);
            media.Modified = DateTime.UtcNow;

            await database.SaveChangesAsync();
        }
    }

    public virtual void UpdateMediaProperty(ref Media media, string text) { }
}

