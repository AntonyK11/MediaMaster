using CommunityToolkit.WinUI.Controls;
using EFCore.BulkExtensions;
using MediaMaster.Controls;
using MediaMaster.DataBase;
using MediaMaster.Extensions;
using WinUI3Localizer;

namespace MediaMaster.Services.MediaInfo;

public abstract class MediaInfoTextBlockBase(DockPanel parent) : MediaInfoControlBase(parent)
{
    private StackPanel? _stackPanel;
    protected EditableTextBlock? EditableTextBlock;

    protected override void Setup()
    {
        _stackPanel = new StackPanel
        {
            Spacing = 4
        };
        _stackPanel.SetValue(DockPanel.DockProperty, Dock.Top);
        AddAnimation(_stackPanel);

        Title = GetTitleTextBlock();
        EditableTextBlock = GetEditableTextBlock();
        _stackPanel.Children.Add(Title);
        _stackPanel.Children.Add(EditableTextBlock);
        Parent.Children.Add(_stackPanel);
    }

    protected override void SetupTranslations()
    {
        if (Title != null && EditableTextBlock != null && !TranslationKey.IsNullOrEmpty())
        {
            Uids.SetUid(Title, $"/Media/{TranslationKey}_Title");
            Uids.SetUid(EditableTextBlock, $"/Media/{TranslationKey}_TextBlock");
        }
    }

    protected virtual EditableTextBlock GetEditableTextBlock()
    {
        var editableTextBlock = new EditableTextBlock
        {
            HorizontalAlignment = HorizontalAlignment.Stretch,
            ConfirmOnReturn = false
        };
        editableTextBlock.TextConfirmed += (_, args) => UpdateMedia(args.NewText, args.OldText);
        return editableTextBlock;
    }

    protected override void Show()
    {
        if (_stackPanel != null)
        {
            _stackPanel.Visibility = Visibility.Visible;
        }
    }

    protected override void Hide()
    {
        if (_stackPanel != null)
        {
            _stackPanel.Visibility = Visibility.Collapsed;
        }
    }

    protected virtual async void UpdateMedia(string newText, string oldText = "")
    {
        if (Medias.Count == 0 || oldText == newText) return;

        foreach (Media media in Medias)
        {
            UpdateMediaProperty(media, newText);
            media.Modified = DateTime.UtcNow;
        }

        await using (var database = new MediaDbContext())
        {
            var transactionSuccessful = await Transaction.Try(database, () => database.BulkUpdateAsync(Medias));

            if (transactionSuccessful)
            {
                InvokeMediaChange();
            }
        }
    }

    protected virtual void UpdateMediaProperty(Media medias, string text) { }
}