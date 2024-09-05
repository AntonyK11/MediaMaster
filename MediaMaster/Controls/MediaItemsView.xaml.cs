using System.Linq.Expressions;
using Windows.Foundation;
using Windows.System;
using CommunityToolkit.WinUI;
using DependencyPropertyGenerator;
using MediaMaster.DataBase;
using MediaMaster.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Media;

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
    private readonly ICollection<Expression<Func<Media, bool>>> SimpleFilterFunctions = [];
    
    private TaskCompletionSource? _taskSource;
    
    public event TypedEventHandler<object, ICollection<Media>>? SelectionChanged;

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
                var medias = (ICollection<Media>)MediaItemsViewControl.ItemsSource;
                foreach (Media updatedMedia in args.Medias)
                {
                    Media? media = medias.FirstOrDefault(m => m.MediaId == updatedMedia.MediaId);
                    if (media != null)
                    {
                        media.Name = updatedMedia.Name;
                        media.Uri = updatedMedia.Uri;
                    }
                }
            }
        };

        Loaded += SetupMediaCollection;


        var cacheMode = new BitmapCache();
        MediaItemsViewControl.CacheMode = cacheMode;
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

        await Task.Yield();

        var currentPageIndex = PagerControl.SelectedPageIndex;
        var pageCount = 1;
        var mediasCount = 0;
        var mediasFound = 0;
        List<Media> medias = [];

        await Task.Run(async () =>
        {
            await using (var database = new MediaDbContext())
            {
                IQueryable<Media> mediaQuery = database.Medias;
                if (SortFunction != null)
                {
                    mediaQuery = SortAscending
                        ? SortMedias(mediaQuery).ThenBy(m => m.Name)
                        : SortMedias(mediaQuery).ThenByDescending(m => m.Name);
                }
                else
                {
                    mediaQuery = SortAscending
                        ? mediaQuery.OrderBy(m => m.Name)
                        : mediaQuery.OrderByDescending(m => m.Name);
                }

                mediaQuery = SimpleFilterMedias(mediaQuery);
                mediaQuery = AdvancedFilterMedias(mediaQuery);
                medias = await mediaQuery.Skip(currentPageIndex * _pageSize).Take(_pageSize).ToListAsync()
                    .ConfigureAwait(false);

                mediasCount = await database.Medias.CountAsync().ConfigureAwait(false);
                mediasFound = await mediaQuery.CountAsync().ConfigureAwait(false);
                pageCount = (int)Math.Round((double)mediasFound / _pageSize, MidpointRounding.ToPositiveInfinity);
            }
        });

        MediasCount = mediasCount;
        MediasFound = mediasFound;
        IsSearching = mediasCount != mediasFound || AdvancedFilterFunctions.Count != 0;

        pageCount = pageCount > 0 ? pageCount : 1;
        if (PagerControl.NumberOfPages > pageCount)
        {
            PagerControl.SelectedPageIndex = pageCount - 1;
        }

        PagerControl.NumberOfPages = pageCount;

        HashSet<int> selectedMedias =
            MediaItemsViewControl.SelectedItems.OfType<Media>().Select(m => m.MediaId).ToHashSet();
        MediaItemsViewControl.ItemsSource = medias;
        foreach (Media media in medias.Where(media => selectedMedias.Contains(media.MediaId)))
        {
            MediaItemsViewControl.Select(medias.IndexOf(media));
        }

        MediasSelectedCount = MediaItemsViewControl.SelectedItems.Count;

        MediaItemsViewControl.ScrollView.ScrollTo(0, 0);
        SetupSelectionPermissions();

        if (medias.Count != 0)
        {
            SetupIcons(medias);
        }

        _tasksService.RemoveMainTask();
    }

    private IOrderedQueryable<Media> SortMedias(IQueryable<Media> medias)
    {
        var sortFunction = (KeyValuePair<bool, Expression<Func<Media, object>>>)SortFunction!;
        return SortAscending ^ sortFunction.Key
            ? medias.OrderByDescending(sortFunction.Value)
            : medias.OrderBy(sortFunction.Value);
    }

    private IQueryable<Media> SimpleFilterMedias(IQueryable<Media> medias)
    {
        return SimpleFilterFunctions.Aggregate(medias, (current, filter) => current.Where(filter));
    }

    private IQueryable<Media> AdvancedFilterMedias(IQueryable<Media> medias)
    {
        return AdvancedFilterFunctions.Aggregate(medias, (current, filter) => current.Where(filter));
    }

    private void SetupIcons(IEnumerable<Media> medias)
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
            foreach (Media media in medias)
            {
                tasks.Add(IconService.GetIconAsync(media.Uri, ImageMode.IconAndThumbnail, 128, 128, tcs));
            }

            await Task.WhenAll(tasks).ConfigureAwait(false);
        });
    }

    private void MediaItemsView_OnSelectionChanged(ItemsView sender, ItemsViewSelectionChangedEventArgs args)
    {
        App.DispatcherQueue.EnqueueAsync(() =>
            SelectionChanged?.Invoke(this, MediaItemsViewControl.SelectedItems.OfType<Media>().ToList()));
        SetupSelectionPermissions();
    }

    private void SetupSelectionPermissions()
    {
        var selectedCount = MediaItemsViewControl.SelectedItems.Count;
        var mediaCount = ((ICollection<Media>)MediaItemsViewControl.ItemsSource).Count;
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

    private void MediaItemsView_OnProcessKeyboardAccelerators(UIElement sender,
        ProcessKeyboardAcceleratorEventArgs args)
    {
        if (args is { Modifiers: VirtualKeyModifiers.Control, Key: VirtualKey.A })
        {
            args.Handled = true;
            SelectAll();
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