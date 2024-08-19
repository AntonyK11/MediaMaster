using CommunityToolkit.WinUI;
using CommunityToolkit.WinUI.Controls;
using EFCore.BulkExtensions;
using MediaMaster.Controls;
using MediaMaster.DataBase;
using WinUI3Localizer;

namespace MediaMaster.Services.MediaInfo;

public class MediaName(DockPanel parent) : MediaInfoControlBase(parent)
{
    public Image? MediaExtensionIcon;
    public EditableTextBlock? EditableTextBlock;
    public Grid? Grid;
    public Border? Border;
    private TaskCompletionSource? _taskSource;

    public override string TranslationKey { get; set; } = "";

    public override void UpdateControlContent()
    {
        if (EditableTextBlock == null || MediaExtensionIcon == null) return;

        EditableTextBlock.EditOnDoubleClick = true;
        EditableTextBlock.EditOnClick = true;
        MediaExtensionIcon.Source = null;

        switch (Medias.Count)
        {
            case 1:
            {
                if (_taskSource is { Task.IsCompleted: false })
                {
                    _taskSource.SetResult();
                }
                _taskSource = new TaskCompletionSource();
                SetMediaExtensionIcon(Medias.First(), _taskSource);
                EditableTextBlock.Text = Medias.First().Name;
                break;
            }
            case 0:
            { 
                EditableTextBlock.Text = "/Media/NoMediaSelected".GetLocalizedString();
                EditableTextBlock.EditOnDoubleClick = false;
                EditableTextBlock.EditOnClick = false;
                break;
            }
            default:
            {
                var text = Medias.First().Name;
                if (Medias.Any(media => media.Name != text))
                {
                    EditableTextBlock.Text = "/Media/MultipleMediasSelected".GetLocalizedString();
                    break;
                }

                Uids.SetUid(EditableTextBlock, "");
                EditableTextBlock.Text = text;
                break;
            }
        }
    }

    public override void Setup()
    {
        Grid = new Grid
        {
            MinHeight = 24,
            HorizontalAlignment = HorizontalAlignment.Center,
            VerticalAlignment = VerticalAlignment.Top,
            ColumnSpacing = 12,
            Margin = new Thickness(0, 0, 0, 16),
            ColumnDefinitions =
            {
                new ColumnDefinition { Width = GridLength.Auto},
                new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star)}
            }
        };
        Grid.SetValue(DockPanel.DockProperty, Dock.Top);

        Border = new Border
        {
            HorizontalAlignment = HorizontalAlignment.Center,
            VerticalAlignment = VerticalAlignment.Center,
            CornerRadius = new CornerRadius(4)
        };
        Border.SetValue(Grid.ColumnProperty, 0);

        MediaExtensionIcon = new Image
        {
            Width = 24,
            Height = 24
        };
        Border.Child = MediaExtensionIcon;

        EditableTextBlock = new EditableTextBlock();
        Uids.SetUid(EditableTextBlock, "MediaName_TextBlock");
        EditableTextBlock.SetValue(Grid.ColumnProperty, 1);
        EditableTextBlock.TextConfirmed += (_, args) => UpdateMedia(args);

        Grid.Children.Add(Border);
        Grid.Children.Add(EditableTextBlock);
        Parent.Children.Add(Grid);
    }

    public override bool ShowInfo(ICollection<Media> medias)
    {
        return medias.Count == 0 || !IsCompact;
    }

    public override void SetupTranslations() { }

    public override void Show()
    {
        if (Grid != null)
        {
            Grid.Visibility = Visibility.Visible;
        }
    }

    public override void Hide()
    {
        if (Grid != null)
        {
            Grid.Visibility = Visibility.Collapsed;
        }
    }

    public virtual async void UpdateMedia(TextConfirmedArgs args)
    {
        if (Medias.Count == 0 || args.OldText == args.NewText) return;

        foreach (var media in Medias)
        {
            media.Name = args.NewText;
            media.Modified = DateTime.UtcNow;
        }

        await using (var database = new MediaDbContext())
        {
            await database.BulkUpdateAsync(Medias);
        }

        InvokeMediaChange();
    }

    private async void SetMediaExtensionIcon(Media media, TaskCompletionSource tcs)
    {
        if (MediaExtensionIcon != null)
        {
            MediaExtensionIcon.Source = null;
            var icon = await IconService.GetIcon(media.Uri, ImageMode.IconOnly, 24, 24, tcs);

            if (Medias.Count == 1 && !tcs.Task.IsCompleted)
            {
                await App.DispatcherQueue.EnqueueAsync(() => MediaExtensionIcon.Source = icon);
            }
        }
    }

    public override void InvokeMediaChange()
    {
        if (Medias.Count == 0) return;
        MediaDbContext.InvokeMediaChange(this, MediaChangeFlags.MediaChanged | MediaChangeFlags.NameChanged, Medias);
    }

    public override void MediaChanged(object? sender, MediaChangeArgs args)
    {
        var mediaIds = Medias.Select(m => m.MediaId).ToList();
        if (Medias.Count == 0 || !args.MediaIds.Intersect(mediaIds).Any() || ReferenceEquals(sender, this) || !(args.Flags.HasFlag(MediaChangeFlags.NameChanged) || args.Flags.HasFlag(MediaChangeFlags.UriChanged))) return;
        Medias = args.Medias.Where(media => mediaIds.Contains(media.MediaId)).ToList();
        UpdateControlContent();
    }
}

