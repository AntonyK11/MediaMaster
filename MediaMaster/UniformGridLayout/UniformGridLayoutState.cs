namespace MediaMaster.UniformGridLayout;

internal class UniformGridLayoutState(VirtualizingLayoutContext context)
{
    public readonly Dictionary<int, UniformGridItem> Items = [];
    public double ColumnWidth { get; internal set; }
    public int ColumnCount { get; internal set; }

    internal UniformGridItem GetItemAt(int index)
    {
        if (!Items.TryGetValue(index, out var item))
        {
            item = new UniformGridItem();
            Items.Add(index, item);
        }
        return item;
    }

    internal void Clear()
    {
        RemoveFromIndex(0);
        Items.Clear();
    }

    internal void RemoveFromIndex(int index)
    {
        RemoveRange(index, Items.Count);
    }

    internal void RemoveRange(int startIndex, int endIndex)
    {
        var keysToRemove = Items.Keys.Where(key => key >= startIndex && key <= endIndex).ToList();
        foreach (var key in keysToRemove)
        {
            RecycleElementAt(key);
            Items.Remove(key);
        }
    }

    internal void RecycleElementAt(int index)
    {
        if (Items.TryGetValue(index, out var item) && item.Element != null)
        {
            context.RecycleElement(item.Element);
            item.Element = null;
        }
    }
}
