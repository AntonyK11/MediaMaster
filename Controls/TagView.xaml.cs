using System.Collections.ObjectModel;
using System.Numerics;
using System.Runtime.CompilerServices;
using System.Security.Cryptography;
using System.Text;
using Microsoft.UI.Input;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Automation;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Input;
using Windows.System;
using Windows.UI;
using Windows.UI.Core;
using CommunityToolkit.WinUI;
using Microsoft.UI.Xaml.Media;


namespace MediaMaster.Controls;

public enum ItemsViewDeleteMode
{
    Enabled,
    Disabled
}

public sealed partial class TagView
{
    public static readonly DependencyProperty ItemsSourceProperty
        = DependencyProperty.Register(
            nameof(ItemsSource),
            typeof(object),
            typeof(TagView),
            new PropertyMetadata(null));

    public object ItemsSource
    {
        get => GetValue(ItemsSourceProperty);
        set
        {
            SetValue(ItemsSourceProperty, value);
            if (value is ObservableCollection<Tag> observableCollection)
            {
                observableCollection.CollectionChanged += (_, _) =>
                {
                    ScrollView.ScrollBy(ScrollView.ViewportWidth / 10000000, 0);
                };
            }
        }
    }

    public static readonly DependencyProperty SelectionModeProperty
        = DependencyProperty.Register(
            nameof(SelectionMode),
            typeof(ItemsViewSelectionMode),
            typeof(TagView),
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
            typeof(TagView),
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

    public static readonly DependencyProperty ShowScrollButtonsProperty
        = DependencyProperty.Register(
            nameof(ShowScrollButtons),
            typeof(bool),
            typeof(TagView),
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

    private ScrollView ScrollView => ItemsViewer.FindDescendants().OfType<ScrollView>().FirstOrDefault(i => i.Name == "PART_ScrollView")!;

    public TagView()
    {
        InitializeComponent();
        ItemsViewer.SizeChanged += (_, _) => UpdateScrollButtonsVisibility();

        ItemsViewer.Loaded += (_, _) =>
        {
            var itemsRepeater = ItemsViewer.FindDescendants().OfType<ItemsRepeater>().FirstOrDefault(i => i.Name == "PART_ItemsRepeater");
            if (itemsRepeater == null) return;
            
            itemsRepeater.ElementPrepared += (_, args) =>
            {
                var itemContainer = (ItemContainer)args.Element;
                itemContainer.ApplyTemplate();
                var result = VisualStateManager.GoToState(itemContainer, DeleteMode == ItemsViewDeleteMode.Enabled ? "EnableDelete" : "DisableDelete", true);
            };

            SetDeleteState(DeleteMode == ItemsViewDeleteMode.Enabled);
        };
    }

    private void SetDeleteState(bool state)
    {
        var itemsRepeater = ItemsViewer.FindDescendants().OfType<ItemsRepeater>().FirstOrDefault(i => i.Name == "PART_ItemsRepeater");
        if (itemsRepeater == null) return;

        int count = VisualTreeHelper.GetChildrenCount(itemsRepeater);
        for (int childIndex = 0; childIndex < count; childIndex++)
        {
            var itemContainer = (ItemContainer)VisualTreeHelper.GetChild(itemsRepeater, childIndex);
            VisualStateManager.GoToState(itemContainer, state ? "EnableDelete" : "DisableDelete", true);
        }
    }

    public void UpdateScrollButtonsVisibility(object? n = null, object? n2 = null)
    {
        if (!ShowScrollButtons)
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
        ScrollBy(-ScrollView.ViewportWidth);

    }

    private void ScrollForwardBtn_Click(object sender, RoutedEventArgs e)
    {
        ScrollBy(ScrollView.ViewportWidth);
    }

    private void ButtonBase_OnClick(object sender, RoutedEventArgs e)
    {
        var button = ((Button)sender);
        var tag = ((Grid)button.Parent).FindDescendants().OfType<TextBlock>().FirstOrDefault(t => t.Name == "TextBlock").Text;
        RemoveTag(tag);
    }

    private void OnKeyDown(object sender, KeyRoutedEventArgs e)
    {
        if (DeleteMode == ItemsViewDeleteMode.Enabled && e.Key is VirtualKey.Delete)
        {
            var tag = (string)((ItemContainer)sender).GetValue(AutomationProperties.NameProperty);
            RemoveTag(tag);
        }
    }

    public void RemoveTag(string Name)
    {
        if (ItemsSource is not Collection<Tag> itemsSource)
        {
            return;
        }

        var tag = itemsSource.FirstOrDefault(t => t.Name == Name);

        if (tag != null)
        {
            itemsSource.Remove(tag);
        }
    }

    private int _operationCount;

    private DateTime _lastScrollTime = DateTime.Now;

    private void PART_ScrollView_OnPointerWheelChanged(object sender, PointerRoutedEventArgs e)
    {
        e.Handled = true;

        var properties = e.GetCurrentPoint(ScrollView).Properties;
        var delta = properties.MouseWheelDelta;
        var scrollAmount = ScrollView.ViewportWidth/5;
        var offsetDelta = delta > 0 ? -scrollAmount : scrollAmount;

        ScrollBy(offsetDelta);
    }

    private void ScrollBy(double scrollAmount)
    {

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

public class Tag
{
    private string _name = null!;
    public required string Name
    {
        get => _name;
        set
        {
            _name = value;
            CalculateColor();
        }
    }

    public SolidColorBrush? Color { get; private set; }

    public Tag()
    {
        CalculateColor();
    }

    private void CalculateColor()
    {
        if (string.IsNullOrEmpty(Name))
        {
            return;
        }
        var hash = SHA256.HashData(Encoding.UTF8.GetBytes(Name));
        var index = 0;

        var color = Windows.UI.Color.FromArgb(50, hash[0], hash[1], hash[2]);
        while (CalculateContrastRatio(Windows.UI.Color.FromArgb(255, 255, 255, 255), color) < 4.5 ||
               CalculateContrastRatio(Windows.UI.Color.FromArgb(255, 0, 0, 0), color) < 4.5)
        {
            index++;
            if (index + 2 >= hash.Length)
            {
                index = 0;
                hash = SHA256.HashData(Encoding.UTF8.GetBytes(Convert.ToBase64String(hash)));
            }
            color = Windows.UI.Color.FromArgb(50, hash[index], hash[index + 1], hash[index + 2]);
        }
        Color = new SolidColorBrush(color);
    }

    // https://github.com/microsoft/WinUI-Gallery/blob/main/WinUIGallery/ControlPages/Accessibility/AccessibilityColorContrastPage.xaml.cs#L67-L86
    // Find the contrast ratio: https://www.w3.org/WAI/GL/wiki/Contrast_ratio
    public static double CalculateContrastRatio(Color first, Color second)
    {
        var relLuminanceOne = GetRelativeLuminance(first);
        var relLuminanceTwo = GetRelativeLuminance(second);
        return (Math.Max(relLuminanceOne, relLuminanceTwo) + 0.05)
               / (Math.Min(relLuminanceOne, relLuminanceTwo) + 0.05);
    }

    // Get relative luminance: https://www.w3.org/WAI/GL/wiki/Relative_luminance
    public static double GetRelativeLuminance(Color c)
    {
        var rSRGB = c.R / 255.0;
        var gSRGB = c.G / 255.0;
        var bSRGB = c.B / 255.0;

        var r = rSRGB <= 0.04045 ? rSRGB / 12.92 : Math.Pow(((rSRGB + 0.055) / 1.055), 2.4);
        var g = gSRGB <= 0.04045 ? gSRGB / 12.92 : Math.Pow(((gSRGB + 0.055) / 1.055), 2.4);
        var b = bSRGB <= 0.04045 ? bSRGB / 12.92 : Math.Pow(((bSRGB + 0.055) / 1.055), 2.4);
        return 0.2126 * r + 0.7152 * g + 0.0722 * b;
    }
}

public class CustomItemsView : ItemsView
{
    public CustomItemsView()
    {
        ProtectedCursor = InputCursor.CreateFromCoreCursor(new CoreCursor(CoreCursorType.Arrow, 0));
    }
}

