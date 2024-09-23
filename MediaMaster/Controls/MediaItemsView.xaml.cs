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

namespace MediaMaster.Controls;

[DependencyProperty("SelectionMode", typeof(ItemsViewSelectionMode), DefaultValue = ItemsViewSelectionMode.Multiple)]
[DependencyProperty("CanSelectAll", typeof(bool), DefaultValue = true, IsReadOnly = false)]
[DependencyProperty("CanDeselectAll", typeof(bool), DefaultValue = true, IsReadOnly = true)]
[DependencyProperty("MediasCount", typeof(int), DefaultValue = 0, IsReadOnly = true)]
[DependencyProperty("MediasFound", typeof(int), DefaultValue = 0, IsReadOnly = true)]
[DependencyProperty("MediasSelectedCount", typeof(int), DefaultValue = 0, IsReadOnly = true)]
[DependencyProperty("IsSearching", typeof(bool), DefaultValue = false, IsReadOnly = true)]
public partial class MediaItemsView : UserControl
{
    private readonly int _pageSize = 250;
    private readonly TasksService _tasksService = App.GetService<TasksService>();
    public readonly ICollection<Expression<Func<Media, bool>>> AdvancedFilterFunctions = [];
    public readonly ICollection<Expression<Func<Media, bool>>> SimpleFilterFunctions = [];

    private TaskCompletionSource? _taskSource;
    
    public event TypedEventHandler<object, HashSet<int>>? SelectionChanged;

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
                var medias = (ICollection<NameUri>)MediaItemsViewControl.ItemsSource;
                foreach (Media updatedMedia in args.Medias)
                {
                    NameUri? media = medias.FirstOrDefault(m => m.MediaId == updatedMedia.MediaId);
                    if (media != null)
                    {
                        media.Name = updatedMedia.Name;
                        media.Uri = updatedMedia.Uri;
                    }
                }
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
        List<NameUri> medias = [];

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

        HashSet<int> selectedMedias = MediaItemsViewControl.SelectedItems.OfType<Media>().Select(m => m.MediaId).ToHashSet();

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

    private void SetupIcons(IEnumerable<NameUri> medias)
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
            foreach (NameUri media in medias.Reverse())
            {
                tasks.Add(IconService.GetIconAsync(media.Uri, ImageMode.IconAndThumbnail, 128, 128, tcs));
            }

            await Task.WhenAll(tasks).ConfigureAwait(false);
        });
    }

    private void MediaItemsView_OnSelectionChanged(ItemsView? sender = null, ItemsViewSelectionChangedEventArgs? args = null)
    {
        App.DispatcherQueue.EnqueueAsync(() =>
            SelectionChanged?.Invoke(this, MediaItemsViewControl.SelectedItems.OfType<NameUri>().Select(m => m.MediaId).ToHashSet()));
        SetupSelectionPermissions();
    }

    private void SetupSelectionPermissions()
    {
        var selectedCount = MediaItemsViewControl.SelectedItems.Count;
        var mediaCount = ((ICollection<NameUri>)MediaItemsViewControl.ItemsSource).Count;
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
            var mediaIds = MediaItemsViewControl.SelectedItems.OfType<NameUri>().Select(n => n.MediaId).ToHashSet();

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
}

public partial class NameUri : ObservableObject
{
    public int MediaId;
    [ObservableProperty] private string _name = "";
    [ObservableProperty] private string _uri = "";
}