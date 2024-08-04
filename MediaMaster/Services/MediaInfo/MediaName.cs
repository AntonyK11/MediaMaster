using EFCore.BulkExtensions;
using MediaMaster.Controls;
using MediaMaster.DataBase;
using MediaMaster.DataBase.Models;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;

namespace MediaMaster.Services.MediaInfo;

public class MediaName(StackPanel parent) : MediaInfoControlBase(parent)
{
    public Image? MediaExtensionIcon;
    public EditableTextBlock? EditableTextBlock;
    public Grid? Grid;
    public Border? Border;
    private MyCancellationTokenSource? _tokenSource;

    public override string TranslationKey { get; set; } = "";

    public override void UpdateControlContent()
    {
        if (EditableTextBlock == null) return;
        EditableTextBlock.Text = Media?.Name ?? "No Media Found";
        if (Media != null)
        {
            SetMediaExtensionIcon(Media);
        }
    }

    public override void Setup(bool isCompact)
    {
        Grid = new Grid
        {
            MinHeight = 24,
            HorizontalAlignment = HorizontalAlignment.Center,
            ColumnSpacing = 12,
            ColumnDefinitions =
            {
                new ColumnDefinition { Width = GridLength.Auto},
                new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star)}
            }
        };

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

        EditableTextBlock = new EditableTextBlock
        {
            PlaceholderText = "Media Name"
        };
        EditableTextBlock.SetValue(Grid.ColumnProperty, 1);
        EditableTextBlock.TextConfirmed += (_, args) => UpdateMedia(args);

        Grid.Children.Add(Border);
        Grid.Children.Add(EditableTextBlock);
        Parent.Children.Add(Grid);
    }

    public override void SetupTranslations() { }

    public override void Show(bool isCompact)
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
        if (Media == null || args.OldText == args.NewText) return;

        Media.Name = args.NewText;
        Media.Modified = DateTime.UtcNow;

        await using (var database = new MediaDbContext())
        {
            await database.BulkUpdateAsync([Media]);
        }

        InvokeMediaChange(Media);
    }

    private void SetMediaExtensionIcon(Media media)
    {
        if (MediaExtensionIcon != null)
        {
            if (_tokenSource is { IsDisposed: false })
            {
                _tokenSource?.Cancel();
            }

            MediaExtensionIcon.Source = null;
            _tokenSource = IconService.AddImage(media.Uri, ImageMode.IconOnly, 24, 24, MediaExtensionIcon);
        }
    }

    public override void InvokeMediaChange(Media media)
    {
        if (Media == null) return;
        MediaDbContext.InvokeMediaChange(this, MediaChangeFlags.MediaChanged | MediaChangeFlags.NameChanged, Media);
    }

    public override void MediaChanged(object? sender, MediaChangeArgs args)
    {
        if (Media == null || args.Media.MediaId != Media.MediaId || ReferenceEquals(sender, this) || !args.Flags.HasFlag(MediaChangeFlags.NameChanged)) return;
        Media = args.Media;
        UpdateControlContent();
    }
}

