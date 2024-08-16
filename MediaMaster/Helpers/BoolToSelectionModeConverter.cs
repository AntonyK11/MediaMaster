using CommunityToolkit.WinUI.Converters;namespace MediaMaster.Helpers;

/// <summary>
///     Converts a null value to a <see cref="Visibility" /> value.
/// </summary>
public partial class BoolToSelectionModeConverter : BoolToObjectConverter
{
    public BoolToSelectionModeConverter()
    {
        TrueValue = ItemsViewSelectionMode.Multiple;
        FalseValue = ItemsViewSelectionMode.Extended;
    }
}