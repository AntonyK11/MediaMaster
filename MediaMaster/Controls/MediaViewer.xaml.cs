using MediaMaster.DataBase;
using MediaMaster.DataBase.Models;
using MediaMaster.Services;
using MediaMaster.Services.MediaInfo;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;

namespace MediaMaster.Controls;

public sealed partial class MediaViewer : UserControl
{
    public static readonly DependencyProperty? MediaIdProperty
        = DependencyProperty.Register(
            nameof(MediaId),
            typeof(int),
            typeof(MediaViewer),
            new PropertyMetadata(null));

    public int? MediaId
    {
        get => (int)GetValue(MediaIdProperty);
        set
        {
            Visibility = value != null ? Visibility.Visible : Visibility.Collapsed;

            if (value == null || value == (int)GetValue(MediaIdProperty)) return;

            MediaExtensionIcon.Source = null;
            FindMedia((int)value);
            SetValue(MediaIdProperty, value);
        }
    }

    private readonly MediaInfoService _mediaInfoService;
    private MyCancellationTokenSource? _tokenSource;

    public MediaViewer()
    {
        InitializeComponent();

        NameTextBox.TextConfirmed += (_, args) => SaveMedia(args);

        _mediaInfoService = new MediaInfoService(StackPanel);
        _mediaInfoService.SetMedia(null);
    }

    public async void FindMedia(int mediaId)
    {
        Media? media;
        await using (var database = new MediaDbContext())
        {
            media = await database.FindAsync<Media>(mediaId);
        }

        _mediaInfoService.SetMedia(media);

        if (media == null) return;

        NameTextBox.Text = media.Name;
        MediaIcon.MediaPath = media.Uri;
        SetMediaExtensionIcon(media);
    }

    private void SetMediaExtensionIcon(Media media)
    {
        if (MediaId != null)
        {
            if (_tokenSource is { IsDisposed: false })
            {
                _tokenSource?.Cancel();
            }

            _tokenSource = IconService.AddImage(media.Uri, ImageMode.IconOnly, 24, 24, MediaExtensionIcon);
        }
    }

    private async void SaveMedia(TextConfirmedArgs args)
    {
        if (MediaId == null || args.OldText == args.NewText) return;

        await using (var database = new MediaDbContext())
        {
            var media = await database.FindAsync<Media>(MediaId);

            if (media == null) return;

            media.Name = args.NewText;
            media.Modified = DateTime.UtcNow;

            await database.SaveChangesAsync();
        }
    }
}