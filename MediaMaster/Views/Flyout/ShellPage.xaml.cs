using MediaMaster.Services;
using MediaMaster.Services.Navigation;
using MediaMaster.ViewModels.Flyout;

namespace MediaMaster.Views.Flyout;

public sealed partial class ShellPage : Page
{
    public ShellViewModel ViewModel { get; }
    private readonly TasksService _tasksService = App.GetService<TasksService>();

    public ShellPage()
    {
        InitializeComponent();
        Loaded += OnLoaded;
        App.GetService<FlyoutNavigationService>().Frame = ContentFrame;
        ViewModel = App.GetService<ShellViewModel>();
    }

    public void OnLoaded(object sender, RoutedEventArgs e)
    {
        App.Flyout!.Activated += MainWindow_Activated;
    }

    public void Hide_Flyout(object sender, RoutedEventArgs routedEventArgs)
    {
        App.Flyout?.Hide_Flyout();
    }

    public void MainWindow_Activated(object sender, WindowActivatedEventArgs args)
    {
        VisualStateManager.GoToState(TitleBarCloseButton,
            args.WindowActivationState != WindowActivationState.Deactivated
                ? "WindowActivated"
                : "WindowDeactivated", false);
    }
}