using System.Collections;
using System.Numerics;
using Windows.Foundation;
using CommunityToolkit.WinUI;
using CommunityToolkit.WinUI.Collections;
using DependencyPropertyGenerator;
using Microsoft.UI.Input;
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

internal partial class ItemTemplateSelector : DataTemplateSelector
{
    public DataTemplate ItemTemplate { get; set; } = null!;
    public DataTemplate AddItemTemplate { get; set; } = null!;

    protected override DataTemplate SelectTemplateCore(object item)
    {
        return item is AddItem ? AddItemTemplate : ItemTemplate;
    }
}

[DependencyProperty("ItemsSource", typeof(ObservableCollection<object>), DefaultValueExpression = "new ObservableCollection<object>()")]
[DependencyProperty("Comparer", typeof(IComparer))]
[DependencyProperty("SelectionMode", typeof(ItemsViewSelectionMode), DefaultValue = ItemsViewSelectionMode.None)]
[DependencyProperty("AddItemButton", typeof(bool), DefaultValue = true)]
[DependencyProperty("ShowScrollButtons", typeof(bool), DefaultValue = true)]
[DependencyProperty("Layout", typeof(Layout), DefaultValueExpression = "new StackLayout { Orientation = Orientation.Horizontal, Spacing = 8 }")]
[DependencyProperty("ItemTemplate", typeof(DataTemplate))]
public partial class CustomItemsView
{
    private DateTime _lastScrollTime = DateTime.Now;

    private int _operationCount;

    public CustomItemsView()
    {
        InitializeComponent();
        ItemsSource = [new AddItem()];
        ItemsView.SizeChanged += (_, _) => UpdateScrollButtonsVisibility();
    }

    private ScrollView? ScrollView => ItemsView
        .FindDescendants()
        .OfType<ScrollView>()
        .FirstOrDefault(i => i.Name == "PART_ScrollView");

    partial void OnItemsSourceChanged(ObservableCollection<object> newValue)
    {
        HandleAddItemButton();

        var advancedCollectionView = new AdvancedCollectionView(newValue);
        advancedCollectionView.SortDescriptions.Add(new SortDescription(SortDirection.Ascending, Comparer));
        ItemsView.ItemsSource = advancedCollectionView;
    }

    partial void OnComparerChanged(IComparer? newValue)
    {
        var advancedCollectionView = new AdvancedCollectionView(ItemsSource);
        advancedCollectionView.SortDescriptions.Add(new SortDescription(SortDirection.Ascending, newValue));
        ItemsView.ItemsSource = advancedCollectionView;
    }

    partial void OnAddItemButtonChanged()
    {
        HandleAddItemButton();
    }

    partial void OnShowScrollButtonsChanged(bool newValue)
    {
        if (newValue)
        {
            UpdateScrollButtonsVisibility();
        }
        else
        {
            ScrollBackBtn.Visibility = Visibility.Collapsed;
            ScrollForwardBtn.Visibility = Visibility.Collapsed;
        }
    }

    public event TypedEventHandler<object, object>? RemoveItemsInvoked;
    public event TypedEventHandler<object, ICollection<object>>? SelectItemsInvoked;

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

    public void UpdateScrollButtonsVisibility(object? sender = null, SizeChangedEventArgs? args = null)
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
        var customItemContainer = button.FindAscendant<CustomItemContainer>();
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