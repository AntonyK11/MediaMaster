using System.Linq.Expressions;
using Windows.Foundation;
using MediaMaster.DataBase;
using Microsoft.UI.Xaml.Input;
using Windows.System;
using CommunityToolkit.WinUI;
using DependencyPropertyGenerator;
using MediaMaster.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.UI.Xaml.Media;

namespace MediaMaster.Controls;

[DependencyProperty("SelectionMode", typeof(ItemsViewSelectionMode), DefaultValue = ItemsViewSelectionMode.Multiple)]
[DependencyProperty("CanSelectAll", typeof(bool), DefaultValue = true, IsReadOnly = false)]
[DependencyProperty("CanDeselectAll", typeof(bool), DefaultValue = true, IsReadOnly = true)]
[DependencyProperty("MediasCount", typeof(int), DefaultValue = 0, IsReadOnly = true)]
[DependencyProperty("MediasSelectedCount", typeof(int), DefaultValue = 0, IsReadOnly = true)]
public sealed partial class MediaItemsView : UserControl
{
    public event TypedEventHandler<object, ICollection<Media>>? SelectionChanged;

    public MediaItemsView()
    {
        this.InitializeComponent();

        MediaDbContext.MediasChanged += async (sender, args) =>
        {
            if (args.Flags.HasFlag(MediaChangeFlags.MediaAdded) || args.Flags.HasFlag(MediaChangeFlags.MediaRemoved))
            {
                await SetupMediaCollection();
            }
        };

        Loaded += SetupMediaCollection;
        

        var cacheMode = new BitmapCache();
        MediaItemsViewControl.CacheMode = cacheMode;
    }

    private int _pageSize = 250;
    
    /// <summary>
    ///     The sort function for the media collection.
    /// </summary>
    /// <remarks>
    ///     The key is a boolean that indicates if the collection should be sorted in ascending order.
    ///     If the key is false the SortAscending will be inverted.
    ///     The value is an expression that indicates the property to sort by.
    ///  </remarks>
    
    // if the key is false the SortAscending will be inverted
    private KeyValuePair<bool, Expression<Func<Media, object>>> _sortFunction = new(true, m => m.Name);

    public KeyValuePair<bool, Expression<Func<Media, object>>> SortFunction
    {
        get => _sortFunction;
        set
        {
            _sortFunction = value;
            _ = SetupMediaCollection();
        }
    }

    public ICollection<Expression<Func<Media, bool>>> FilterFunctions = [];

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

    public async void SetupMediaCollection(object sender, RoutedEventArgs routedEventArgs)
    {
        Loaded -= SetupMediaCollection;
        await SetupMediaCollection();
        PagerControl.SelectedIndexChanged += async (_, args) =>
        {
            if (args.PreviousPageIndex != args.NewPageIndex && args.PreviousPageIndex < PagerControl.NumberOfPages && args.NewPageIndex < PagerControl.NumberOfPages)
            {
                await SetupMediaCollection();
            }
        };
    }

    public async Task SetupMediaCollection()
    {
        await Task.Yield();

        var currentPageIndex = PagerControl.SelectedPageIndex;
        var pageCount = 1;
        List<Media> medias = [];

        await Task.Run(async () =>
        {
            await using (var database = new MediaDbContext())
            {
                var itemNumber = await database.Medias.CountAsync().ConfigureAwait(false);
                pageCount = (int)Math.Round((double)itemNumber / _pageSize, MidpointRounding.ToPositiveInfinity);

                IQueryable<Media> mediaQuery = database.Medias;
                mediaQuery = SortMedias(mediaQuery);
                mediaQuery = FilterMedias(mediaQuery);
                medias = await mediaQuery.Skip(currentPageIndex * _pageSize).Take(_pageSize).ToListAsync().ConfigureAwait(false);
            }
        });

        PagerControl.NumberOfPages = pageCount > 0 ? pageCount : 1;

        var selectedMedias = MediaItemsViewControl.SelectedItems.OfType<Media>().Select(m => m.MediaId).ToHashSet();
        MediaItemsViewControl.ItemsSource = medias;
        foreach (var media in medias.Where(media => selectedMedias.Contains(media.MediaId)))
        {
            MediaItemsViewControl.Select(medias.IndexOf(media));
        }

        MediasCount = medias.Count;
        MediasSelectedCount = MediaItemsViewControl.SelectedItems.Count;

        MediaItemsViewControl.ScrollView.ScrollTo(0, 0);
        SetupSelectionPermissions();

        if (medias.Count != 0)
        {
            SetupIcons(medias);
        }
    }

    public IQueryable<Media> SortMedias(IQueryable<Media> medias)
    {
        return SortAscending ^ SortFunction.Key ? medias.OrderByDescending(SortFunction.Value) : medias.OrderBy(SortFunction.Value);
    }

    public IQueryable<Media> FilterMedias(IQueryable<Media> medias)
    {
        return FilterFunctions.Aggregate(medias, (current, filter) => current.Where(filter));
    }

    private MyCancellationTokenSource? _tokenSource;

    private void SetupIcons(IEnumerable<Media> medias)
    {
        if (_tokenSource is { IsDisposed: false })
        {
            try
            {
                _tokenSource.Cancel();
            }
            catch (ObjectDisposedException) { }
        }
        _tokenSource = new MyCancellationTokenSource();

        var cts = _tokenSource;
        STATask.StartSTATask(async () =>
        {
            using (cts)
            {
                ICollection<Task> tasks = [];
                foreach (var media in medias)
                {
                    tasks.Add(IconService.GetIconAsync(media.Uri, ImageMode.IconAndThumbnail, 128, 128, cts));
                }

                await Task.WhenAll(tasks).ConfigureAwait(false);
            }
        });
    }

    private void MediaItemsView_OnSelectionChanged(ItemsView sender, ItemsViewSelectionChangedEventArgs args)
    {
        App.DispatcherQueue.EnqueueAsync(() => SelectionChanged?.Invoke(this, MediaItemsViewControl.SelectedItems.OfType<Media>().ToList()));
        SetupSelectionPermissions();
    }

    private void SetupSelectionPermissions()
    {
        var count = MediaItemsViewControl.SelectedItems.Count;
        if (count == 0)
        {
            CanSelectAll = true;
            CanDeselectAll = false;
        }
        else if (count == _pageSize || count == ((ICollection<Media>)MediaItemsViewControl.ItemsSource).Count)
        {
            CanSelectAll = false;
            CanDeselectAll = true;
        }
        else
        {
            CanSelectAll = true;
            CanDeselectAll = true;
        }
        MediasSelectedCount = count;
    }

    private void MediaItemsView_OnProcessKeyboardAccelerators(UIElement sender, ProcessKeyboardAcceleratorEventArgs args)
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