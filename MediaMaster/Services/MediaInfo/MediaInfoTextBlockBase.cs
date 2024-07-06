using MediaMaster.Controls;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using WinUI3Localizer;

namespace MediaMaster.Services.MediaInfo;

public abstract class MediaInfoTextBlockBase(StackPanel parent) : MediaInfoControlBase(parent)
{
    public TextBlock? TextBlock;
    public EditableTextBlock? EditableTextBlock;

    public override void Setup()
    {
        var stackPanel = new StackPanel
        {
            Spacing = 4
        };
        TextBlock = new TextBlock();
        EditableTextBlock = GetEditableTextBlock();
        stackPanel.Children.Add(TextBlock);
        stackPanel.Children.Add(EditableTextBlock);
        Parent.Children.Add(stackPanel);
    }

    public override void SetupTranslations()
    {
        if (TextBlock != null && EditableTextBlock != null)
        {
            Uids.SetUid(TextBlock, $"/Media/{TranslationKey}_Title");
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
        editableTextBlock.TextConfirmed += (_, text) => TextConfirmed(text);
        return editableTextBlock;
    }

    public override void Show()
    {
        if (TextBlock != null && EditableTextBlock != null)
        {
            TextBlock.Visibility = Visibility.Visible;
            EditableTextBlock.Visibility = Visibility.Visible;
        }
    }

    public override void Hide()
    {
        if (TextBlock != null && EditableTextBlock != null)
        {
            TextBlock.Visibility = Visibility.Collapsed;
            EditableTextBlock.Visibility = Visibility.Collapsed;
        }
    }

    public virtual void TextConfirmed(string text)
    {
        UpdateMedia(text);
    }
}

