using System.Diagnostics;
using Windows.Storage.Pickers;
using EFCore.BulkExtensions;
using MediaMaster.DataBase;
using MediaMaster.DataBase.Models;
using MediaMaster.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using WinUIEx;

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
            SetValue(MediaIdProperty, value);

            Visibility = value != null ? Visibility.Visible : Visibility.Collapsed;

            TagView.MediaId = value;
            MediaExtensionIcon.Source = null;

            if (value == null) return;

            Media? media;
            using (var database = new MediaDbContext())
            {
                media = database.Find<Media>(value);
            }

            if (media == null) return;

            NameTextBox.Text = media.Name;
            PathTextBox.Text = media.FilePath;
            DescriptionTextBox.Text = media.Description;
            MediaIcon.MediaPath = media.FilePath;
            SetMediaExtensionIcon();
        }
    }

    private MyCancellationTokenSource? _tokenSource;

    public MediaViewer()
    {
        InitializeComponent();

        NameTextBox.TextConfirmed += (_, _) => SaveMedia();
        DescriptionTextBox.TextConfirmed += (_, _) => SaveMedia();

        TagView.SelectTagsInvoked += (_, _) => SaveSelectedTags(TagView.Tags);
        TagView.RemoveTagsInvoked += (_, _) => SaveSelectedTags(TagView.Tags);
    }

    private async void SaveSelectedTags(IEnumerable<Tag> selectedTags)
    {
        await using (MediaDbContext dataBase = new())
        {
            if (MediaId != null)
            {
                var trackedMedia = await dataBase.Medias.Select(m => new { m.MediaId, Tags = m.Tags.Select(t => new { t.TagId }) }).FirstOrDefaultAsync(m => m.MediaId == MediaId);

                if (trackedMedia != null)
                {
                    HashSet<int> currentTagIds = trackedMedia.Tags.Select(t => t.TagId).ToHashSet();
                    HashSet<int> selectedTagIds = selectedTags.Select(t => t.TagId).ToHashSet();

                    List<int> tagsToAdd = selectedTagIds.Except(currentTagIds).ToList();
                    List<int> tagsToRemove = currentTagIds.Except(selectedTagIds).ToList();

                    // Bulk add new tags
                    if (tagsToAdd.Count != 0)
                    {
                        List<MediaTag> newMediaTags = tagsToAdd.Select(tagId => new MediaTag { MediaId = trackedMedia.MediaId, TagId = tagId }).ToList();
                        await dataBase.BulkInsertAsync(newMediaTags);
                    }

                    // Bulk remove old tags
                    if (tagsToRemove.Count != 0)
                    {
                        List<MediaTag> mediaTagsToRemove = await dataBase.MediaTags
                            .Where(mt => mt.MediaId == trackedMedia.MediaId && tagsToRemove.Contains(mt.TagId))
                            .ToListAsync();
                        await dataBase.BulkDeleteAsync(mediaTagsToRemove);
                    }
                }
            }
        }
    }

    private void SetMediaExtensionIcon()
    {
        if (MediaId != null)
        {
            if (_tokenSource is { IsDisposed: false })
            {
                _tokenSource?.Cancel();
            }

            _tokenSource = IconService.AddImage1(PathTextBox.Text, ImageMode.IconOnly, 24, 24, MediaExtensionIcon);
        }
    }

    private async void PathTextBox_OnEdit(EditableTextBlock sender, string args)
    {
        if (App.MainWindow == null) return;

        var openPicker = new FileOpenPicker();

        var hWnd = App.MainWindow.GetWindowHandle();

        WinRT.Interop.InitializeWithWindow.Initialize(openPicker, hWnd);

        openPicker.ViewMode = PickerViewMode.Thumbnail;

        //openPicker.SuggestedStartLocation = PickerLocationId.ComputerFolder Path.GetDirectoryName(PathTextBox.Text);
        openPicker.FileTypeFilter.Add("*");
        openPicker.CommitButtonText = "hello";

        var file = await openPicker.PickSingleFileAsync();
    }

    private async void SaveMedia()
    {
        if (MediaId == null) return;

        await using (var database = new MediaDbContext())
        {
            var media = await database.FindAsync<Media>(MediaId);


            if (media == null) return;

            media.Name = NameTextBox.Text;
            media.Description = DescriptionTextBox.Text;

            await database.SaveChangesAsync();
        }
    }
}