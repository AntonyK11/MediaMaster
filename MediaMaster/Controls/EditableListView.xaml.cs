using CommunityToolkit.Mvvm.ComponentModel;
using DependencyPropertyGenerator;
using MediaMaster.Extensions;

namespace MediaMaster.Controls;

public sealed partial class StringValue : ObservableObject
{
    [ObservableProperty] public partial string Value { get; set; } = "";
}

[DependencyProperty("ItemsSource", typeof(ObservableCollection<StringValue>), DefaultValueExpression = "new ObservableCollection<StringValue>()")]
public sealed partial class EditableListView : UserControl
{
    public EditableListView()
    {
        ItemsSource = [new StringValue()];
        InitializeComponent();
    }

    public ICollection<string> Strings
    {
        get => ItemsSource.Select(v => v.Value).Where(v => !v.IsNullOrEmpty()).ToList();
        set => ItemsSource = new ObservableCollection<StringValue>(value.Select(a => new StringValue { Value = a }));
    }

    partial void OnItemsSourceChanged()
    {
        SetupEmptyStringValue();
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