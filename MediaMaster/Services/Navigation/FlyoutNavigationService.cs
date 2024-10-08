﻿using MediaMaster.Interfaces.Services;

namespace MediaMaster.Services.Navigation;

public sealed class FlyoutNavigationService(IPageService pageService) : NavigationService(pageService)
{
    public override Frame? Frame
    {
        get
        {
            if (_frame != null) return _frame;
            _frame = App.Flyout?.Content as Frame;
            RegisterFrameEvents();

            return _frame;
        }

        set
        {
            UnregisterFrameEvents();
            _frame = value;
            RegisterFrameEvents();
        }
    }
}