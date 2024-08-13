using System.Collections.ObjectModel;
using System.Linq.Expressions;
using System.Numerics;
using Windows.Foundation;
using CommunityToolkit.WinUI.Collections;
using MediaMaster.DataBase;
using Microsoft.UI.Composition;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Input;
using Windows.System;
using CommunityToolkit.WinUI;
using MediaMaster.DataBase.Models;
using MediaMaster.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.UI.Xaml.Media;

namespace MediaMaster.Controls;

public sealed partial class MediaItemsView : UserControl
{
    public static readonly DependencyProperty SelectionModeProperty
        = DependencyProperty.Register(
            nameof(SelectionModeProperty),
            typeof(ItemsViewSelectionMode),
            typeof(MediaItemsView),
            new PropertyMetadata(ItemsViewSelectionMode.Multiple));

    public ItemsViewSelectionMode SelectionMode
    {
        get => (ItemsViewSelectionMode)GetValue(SelectionModeProperty);
        set => SetValue(SelectionModeProperty, value);
    }

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
            SetupMediaCollection().ConfigureAwait(false);
        }
    }

    private bool _sortAscending = true;

    public bool SortAscending
    {
        get => _sortAscending;
        set
        {
            _sortAscending = value;
            SetupMediaCollection().ConfigureAwait(false);
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
        IconService.ClearCache();
        await Task.Yield();

        var currentPageIndex = PagerControl.SelectedPageIndex;
        var pageCount = 1;
        ICollection<Media> medias = [];

        await Task.Run(async () =>
        {
            await using (var database = new MediaDbContext())
            {
                var itemNumber = await database.Medias.CountAsync();
                pageCount = (int)Math.Round((double)itemNumber / _pageSize, MidpointRounding.ToPositiveInfinity);

                IQueryable<Media> mediaQuery = database.Medias;
                mediaQuery = SortMedias(mediaQuery);
                medias = await mediaQuery.Skip(currentPageIndex * _pageSize).Take(_pageSize).ToListAsync();
            }
        });

        PagerControl.NumberOfPages = pageCount > 0 ? pageCount : 1;
        MediaItemsViewControl.ItemsSource = medias;
        MediaItemsViewControl.ScrollView.ScrollTo(0, 0);

        if (medias.Count != 0)
        {
            SetupIcons(medias);
        }
    }

    public IQueryable<Media> SortMedias(IQueryable<Media> medias)
    {
        return SortAscending ^ SortFunction.Key ? medias.OrderByDescending(SortFunction.Value) : medias.OrderBy(SortFunction.Value);
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

                await Task.WhenAll(tasks);
            }
        });
    }

    private void MediaItemsView_OnSelectionChanged(ItemsView sender, ItemsViewSelectionChangedEventArgs args)
    {
        App.DispatcherQueue.EnqueueAsync(() => SelectionChanged?.Invoke(this, MediaItemsViewControl.SelectedItems.OfType<Media>().ToList()));
    }

    private void MediaItemsView_OnProcessKeyboardAccelerators(UIElement sender, ProcessKeyboardAcceleratorEventArgs args)
    {
        if (args is { Modifiers: VirtualKeyModifiers.Control, Key: VirtualKey.A })
        {
            args.Handled = true;
            var oldSelectionCount = MediaItemsViewControl.SelectedItems.Count;
            MediaItemsViewControl.SelectAll();
            var newSelectionCount = MediaItemsViewControl.SelectedItems.Count;

            if (oldSelectionCount == newSelectionCount)
            {
                MediaItemsViewControl.DeselectAll();
            }
        }
    }
}