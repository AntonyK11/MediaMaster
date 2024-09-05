using Windows.Foundation;

namespace MediaMaster.Controls;

public partial class ComboBoxEx : ComboBox
{
    private double _cachedWidth;

    public ComboBoxEx()
    {
        Style = (Style)Application.Current.Resources["DefaultComboBoxStyle"];
    }

    protected override void OnDropDownOpened(object e)
    {
        Width = _cachedWidth;

        base.OnDropDownOpened(e);
    }

    protected override void OnDropDownClosed(object e)
    {
        Width = double.NaN;

        base.OnDropDownClosed(e);
    }

    protected override Size MeasureOverride(Size availableSize)
    {
        Size baseSize = base.MeasureOverride(availableSize);
        _cachedWidth = baseSize.Width;

        return baseSize;
    }
}