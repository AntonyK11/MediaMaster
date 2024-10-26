using CommunityToolkit.WinUI;
using CommunityToolkit.WinUI.Controls;
using EFCore.BulkExtensions;
using MediaMaster.Controls;
using MediaMaster.DataBase;
using Microsoft.UI.Xaml.Media.Imaging;
using WinUI3Localizer;

namespace MediaMaster.Services.MediaInfo;

public sealed class MediaName(DockPanel parent) : MediaInfoControlBase(parent)
{
    private Border? _border;
    private EditableTextBlock? _editableTextBlock;
    private Grid? _grid;
    private Image? _mediaExtensionIcon;
    private TaskCompletionSource? _taskSource;

    protected override string TranslationKey => "";

    protected override void UpdateControlContent()
    {
        if (_editableTextBlock == null || _mediaExtensionIcon == null) return;

        _editableTextBlock.EditOnDoubleClick = true;
        _editableTextBlock.EditOnClick = true;
        _mediaExtensionIcon.Source = null;

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
                _editableTextBlock.Text = Medias.First().Name;
                break;
            }
            case 0:
            {
                _editableTextBlock.Text = "/Media/NoMediaSelected".GetLocalizedString();
                _editableTextBlock.EditOnDoubleClick = false;
                _editableTextBlock.EditOnClick = false;
                break;
            }
            default:
            {
                var text = Medias.First().Name;
                if (Medias.Any(media => media.Name != text))
                {
                    _editableTextBlock.Text = "/Media/MultipleMediasSelected".GetLocalizedString();
                    break;
                }

                Uids.SetUid(_editableTextBlock, "");
                _editableTextBlock.Text = text;
                break;
            }
        }
    }

    protected override void Setup()
    {
        _grid = new Grid
        {
            MinHeight = 24,
            HorizontalAlignment = HorizontalAlignment.Center,
            VerticalAlignment = VerticalAlignment.Top,
            ColumnSpacing = 12,
            Margin = new Thickness(0, 0, 0, 16),
            ColumnDefinitions =
            {
                new ColumnDefinition { Width = GridLength.Auto },
                new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) }
            }
        };
        _grid.SetValue(DockPanel.DockProperty, Dock.Top);

        _border = new Border
        {
            HorizontalAlignment = HorizontalAlignment.Center,
            VerticalAlignment = VerticalAlignment.Center,
            CornerRadius = new CornerRadius(4)
        };
        _border.SetValue(Grid.ColumnProperty, 0);

        _mediaExtensionIcon = new Image
        {
            Width = 24,
            Height = 24
        };
        _border.Child = _mediaExtensionIcon;

        _editableTextBlock = new EditableTextBlock();
        Uids.SetUid(_editableTextBlock, "MediaName_TextBlock");
        _editableTextBlock.SetValue(Grid.ColumnProperty, 1);
        _editableTextBlock.TextConfirmed += (_, args) => UpdateMedia(args);

        _grid.Children.Add(_border);
        _grid.Children.Add(_editableTextBlock);
        Parent.Children.Add(_grid);
    }

    protected override bool ShowInfo(ICollection<Media> medias)
    {
        return true;
    }

    protected override void SetupTranslations() { }

    protected override void Show()
    {
        if (_grid != null)
        {
            _grid.Visibility = Visibility.Visible;
        }
    }

    protected override void Hide()
    {
        if (_grid != null)
        {
            _grid.Visibility = Visibility.Collapsed;
        }
    }

    private async void UpdateMedia(TextConfirmedArgs args)
    {
        if (Medias.Count == 0 || args.OldText == args.NewText) return;

        foreach (Media media in Medias)
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
        if (_mediaExtensionIcon != null)
        {
            _mediaExtensionIcon.Source = null;
            BitmapSource? icon = await IconService.GetIcon(media.Uri, ImageMode.IconOnly, 24, 24, tcs);

            if (Medias.Count == 1 && !tcs.Task.IsCompleted)
            {
                await App.DispatcherQueue.EnqueueAsync(() => _mediaExtensionIcon.Source = icon);
            }
        }
    }

    protected override void InvokeMediaChange()
    {
        if (Medias.Count == 0) return;
        MediaDbContext.InvokeMediaChange(this, MediaChangeFlags.MediaChanged | MediaChangeFlags.NameChanged, Medias);
    }

    protected override void MediaChanged(object? sender, MediaChangeArgs args)
    {
        HashSet<int> mediaIds = Medias.Select(m => m.MediaId).ToHashSet();
        
        if (Medias.Count == 0 ||
            !args.MediaIds.Intersect(mediaIds).Any() ||
            ReferenceEquals(sender, this) ||
            !args.Flags.HasFlag(MediaChangeFlags.NameChanged) &&
            !args.Flags.HasFlag(MediaChangeFlags.UriChanged)) return;
        
        Medias = args.Medias.Where(media => mediaIds.Contains(media.MediaId)).ToList();
        UpdateControlContent();
    }
}