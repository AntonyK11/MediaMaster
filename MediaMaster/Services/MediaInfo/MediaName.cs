using CommunityToolkit.WinUI;
using CommunityToolkit.WinUI.Controls;
using EFCore.BulkExtensions;
using MediaMaster.Controls;
using MediaMaster.DataBase;
using MediaMaster.DataBase.Models;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;

namespace MediaMaster.Services.MediaInfo;

public class MediaName(DockPanel parent) : MediaInfoControlBase(parent)
{
    public Image? MediaExtensionIcon;
    public EditableTextBlock? EditableTextBlock;
    public Grid? Grid;
    public Border? Border;
    private MyCancellationTokenSource? _tokenSource;

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
                SetMediaExtensionIcon(Medias.First());
                EditableTextBlock.Text = Medias.First().Name;
                break;
            }
            case 0:
            {
                EditableTextBlock.Text = "No Media Selected";
                EditableTextBlock.EditOnDoubleClick = false;
                EditableTextBlock.EditOnClick = false;
                break;
            }
            default:
            {
                var text = Medias.First().Name;
                if (Medias.Any(media => media.Name != text))
                {
                    text = "Multiple Media Selected";
                }

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

    private async void SetMediaExtensionIcon(Media media)
    {
        if (MediaExtensionIcon != null)
        {
            if (_tokenSource is { IsDisposed: false })
            {
                await _tokenSource.CancelAsync();
            }
            _tokenSource = new MyCancellationTokenSource();

            MediaExtensionIcon.Source = null;
            var icon = await IconService.GetIcon(media.Uri, ImageMode.IconOnly, 24, 24, _tokenSource);
            await App.DispatcherQueue.EnqueueAsync(() => MediaExtensionIcon.Source = icon);
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
        if (Medias.Count == 0 || !args.MediaIds.Intersect(mediaIds).Any() || ReferenceEquals(sender, this) || !args.Flags.HasFlag(MediaChangeFlags.NameChanged)) return;
        Medias = args.Medias.Where(media => mediaIds.Contains(media.MediaId)).ToList();
        UpdateControlContent();
    }
}

