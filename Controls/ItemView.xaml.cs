using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.Numerics;
using Windows.Foundation;
using CommunityToolkit.WinUI;
using Microsoft.UI.Input;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Media;

namespace MediaMaster.Controls;

public enum ItemsViewDeleteMode
{
    Enabled,
    Disabled
}

public class AddItem
{
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

public sealed partial class ItemView
{
    public static readonly DependencyProperty ItemsSourceProperty
        = DependencyProperty.Register(
            nameof(ItemsSource),
            typeof(ObservableCollection<object>),
            typeof(ItemView),
            new PropertyMetadata(null));
    
    public ObservableCollection<object> ItemsSource
    {
        get
        {
            var collection = (ObservableCollection<object>?)GetValue(ItemsSourceProperty);
            if (collection == null)
            {
                collection = new ObservableCollection<object>();
                collection.CollectionChanged += OnCollectionChanged;
            }

            return collection;
        }
        set
        {
            SetValue(ItemsSourceProperty, value);
            //if (value is ObservableCollection<Tag> observableCollection)
            //{
            //    observableCollection.CollectionChanged += (_, _) =>
            //    {
            //        ScrollView.ScrollBy(ScrollView.ViewportWidth / 10000000, 0);
            //    };
            //}

            if (AddItemButton)
            {
                var addItem = value.FirstOrDefault(t => t is AddItem);
                if (addItem == null)
                {
                    addItem = new AddItem();
                    value.Add(addItem);
                }
            }

            value.CollectionChanged += OnCollectionChanged;
        }
    }

    public static readonly DependencyProperty SelectionModeProperty
        = DependencyProperty.Register(
            nameof(SelectionMode),
            typeof(ItemsViewSelectionMode),
            typeof(ItemView),
            new PropertyMetadata(ItemsViewSelectionMode.None));

    public ItemsViewSelectionMode SelectionMode
    {
        get => (ItemsViewSelectionMode)GetValue(SelectionModeProperty);
        set => SetValue(SelectionModeProperty, value);
    }
    
    public static readonly DependencyProperty DeleteModeProperty
        = DependencyProperty.Register(
            nameof(DeleteMode),
            typeof(ItemsViewDeleteMode),
            typeof(ItemView),
            new PropertyMetadata(ItemsViewDeleteMode.Disabled));

    public ItemsViewDeleteMode DeleteMode
    {
        get => (ItemsViewDeleteMode)GetValue(DeleteModeProperty);
        set
        {
            SetValue(DeleteModeProperty, value);
            SetDeleteState(value == ItemsViewDeleteMode.Enabled);
        }
    }

    public static readonly DependencyProperty AddItemButtonProperty
        = DependencyProperty.Register(
            nameof(AddItemButton),
            typeof(bool),
            typeof(ItemView),
            new PropertyMetadata(true));

    public bool AddItemButton
    {
        get => (bool)GetValue(AddItemButtonProperty);
        set
        {
            SetValue(AddItemButtonProperty, value);
            var addItem = ItemsSource.FirstOrDefault(t => t is AddItem);
            if (!value)
            {
                if (addItem != null)
                {
                    ItemsSource.Remove(addItem);
                }
            }
            else
            {
                if (addItem == null)
                {
                    addItem = new AddItem();
                    ItemsSource.Add(addItem);
                }
            }
        }
    }

    public static readonly DependencyProperty ShowScrollButtonsProperty
        = DependencyProperty.Register(
            nameof(ShowScrollButtons),
            typeof(bool),
            typeof(ItemView),
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
            typeof(ItemView),
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
            typeof(ItemView),
            new PropertyMetadata(null));

    public DataTemplate ItemTemplate
    {
        get => (DataTemplate)GetValue(ItemTemplateProperty);
        set => SetValue(ItemTemplateProperty, value);
    }

    private DateTime _lastScrollTime = DateTime.Now;

    private int _operationCount;

    public ItemView()
    {
        InitializeComponent();
        ItemsViewer.SizeChanged += (_, _) => UpdateScrollButtonsVisibility();

        ItemsViewer.Loaded += (_, _) =>
        {
            ItemsRepeater? itemsRepeater = ItemsViewer.FindDescendants().OfType<ItemsRepeater>()
                .FirstOrDefault(i => i.Name == "PART_ItemsRepeater");
            if (itemsRepeater == null) return;

            itemsRepeater.ElementPrepared += (_, args) =>
            {
                var itemContainer = (ItemContainer)args.Element;
                itemContainer.ApplyTemplate();
                VisualStateManager.GoToState(itemContainer,
                    DeleteMode == ItemsViewDeleteMode.Enabled ? "EnableDelete" : "DisableDelete", true);
            };

            SetDeleteState(DeleteMode == ItemsViewDeleteMode.Enabled);
        };
    }

    private ScrollView? ScrollView => ItemsViewer.FindDescendants().OfType<ScrollView>()
        .FirstOrDefault(i => i.Name == "PART_ScrollView")!;

    public event TypedEventHandler<object, ICollection<object>>? RemoveItemsInvoked;
    public event TypedEventHandler<object, ICollection<object>>? SelectItemsInvoked;

    public ICollection<T> GetItemSource<T>()
    {
        return ItemsSource.Where(e => e is T and not AddItem).Cast<T>().ToList();
    }

    private async void OnCollectionChanged(object? sender,
        NotifyCollectionChangedEventArgs notifyCollectionChangedEventArgs)
    {
        if (AddItemButton && sender is ObservableCollection<object> collection)
        {
            await Task.Delay(1);
            var addItem = collection.FirstOrDefault(t => t is AddItem);

            if (addItem != null)
            {
                var index = collection.IndexOf(addItem);
                if (index != collection.Count - 1)
                {
                    collection.Move(index, collection.Count - 1);
                }
            }
            else
            {
                addItem = new AddItem();
                collection.Add(addItem);
            }
        }
    }

    private void SetDeleteState(bool state)
    {
        ItemsRepeater? itemsRepeater = ItemsViewer.FindDescendants().OfType<ItemsRepeater>()
            .FirstOrDefault(i => i.Name == "PART_ItemsRepeater");
        if (itemsRepeater == null) return;

        var count = VisualTreeHelper.GetChildrenCount(itemsRepeater);
        for (var childIndex = 0; childIndex < count; childIndex++)
        {
            var itemContainer = (ItemContainer)VisualTreeHelper.GetChild(itemsRepeater, childIndex);
            VisualStateManager.GoToState(itemContainer, state ? "EnableDelete" : "DisableDelete", true);
        }
    }

    public void UpdateScrollButtonsVisibility(object? n = null, object? n2 = null)
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
        if (ScrollView == null)
        {
            return;
        }

        ScrollBy(-ScrollView.ViewportWidth);
    }

    private void ScrollForwardBtn_Click(object sender, RoutedEventArgs e)
    {
        if (ScrollView == null)
        {
            return;
        }

        ScrollBy(ScrollView.ViewportWidth);
    }

    private void PART_DeleteButton_OnClick(object sender, RoutedEventArgs e)
    {
        var button = (Button)sender;
        TextBlock? textBlock = ((Grid)button.Parent).FindDescendants().OfType<TextBlock>()
            .FirstOrDefault(t => t.Name == "TextBlock");
        if (textBlock == null) return;
        RemoveItem(textBlock.Tag);
    }

    public void RemoveItem(object item)
    {
        var itemToRemove = ItemsSource.FirstOrDefault(i => i == item);

        if (itemToRemove != null)
        {
            ItemsSource.Remove(itemToRemove);
            RemoveItemsInvoked?.Invoke(this, GetItemSource<object>());
        }
    }

    private void PART_AddButton_OnClick(object sender, RoutedEventArgs e)
    {
        SelectItemsInvoked?.Invoke(this, GetItemSource<object>());
    }

    private void PART_ScrollView_OnPointerWheelChanged(object sender, PointerRoutedEventArgs e)
    {
        if (ScrollView == null)
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

// public class CustomItemsView : ItemsView
// {
//     public CustomItemsView()
//     {
//         ProtectedCursor = InputCursor.CreateFromCoreCursor(new CoreCursor(CoreCursorType.Arrow, 0));
//     }
// }