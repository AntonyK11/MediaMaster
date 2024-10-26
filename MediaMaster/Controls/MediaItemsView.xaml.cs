using System.Linq.Expressions;
using Windows.Foundation;
using Windows.System;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.WinUI;
using DependencyPropertyGenerator;
using MediaMaster.DataBase;
using MediaMaster.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.UI.Xaml.Input;
using EFCore.BulkExtensions;

namespace MediaMaster.Controls;

[DependencyProperty("SelectionMode", typeof(ItemsViewSelectionMode), DefaultValue = ItemsViewSelectionMode.Multiple)]
[DependencyProperty("CanSelectAll", typeof(bool), DefaultValue = true, IsReadOnly = false)]
[DependencyProperty("CanDeselectAll", typeof(bool), DefaultValue = true, IsReadOnly = true)]
[DependencyProperty("MediasCount", typeof(int), DefaultValue = 0, IsReadOnly = true)]
[DependencyProperty("MediasFound", typeof(int), DefaultValue = 0, IsReadOnly = true)]
[DependencyProperty("MediasSelectedCount", typeof(int), DefaultValue = 0, IsReadOnly = true)]
[DependencyProperty("IsSearching", typeof(bool), DefaultValue = false, IsReadOnly = true)]
public sealed partial class MediaItemsView : UserControl
{
    private readonly int _pageSize = 250;
    private readonly TasksService _tasksService = App.GetService<TasksService>();
    public readonly ICollection<Expression<Func<Media, bool>>> AdvancedFilterFunctions = [];
    public readonly ICollection<Expression<Func<Media, bool>>> SimpleFilterFunctions = [];

    private TaskCompletionSource? _taskSource;
    
    public event TypedEventHandler<object, HashSet<int>>? SelectionChanged;
    private bool _updateFromUser = true;

    public MediaItemsView()
    {
        InitializeComponent();

        MediaDbContext.MediasChanged += async (sender, args) =>
        {
            if (args.Flags.HasFlag(MediaChangeFlags.MediaAdded) || args.Flags.HasFlag(MediaChangeFlags.MediaRemoved))
            {
                await SetupMediaCollection();
            }
            else
            {
                _updateFromUser = false;
                var medias = (ICollection<CompactMedia>)MediaItemsViewControl.ItemsSource;
                foreach (Media updatedMedia in args.Medias)
                {
                    CompactMedia? media = medias.FirstOrDefault(m => m.MediaId == updatedMedia.MediaId);
                    if (media != null)
                    {
                        media.Name = updatedMedia.Name;
                        media.Uri = updatedMedia.Uri;
                        media.IsFavorite = updatedMedia.IsFavorite;
                        media.IsArchived = updatedMedia.IsArchived;
                    }
                }
                _updateFromUser = true;
            }
        };

        Loaded += SetupMediaCollection;
    }
    
    private bool _sortAscending = true;
    public bool SortAscending
    {
        get => _sortAscending;
        set
        {
            _sortAscending = value;
            _ = SetupMediaCollection();
        }
    }


    /// <summary>
    ///     The sort function for the media collection.
    /// </summary>
    /// <remarks>
    ///     The key is a boolean that indicates if the collection should be sorted in ascending order.
    ///     If the key is false the SortAscending will be inverted.
    ///     The value is an expression that indicates the property to sort by.
    /// </remarks>
    private KeyValuePair<bool, Expression<Func<Media, object>>>? _sortFunction;
    
    public KeyValuePair<bool, Expression<Func<Media, object>>>? SortFunction
    {
        get => _sortFunction;
        set
        {
            _sortFunction = value;
            _ = SetupMediaCollection();
        }
    }

    private async void SetupMediaCollection(object sender, RoutedEventArgs routedEventArgs)
    {
        Loaded -= SetupMediaCollection;
        await SetupMediaCollection();
        PagerControl.SelectedIndexChanged += async (_, args) =>
        {
            if (args.PreviousPageIndex != args.NewPageIndex && args.PreviousPageIndex < PagerControl.NumberOfPages &&
                args.NewPageIndex < PagerControl.NumberOfPages)
            {
                await SetupMediaCollection();
            }
        };
    }

    public async Task SetupMediaCollection()
    {
        _tasksService.AddMainTask();

        var currentPageIndex = PagerControl.SelectedPageIndex;
        var mediasCount = 0;
        var mediasFound = 0;
        List<CompactMedia> medias = [];

        await Task.Run(async () =>
        {
            await using (var database = new MediaDbContext())
            {
                mediasCount = await database.Medias.CountAsync().ConfigureAwait(false);
            }

            (medias, mediasFound) = await SearchService.GetMedias(SortFunction, SortAscending, SimpleFilterFunctions, AdvancedFilterFunctions, currentPageIndex * _pageSize, _pageSize);
        });

        MediasCount = mediasCount;
        MediasFound = mediasFound;
        IsSearching = mediasCount != mediasFound || AdvancedFilterFunctions.Count != 0;

        var pageCount = (int)Math.Round((double)mediasFound / _pageSize, MidpointRounding.ToPositiveInfinity);
        pageCount = pageCount > 0 ? pageCount : 1;
        if (PagerControl.NumberOfPages > pageCount && PagerControl.SelectedPageIndex > pageCount - 1)
        {
            PagerControl.SelectedPageIndex = pageCount - 1;
        }

        PagerControl.NumberOfPages = pageCount;

        HashSet<int> selectedMedias = MediaItemsViewControl.SelectedItems.OfType<CompactMedia>().Select(m => m.MediaId).ToHashSet();

        MediaItemsViewControl.SelectionChanged -= MediaItemsView_OnSelectionChanged;
        MediaItemsViewControl.ItemsSource = medias;
        foreach (var media in medias.Where(media => selectedMedias.Contains(media.MediaId)))
        {
            MediaItemsViewControl.Select(medias.IndexOf(media));
        }
        MediaItemsViewControl.SelectionChanged += MediaItemsView_OnSelectionChanged;
        MediaItemsView_OnSelectionChanged();

        MediasSelectedCount = MediaItemsViewControl.SelectedItems.Count;

        SetupSelectionPermissions();

        if (medias.Count != 0)
        {
            SetupIcons(medias);
        }

        _tasksService.RemoveMainTask();
        MediaItemsViewControl.ScrollView.ScrollTo(0, 0);
    }

    private void SetupIcons(IEnumerable<CompactMedia> medias)
    {
        if (_taskSource is { Task.IsCompleted: false })
        {
            _taskSource.SetResult();
        }

        _taskSource = new TaskCompletionSource();

        TaskCompletionSource? tcs = _taskSource;
        STATask.StartSTATask(async () =>
        {
            ICollection<Task> tasks = [];
            foreach (CompactMedia media in medias.Reverse())
            {
                tasks.Add(IconService.GetIconAsync(media.Uri, ImageMode.IconAndThumbnail, 128, 128, tcs));
            }

            await Task.WhenAll(tasks).ConfigureAwait(false);
        });
    }

    private void MediaItemsView_OnSelectionChanged(ItemsView? sender = null, ItemsViewSelectionChangedEventArgs? args = null)
    {
        App.DispatcherQueue.EnqueueAsync(() =>
            SelectionChanged?.Invoke(this, MediaItemsViewControl.SelectedItems.OfType<CompactMedia>().Select(m => m.MediaId).ToHashSet()));
        SetupSelectionPermissions();
    }

    private void SetupSelectionPermissions()
    {
        var selectedCount = MediaItemsViewControl.SelectedItems.Count;
        var mediaCount = ((ICollection<CompactMedia>)MediaItemsViewControl.ItemsSource).Count;
        if (selectedCount == 0)
        {
            CanSelectAll = mediaCount != 0;
            CanDeselectAll = false;
        }
        else if (selectedCount == _pageSize || selectedCount == mediaCount)
        {
            CanSelectAll = false;
            CanDeselectAll = true;
        }
        else
        {
            CanSelectAll = true;
            CanDeselectAll = true;
        }

        MediasSelectedCount = selectedCount;
    }

    private async void MediaItemsView_OnProcessKeyboardAccelerators(UIElement sender,
        ProcessKeyboardAcceleratorEventArgs args)
    {
        if (args is { Modifiers: VirtualKeyModifiers.Control, Key: VirtualKey.A })
        {
            args.Handled = true;
            SelectAll();
        }
        else if (args.Key == VirtualKey.Delete)
        {
            args.Handled = true;
            var mediaIds = MediaItemsViewControl.SelectedItems.OfType<CompactMedia>().Select(n => n.MediaId).ToHashSet();

            List<Media> medias = [];
            await Task.Run(async () =>
            {
                await using (var database = new MediaDbContext())
                {
                    medias = await database.Medias.Where(m => mediaIds.Contains(m.MediaId)).ToListAsync();
                }
            });
            await MediaService.DeleteMedias(this, medias);
        }
    }

    public void SelectAll()
    {
        var oldSelectionCount = MediaItemsViewControl.SelectedItems.Count;
        MediaItemsViewControl.SelectAll();
        var newSelectionCount = MediaItemsViewControl.SelectedItems.Count;

        if (oldSelectionCount == newSelectionCount)
        {
            ClearSelection();
        }
    }

    public void ClearSelection()
    {
        MediaItemsViewControl.DeselectAll();
    }

    private void UIElement_OnPointerEntered(object sender, PointerRoutedEventArgs e)
    {
        var archiveToggleButton = ((ItemContainer)sender).FindDescendant<IconToggleButton>(s => s.Name == "ArchiveToggleButton");
        if (archiveToggleButton != null)
        {
            archiveToggleButton.Visibility = Visibility.Visible;
        }

        var favoriteToggleButton = ((ItemContainer)sender).FindDescendant<IconToggleButton>(s => s.Name == "FavoriteToggleButton");
        if (favoriteToggleButton != null)
        {
            favoriteToggleButton.Visibility = Visibility.Visible;
        }
    }

    private void UIElement_OnPointerExited(object sender, PointerRoutedEventArgs e)
    {
        var archiveToggleButton = ((ItemContainer)sender).FindDescendant<IconToggleButton>(s => s.Name == "ArchiveToggleButton");
        if (archiveToggleButton != null && !archiveToggleButton.IsChecked)
        {
            archiveToggleButton.Visibility = Visibility.Collapsed;
        }

        var favoriteToggleButton = ((ItemContainer)sender).FindDescendant<IconToggleButton>(s => s.Name == "FavoriteToggleButton");
        if (favoriteToggleButton != null && !favoriteToggleButton.IsChecked)
        {
            favoriteToggleButton.Visibility = Visibility.Collapsed;
        }
    }

    private void ToggleButton_OnChecked(object sender, RoutedEventArgs e)
    {
        ((FrameworkElement)sender).Visibility = Visibility.Visible;
    }

    private void ToggleButton_OnUnchecked(object sender, RoutedEventArgs e)
    {
        if (!_updateFromUser)
        {
            ((FrameworkElement)sender).Visibility = Visibility.Collapsed;
        }
    }

    private async void FavoriteToggleButton_OnClick(object sender, RoutedEventArgs args)
    {
        if (MediaDbContext.FavoriteTag != null)
        {
            var button = (IconToggleButton)sender;
            var mediaId = (int)button.Tag;
            Media? media;

            await using (var database = new MediaDbContext())
            {
                media = await database.Medias.FirstOrDefaultAsync(m => m.MediaId == mediaId);
                if (media != null)
                {
                    if (button.IsChecked)
                    {
                        var mediaTag = new MediaTag
                        {
                            MediaId = media.MediaId,
                            TagId = MediaDbContext.FavoriteTag.TagId
                        };
                        await database.BulkInsertAsync([mediaTag]);
                    }
                    else
                    {
                        var mediaTag = await database.MediaTags
                            .FirstOrDefaultAsync(m => m.MediaId == mediaId && m.TagId == MediaDbContext.FavoriteTag.TagId);
                        if (mediaTag != null)
                        {
                            await database.BulkDeleteAsync([mediaTag]);
                        }
                    }

                    media.IsFavorite = button.IsChecked;
                    media.Modified = DateTime.UtcNow;

                    await database.BulkUpdateAsync([media]);
                }
            }

            if (media != null)
            {
                if (button.IsChecked)
                {
                    MediaDbContext.InvokeMediaChange(this, MediaChangeFlags.MediaChanged | MediaChangeFlags.TagsChanged,
                        [media], tagsAdded: [MediaDbContext.FavoriteTag]);
                }
                else
                {
                    MediaDbContext.InvokeMediaChange(this, MediaChangeFlags.MediaChanged | MediaChangeFlags.TagsChanged,
                        [media], tagsRemoved: [MediaDbContext.FavoriteTag]);
                }
            }
        }
    }

    private async void ArchiveToggleButton_OnClick(object sender, RoutedEventArgs args)
    {
        if (MediaDbContext.ArchivedTag != null)
        {
            var button = (IconToggleButton)sender;
            var mediaId = (int)button.Tag;
            Media? media;

            await using (var database = new MediaDbContext())
            {
                media = await database.Medias.FirstOrDefaultAsync(m => m.MediaId == mediaId);
                if (media != null)
                {
                    if (button.IsChecked)
                    {
                        var mediaTag = new MediaTag
                        {
                            MediaId = media.MediaId,
                            TagId = MediaDbContext.ArchivedTag.TagId
                        };
                        await database.BulkInsertAsync([mediaTag]);
                    }
                    else
                    {
                        var mediaTag = await database.MediaTags
                            .FirstOrDefaultAsync(m => m.MediaId == mediaId && m.TagId == MediaDbContext.ArchivedTag.TagId);
                        if (mediaTag != null)
                        {
                            await database.BulkDeleteAsync([mediaTag]);
                        }
                    }

                    media.IsArchived = button.IsChecked;
                    media.Modified = DateTime.UtcNow;

                    await database.BulkUpdateAsync([media]);
                }
            }

            if (media != null)
            {
                if (button.IsChecked)
                {
                    MediaDbContext.InvokeMediaChange(this, MediaChangeFlags.MediaChanged | MediaChangeFlags.TagsChanged,
                        [media], tagsAdded: [MediaDbContext.ArchivedTag]);
                }
                else
                {
                    MediaDbContext.InvokeMediaChange(this, MediaChangeFlags.MediaChanged | MediaChangeFlags.TagsChanged,
                        [media], tagsRemoved: [MediaDbContext.ArchivedTag]);
                }
            }
        }
    }

    private void ToggleButton_OnDataContextChanged(FrameworkElement sender, DataContextChangedEventArgs args)
    {
        if (args.NewValue is bool shouldBeVisible)
        {
            if (((IconToggleButton)sender).IsUnderPointer)
            {
                sender.Visibility = Visibility.Visible;
            }
            else
            {
                sender.Visibility = shouldBeVisible ? Visibility.Visible : Visibility.Collapsed;
            }
        }
    }
}

public sealed partial class CompactMedia : ObservableObject
{
    public int MediaId;
    [ObservableProperty] private string _name = "";
    [ObservableProperty] private string _uri = "";
    [ObservableProperty] private bool _isArchived = false;
    [ObservableProperty] private bool _isFavorite = false;
}