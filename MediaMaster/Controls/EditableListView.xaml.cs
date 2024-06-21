using System.Collections.ObjectModel;
using Windows.System;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.WinUI;
using Microsoft.IdentityModel.Tokens;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Media;

namespace MediaMaster.Controls;

public partial class StringValue : ObservableObject
{
    [ObservableProperty] public string _value = "";
}

public sealed partial class EditableListView : UserControl
{
    public static readonly DependencyProperty ItemsSourceProperty
        = DependencyProperty.Register(
            nameof(ItemsSource),
            typeof(ObservableCollection<StringValue>),
            typeof(EditableListView),
            new PropertyMetadata(null));

    public ObservableCollection<StringValue> ItemsSource
    {
        get => (ObservableCollection<StringValue>)GetValue(ItemsSourceProperty);
        set
        {
            SetValue(ItemsSourceProperty, value);
            value.Add(new StringValue());
            foreach (var stringValue in value)
            {
                Update(stringValue);
            }
        }
    }

    public ICollection<string> Strings
    {
        get => ItemsSource.Select(v => v.Value).Where(v => !v.IsNullOrEmpty()).ToList();
        set
        {
            ItemsSource = new ObservableCollection<StringValue>(value.Select(a => new StringValue() { Value = a }));
        }
    }

    private void TextBox_OnLostFocus(object sender, RoutedEventArgs e)
    {
        if (((UIElement)sender).Visibility == Visibility.Visible)
        {
            Selector_OnSelectionChanged(ListView.SelectedItems.FirstOrDefault());
        }
    }

    public EditableListView()
    {
        ItemsSource = [];
        this.InitializeComponent();
        Loaded += (_, _) =>
        {
            foreach (var stringValue in ItemsSource)
            {
                Update(stringValue);
            }
        };

        ListView.SelectionChanged += (sender, args) => Selector_OnSelectionChanged(args.RemovedItems.FirstOrDefault());
    }

    private void UIElement_OnDoubleTapped(object sender, DoubleTappedRoutedEventArgs e)
    {
        if (sender is ListViewItem listViewItem)
        {
            var textBox = listViewItem.FindDescendants().OfType<TextBox>().FirstOrDefault(x => x.Name is "TextBox");
            var textBlock = listViewItem.FindDescendants().OfType<TextBlock>().FirstOrDefault(x => x.Name is "TextBlock");
            if (textBox == null || textBlock == null) return;

            textBox.Visibility = Visibility.Visible;
            textBlock.Visibility = Visibility.Collapsed;
            textBox.Focus(FocusState.Programmatic);
            textBox.SelectAll();
        }
    }

    private void Selector_OnSelectionChanged(object? itemRemoved)
    {
        if (itemRemoved == null) return;

        var listViewItem = ListView.ContainerFromItem(itemRemoved);
        var textBox = listViewItem.FindDescendants().OfType<TextBox>().FirstOrDefault(x => x.Name is "TextBox");
        var textBlock = listViewItem.FindDescendants().OfType<TextBlock>().FirstOrDefault(x => x.Name is "TextBlock");
        if (textBox == null || textBlock == null) return;

        if (((StringValue)itemRemoved).Value.IsNullOrEmpty() && !textBox.Text.IsNullOrEmpty())
        {
            var stringValue = new StringValue();
            ItemsSource.Add(stringValue);
            Update(stringValue);
        }

        ((StringValue)itemRemoved).Value = textBox.Text;
        Update(((StringValue)itemRemoved));
        textBlock.Text = textBox.Text;

        while (ItemsSource.Count(v => v.Value.IsNullOrEmpty()) > 1)
        {
            ItemsSource.Remove(ItemsSource.First(v => v.Value.IsNullOrEmpty()));
        }

        textBox.Visibility = Visibility.Collapsed;
        textBlock.Visibility = Visibility.Visible;
        textBox.SelectionStart = 0;
        textBox.SelectionLength = 0;
    }

    private async void Update(StringValue item)
    {
        await Task.Delay(10);
        var listViewItem = ListView.ContainerFromItem(item);
        if (listViewItem == null) return;
        var textBlock = listViewItem.FindDescendants().OfType<TextBlock>().FirstOrDefault(x => x.Name is "TextBlock");
        if (textBlock == null) return;

        if (item.Value.IsNullOrEmpty())
        {
            textBlock.CharacterSpacing = 0;
            textBlock.Foreground = (SolidColorBrush)Resources["TextControlPlaceholderForeground"];
            textBlock.Text = "Add new item";
        }
        else
        {
            textBlock.CharacterSpacing = 18;
            textBlock.Foreground = (SolidColorBrush)Resources["TextControlForeground"];
        }
    }

    private void TextBox_OnKeyDown(object sender, KeyRoutedEventArgs e)
    {
        if (e.Key == VirtualKey.Enter)
        {
            var item = (StringValue)((TextBox)sender).Tag;
            Selector_OnSelectionChanged(item);
            e.Handled = true;
        }
    }
}

