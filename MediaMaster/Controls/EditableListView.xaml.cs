using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using MediaMaster.Extensions;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;

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
            SetupEmptyStringValue();
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

    public EditableListView()
    {
        ItemsSource = [new StringValue()];
        InitializeComponent();
    }

    private void EditableTextBlock_OnTextConfirmed(EditableTextBlock sender, TextConfirmedArgs args)
    {
        SetupEmptyStringValue();
    }

    private void SetupEmptyStringValue()
    {
        if (ItemsSource.Count(v => v.Value.IsNullOrEmpty()) == 0)
        {
            ItemsSource.Add(new StringValue());
        }
        else
        {
            while (ItemsSource.Count(v => v.Value.IsNullOrEmpty()) > 1)
            {
                ItemsSource.Remove(ItemsSource.First(v => v.Value.IsNullOrEmpty()));
            }
        }
    }
}
