using System.Collections;
using System.Collections.ObjectModel;
using System.Numerics;
using Windows.Foundation;
using CommunityToolkit.WinUI;
using CommunityToolkit.WinUI.Collections;
using Microsoft.UI.Input;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Input;

namespace MediaMaster.Controls;

public class AddItem;

public class ItemsComparer : IComparer
{
    public static readonly IComparer Instance = new ItemsComparer();

    public int Compare(object? x, object? y)
    {
        if (x is AddItem)
        {
            return 1;
        }
        if (y is AddItem)
        {
            return -1;
        }

        return 0;
    }
}

internal class ItemTemplateSelector : DataTemplateSelector
{
    public DataTemplate ItemTemplate { get; set; } = null!;
    public DataTemplate AddItemTemplate { get; set; } = null!;

    protected override DataTemplate SelectTemplateCore(object item)
    {
        return item is AddItem ? AddItemTemplate : ItemTemplate;
    }
}

public sealed partial class CustomItemsView
{
    public static readonly DependencyProperty ItemsSourceProperty
        = DependencyProperty.Register(
            nameof(ItemsSource),
            typeof(ObservableCollection<object>),
            typeof(CustomItemsView),
            new PropertyMetadata(null));
    
    public ObservableCollection<object> ItemsSource
    {
        get => (ObservableCollection<object>)GetValue(ItemsSourceProperty);
        set
        {
            SetValue(ItemsSourceProperty, value);
            HandleAddItemButton();

            var advancedCollectionView = new AdvancedCollectionView(value);
            advancedCollectionView.SortDescriptions.Add(new SortDescription(SortDirection.Ascending, Comparer));
            ItemsView.ItemsSource = advancedCollectionView;
        }
    }

    public static readonly DependencyProperty ComparerProperty
        = DependencyProperty.Register(
            nameof(Comparer),
            typeof(IComparer),
            typeof(CustomItemsView),
            new PropertyMetadata(null));

    public IComparer? Comparer
    {
        get => (IComparer?)GetValue(ComparerProperty);
        set
        {
            SetValue(ComparerProperty, value);

            var advancedCollectionView = new AdvancedCollectionView(ItemsSource);
            advancedCollectionView.SortDescriptions.Add(new SortDescription(SortDirection.Ascending, value));
            ItemsView.ItemsSource = advancedCollectionView;
        }
    }

    public static readonly DependencyProperty SelectionModeProperty
        = DependencyProperty.Register(
            nameof(SelectionMode),
            typeof(ItemsViewSelectionMode),
            typeof(CustomItemsView),
            new PropertyMetadata(ItemsViewSelectionMode.None));
    
    public ItemsViewSelectionMode SelectionMode
    {
        get => (ItemsViewSelectionMode)GetValue(SelectionModeProperty);
        set => SetValue(SelectionModeProperty, value);
    }

    public static readonly DependencyProperty AddItemButtonProperty
        = DependencyProperty.Register(
            nameof(AddItemButton),
            typeof(bool),
            typeof(CustomItemsView),
            new PropertyMetadata(true));
    
    public bool AddItemButton
    {
        get => (bool)GetValue(AddItemButtonProperty);
        set
        {
            SetValue(AddItemButtonProperty, value);
            HandleAddItemButton();
        }
    }

    public static readonly DependencyProperty ShowScrollButtonsProperty
        = DependencyProperty.Register(
            nameof(ShowScrollButtons),
            typeof(bool),
            typeof(CustomItemsView),
            new PropertyMetadata(true));
    
    public bool ShowScrollButtons
    {
        get => (bool)GetValue(ShowScrollButtonsProperty);
        set
        {
            SetValue(ShowScrollButtonsProperty, value);
            if (value)
            {
                UpdateScrollButtonsVisibility();
            }
            else
            {
                ScrollBackBtn.Visibility = Visibility.Collapsed;
                ScrollForwardBtn.Visibility = Visibility.Collapsed;
            }
        }
    }

    public static readonly DependencyProperty LayoutProperty
        = DependencyProperty.Register(
            nameof(Layout),
            typeof(Layout),
            typeof(CustomItemsView),
            new PropertyMetadata(new StackLayout { Orientation = Orientation.Horizontal, Spacing = 8 }));

    public Layout Layout
    {
        get => (Layout)GetValue(LayoutProperty);
        set => SetValue(LayoutProperty, value);
    }
    
    public static readonly DependencyProperty ItemTemplateProperty
        = DependencyProperty.Register(
            nameof(ItemTemplate),
            typeof(DataTemplate),
            typeof(CustomItemsView),
            new PropertyMetadata(null));

    public DataTemplate ItemTemplate
    {
        get => (DataTemplate)GetValue(ItemTemplateProperty);
        set => SetValue(ItemTemplateProperty, value);
    }

    public event TypedEventHandler<object, object>? RemoveItemsInvoked;
    public event TypedEventHandler<object, ICollection<object>>? SelectItemsInvoked;
    
    private DateTime _lastScrollTime = DateTime.Now;

    private int _operationCount;
    
    public CustomItemsView()
    {
        InitializeComponent();
        ItemsSource = [new AddItem()];
        ItemsView.SizeChanged += (_, _) => UpdateScrollButtonsVisibility();
    }

    private void HandleAddItemButton()
    {
        var addItem = ItemsSource.FirstOrDefault(t => t is AddItem);
        if (!AddItemButton)
        {
            if (addItem != null)
            {
                ItemsSource.Remove(addItem);
            }
        }
        else if (addItem == null)
        {
            addItem = new AddItem();
            ItemsSource.Add(addItem);
        }
    }

    public ICollection<T> GetItemSource<T>()
    {
        return ItemsSource.Where(e => e is T and not AddItem).Cast<T>().ToList();
    }
    
    private ScrollView? ScrollView => ItemsView.FindDescendants().OfType<ScrollView>()
        .FirstOrDefault(i => i.Name == "PART_ScrollView");

    private void UpdateScrollButtonsVisibility(object? sender = null, SizeChangedEventArgs? args = null)
    {
        if (!ShowScrollButtons || ScrollView == null)
        {
            return;
        }

        if (ScrollView.HorizontalOffset < 1)
        {
            ScrollBackBtn.Visibility = Visibility.Collapsed;
        }
        else if (ScrollView.HorizontalOffset > 1)
        {
            ScrollBackBtn.Visibility = Visibility.Visible;
        }

        if (ScrollView.HorizontalOffset > ScrollView.ScrollableWidth - 1)
        {
            ScrollForwardBtn.Visibility = Visibility.Collapsed;
        }
        else if (ScrollView.HorizontalOffset < ScrollView.ScrollableWidth - 1)
        {
            ScrollForwardBtn.Visibility = Visibility.Visible;
        }
    }

    private void ScrollBackBtn_Click(object sender, RoutedEventArgs e)
    {
        if (ScrollView == null) return;
        ScrollBy(-ScrollView.ViewportWidth);
    }

    private void ScrollForwardBtn_Click(object sender, RoutedEventArgs e)
    {
        if (ScrollView == null) return;
        ScrollBy(ScrollView.ViewportWidth);
    }

    private void PART_DeleteButton_OnClick(object sender, RoutedEventArgs e)
    {
        var button = (Button)sender;
        CustomItemContainer? customItemContainer = button.FindAscendant<CustomItemContainer>();
        if (customItemContainer != null)
        {
            RemoveItem(customItemContainer.DataContext);
        }
    }

    public void RemoveItem(object item)
    {
        RemoveItemsInvoked?.Invoke(this, item);
    }

    private void PART_AddButton_OnClick(object sender, RoutedEventArgs e)
    {
        SelectItemsInvoked?.Invoke(this, GetItemSource<object>());
    }

    private void PART_ScrollView_OnPointerWheelChanged(object sender, PointerRoutedEventArgs e)
    {
        if (ScrollView == null || ScrollView.ScrollableWidth == 0)
        {
            return;
        }

        e.Handled = true;

        PointerPointProperties? properties = e.GetCurrentPoint(ScrollView).Properties;
        var delta = properties.MouseWheelDelta;
        var scrollAmount = ScrollView.ViewportWidth / 5;
        var offsetDelta = delta > 0 ? -scrollAmount : scrollAmount;

        ScrollBy(offsetDelta);
    }

    private void ScrollBy(double scrollAmount)
    {
        if (ScrollView == null)
        {
            return;
        }

        if (DateTime.Now - _lastScrollTime > TimeSpan.FromMilliseconds(100))
        {
            _operationCount = 0;
        }

        const double minimumVelocity = 30.0;
        //const float inertiaDecayRate = 0.95f;
        //const double velocityNeededPerPixel = 2.995733261108394;
        const float inertiaDecayRate = 0.9995f;
        const double velocityNeededPerPixel = 7.600855902349023;

        var offsetVelocity = _operationCount == 0 ? minimumVelocity : 0.0;
        _operationCount++;

        if (scrollAmount < 0.0)
        {
            offsetVelocity *= -1;
        }

        offsetVelocity += scrollAmount * velocityNeededPerPixel;
        ScrollView.AddScrollVelocity(new Vector2((float)offsetVelocity, 0), new Vector2(inertiaDecayRate, 0));
    }

    private void PART_ScrollView_OnViewChanged(ScrollView sender, object args)
    {
        UpdateScrollButtonsVisibility();
        _lastScrollTime = DateTime.Now;
    }
}