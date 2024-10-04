using MediaMaster.Interfaces.Services;

namespace MediaMaster.Services.Navigation;

public sealed class MainNavigationService(IPageService pageService) : NavigationService(pageService)
{
    public override Frame? Frame
    {
        get
        {
            if (_frame != null) return _frame;

            _frame = App.MainWindow?.Content as Frame;
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