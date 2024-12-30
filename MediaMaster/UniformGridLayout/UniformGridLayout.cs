using System.Collections.Specialized;
using Windows.Foundation;
using DependencyPropertyGenerator;
using DocumentFormat.OpenXml.Spreadsheet;

namespace MediaMaster.UniformGridLayout;


[DependencyProperty("DesiredColumnWidth", typeof(double), DefaultValue = 250d)]
[DependencyProperty("ColumnSpacing", typeof(double), DefaultValue = 0d)]
[DependencyProperty("RowSpacing", typeof(double), DefaultValue = 0d)]
[DependencyProperty("ItemHeight", typeof(double), DefaultValue = 100d)]
internal partial class UniformGridLayout : VirtualizingLayout
{
    protected override void InitializeForContextCore(VirtualizingLayoutContext context)
    {
        base.InitializeForContextCore(context);
        context.LayoutState = new UniformGridLayoutState(context);
    }

    protected override void UninitializeForContextCore(VirtualizingLayoutContext context)
    {
        base.UninitializeForContextCore(context);
        context.LayoutState = null;
    }

    protected override void OnItemsChangedCore(VirtualizingLayoutContext context, object source, NotifyCollectionChangedEventArgs args)
    {
        if (context.LayoutState is not UniformGridLayoutState state) return;

        switch (args.Action)
        {
            case NotifyCollectionChangedAction.Add:
                state.RemoveFromIndex(args.NewStartingIndex);
                break;
            case NotifyCollectionChangedAction.Replace:
                state.RemoveFromIndex(args.NewStartingIndex);
                state.RecycleElementAt(args.NewStartingIndex);
                break;
            case NotifyCollectionChangedAction.Move:
                var minIndex = Math.Min(args.NewStartingIndex, args.OldStartingIndex);
                var maxIndex = Math.Max(args.NewStartingIndex, args.OldStartingIndex);
                state.RemoveRange(minIndex, maxIndex);
                break;
            case NotifyCollectionChangedAction.Remove:
                state.RemoveFromIndex(args.OldStartingIndex);
                break;
            case NotifyCollectionChangedAction.Reset:
                state.Clear();
                break;
        }

        base.OnItemsChangedCore(context, source, args);
    }

    protected override Size MeasureOverride(VirtualizingLayoutContext context, Size availableSize)
    {
        if (context.ItemCount == 0 || context.RealizationRect.Width == 0 || context.RealizationRect.Height == 0)
        {
            return new Size(availableSize.Width, 0);
        }

        var state = (UniformGridLayoutState)context.LayoutState;
        CalculateColumnMetrics(availableSize.Width, state);

        var totalHeight = ArrangeItems(context, state);

        EnsureSelectedItemVisible(context, state);

        return new Size(availableSize.Width, totalHeight);
    }

    protected override Size ArrangeOverride(VirtualizingLayoutContext context, Size finalSize)
    {
        if (context.RealizationRect.Width == 0 || context.RealizationRect.Height == 0)
        {
            return finalSize;
        }

        var state = (UniformGridLayoutState)context.LayoutState;
        foreach (var keyValue in state.Items)
        {
            keyValue.Value.Element?.Arrange(keyValue.Value.Rect);
        }

        return finalSize;
    }

    private void CalculateColumnMetrics(double availableWidth, UniformGridLayoutState state)
    {
        var desiredColumnWidth = DesiredColumnWidth;
        var columnSpacing = ColumnSpacing;

        if (double.IsNaN(desiredColumnWidth) || desiredColumnWidth >= availableWidth)
        {
            state.ColumnWidth = availableWidth;
            state.ColumnCount = 1;
            return;
        }

        var totalAvailableWidth = availableWidth + columnSpacing;
        state.ColumnCount = (int)Math.Floor(totalAvailableWidth / desiredColumnWidth);
        state.ColumnWidth = (totalAvailableWidth / state.ColumnCount) - columnSpacing;
    }

    private double ArrangeItems(VirtualizingLayoutContext context, UniformGridLayoutState state)
    {
        var itemHeight = ItemHeight;
        var rowSpacing = RowSpacing;
        var rowHeight = itemHeight + rowSpacing;
        var columnCount = state.ColumnCount;
        var itemSize = new Size(state.ColumnWidth, itemHeight);
        var columnWidth = state.ColumnWidth;
        var columnWidthWithSpacing = columnWidth + ColumnSpacing;
        var realizationBounds = context.RealizationRect;
        var itemCount = context.ItemCount;

        var startRow = (int)(Math.Max(realizationBounds.Top, 0) / rowHeight);
        var startIndex = startRow * columnCount;
        var endRow = (int)(realizationBounds.Bottom / rowHeight) + 1;
        var endIndex = Math.Min(itemCount, endRow * columnCount);

        //SelectedIndex = (int)(Math.Max(context.VisibleRect.Top, 0) / rowHeight) * columnCount;

        for (var i = 0; i < startIndex; i++)
        {
            state.RecycleElementAt(i);
        }

        for (var i = endIndex; i < itemCount; i++)
        {
            state.RecycleElementAt(i);
        }

        var top = startRow * rowHeight;
        var columnIndex = startIndex % columnCount;

        for (var i = startIndex; i < endIndex; i++)
        {
            var item = state.GetItemAt(i);
            var xOffset = columnIndex * columnWidthWithSpacing;

            item.Element = context.GetOrCreateElementAt(i, ElementRealizationOptions.SuppressAutoRecycle);
            item.Element.Measure(itemSize);
            item.Rect = new Rect(xOffset, top, columnWidth, itemHeight);

            if (++columnIndex == columnCount)
            {
                columnIndex = 0;
                top += rowHeight;
            }
        }

        var totalRows = (itemCount + columnCount - 1) / columnCount;

        return totalRows * rowHeight - rowSpacing;
    }

    //private int SelectedIndex = 200;
    //private double oldWidth = 0;
    private void EnsureSelectedItemVisible(VirtualizingLayoutContext context, UniformGridLayoutState state)
    {

        ////
        //if (SelectedIndex < 0 || SelectedIndex >= context.ItemCount || oldWidth == context.VisibleRect.Width)
        //{
        //    return;
        //}
        //oldWidth = context.VisibleRect.Width;


        //var item = state.GetItemAt(SelectedIndex);
        //var visibleBounds = context.VisibleRect;

        //// Calculate the item's position in the viewport coordinate system
        //var itemTop = item.Rect.Top + context.LayoutOrigin.Y;

        //Debug.WriteLine(itemTop);
        //Debug.WriteLine(visibleBounds.Top);
        //// If the item is above the visible area, shift the layout down.
        //if (itemTop < visibleBounds.Top)
        //{
        //    // Calculate how far we need to move down to bring the item into view.
        //    var shift = visibleBounds.Top - itemTop;
        //    //context.LayoutOrigin = new Point(context.LayoutOrigin.X, 0);
        //}

        ////oldWidth = context.VisibleRect.Width;

        ////var item = state.GetItemAt(SelectedIndex);

        ////var visibleBounds = context.VisibleRect;
        ////var itemTop = item.Rect.Top + context.LayoutOrigin.Y;
        //////var itemBottom = item.Rect.Bottom + context.LayoutOrigin.Y;
        //////Debug.WriteLine(item.Rect.Top);
        //////Debug.WriteLine(context.LayoutOrigin.Y);
        ////// If item is above the visible area, shift down
        //////if (itemTop < visibleBounds.Top)
        //////{
        ////    var shift = visibleBounds.Top - item.Rect.Top;
        ////    Debug.WriteLine(shift);
        ////    context.LayoutOrigin = new Point(context.LayoutOrigin.X,  shift);
        //////}
    }
}
