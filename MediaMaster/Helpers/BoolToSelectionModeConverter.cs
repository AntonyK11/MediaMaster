﻿using CommunityToolkit.WinUI.Converters;

namespace MediaMaster.Helpers;

public sealed partial class BoolToSelectionModeConverter : BoolToObjectConverter
{
    public BoolToSelectionModeConverter()
    {
        TrueValue = ItemsViewSelectionMode.Multiple;
        FalseValue = ItemsViewSelectionMode.Extended;
    }
}