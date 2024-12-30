using Windows.Foundation;

namespace MediaMaster.UniformGridLayout;

internal record UniformGridItem()
{
    public Rect Rect { get; internal set; }

    public UIElement? Element { get; internal set; }
}
