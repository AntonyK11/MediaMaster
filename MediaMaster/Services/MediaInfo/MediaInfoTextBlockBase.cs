using EFCore.BulkExtensions;
using MediaMaster.Controls;
using MediaMaster.DataBase;
using MediaMaster.DataBase.Models;
using Microsoft.IdentityModel.Tokens;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using WinUI3Localizer;

namespace MediaMaster.Services.MediaInfo;

public abstract class MediaInfoTextBlockBase(StackPanel parent) : MediaInfoControlBase(parent)
{
    public EditableTextBlock? EditableTextBlock;
    public StackPanel? StackPanel;

    public override void Setup(bool isCompact)
    {
        StackPanel = new StackPanel
        {
            Spacing = 4
        };
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
        editableTextBlock.TextConfirmed += (_, args) => UpdateMedia(args);
        return editableTextBlock;
    }

    public override void Show(bool isCompact)
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

    public virtual async void UpdateMedia(TextConfirmedArgs args)
    {
        if (Media == null || args.OldText == args.NewText) return;

        UpdateMediaProperty(Media, args.NewText);
        Media.Modified = DateTime.UtcNow;

        await using (var database = new MediaDbContext())
        {
            await database.BulkUpdateAsync([Media]);
        }

        InvokeMediaChange(Media);
    }

    public virtual void UpdateMediaProperty(Media media, string text) { }
}

