using CommunityToolkit.WinUI.Converters;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;

namespace MediaMaster.Helpers;

/// <summary>
///     Converts a null value to a <see cref="Visibility" /> value.
/// </summary>
public class BoolToSelectionModeConverter : BoolToObjectConverter
{
    public BoolToSelectionModeConverter()
    {
        TrueValue = ItemsViewSelectionMode.Multiple;
        FalseValue = ItemsViewSelectionMode.Extended;
    }
}