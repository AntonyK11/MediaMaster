using Windows.Media.Core;
using MediaMaster.Interfaces.Services;
using MediaMaster.Services;
using MediaMaster.Services.Navigation;
using MediaMaster.ViewModels.Flyout;

namespace MediaMaster.Views.Flyout;

public partial class ShellPage : Page
{
    public ShellViewModel ViewModel { get; }

    private readonly TasksService _tasksService = App.GetService<TasksService>();

    private readonly ITeachingService TeachingService;

    public ShellPage()
    {
        InitializeComponent();
        Loaded += OnLoaded;
        App.GetService<FlyoutNavigationService>().Frame = ContentFrame;
        ViewModel = App.GetService<ShellViewModel>();
        TeachingService = App.GetService<ITeachingService>();

        TeachingService.Configure(2, DragTeachingTip);
        DragMediaPlayerElement.MediaPlayer.IsLoopingEnabled = true;
        DragMediaPlayerElement.MediaPlayer.IsMuted = true;
        DragMediaPlayerElement.MediaPlayer.AutoPlay = true;

        TeachingService.Configure(3, ContextMenuTeachingTip);
        ContextMenuMediaPlayerElement.MediaPlayer.IsLoopingEnabled = true;
        ContextMenuMediaPlayerElement.MediaPlayer.IsMuted = true;
        ContextMenuMediaPlayerElement.MediaPlayer.AutoPlay = true;

        SetTeachingTipSource(App.GetService<IThemeSelectorService>().ActualTheme);
        App.GetService<IThemeSelectorService>().ThemeChanged += (_, theme) => SetTeachingTipSource(theme);
    }

    private readonly MediaSource _lightSourceDrag = MediaSource.CreateFromUri(new Uri("ms-appx:///Assets/DragInFlyout_Light.mp4"));
    private readonly MediaSource _darkSourceDrag = MediaSource.CreateFromUri(new Uri("ms-appx:///Assets/DragInFlyout_Dark.mp4"));
    private readonly MediaSource _lightSourceContextMenu = MediaSource.CreateFromUri(new Uri("ms-appx:///Assets/ContextMenu_Light.mp4"));
    private readonly MediaSource _darkSourceContextMenu = MediaSource.CreateFromUri(new Uri("ms-appx:///Assets/ContextMenu_Dark.mp4"));

    private void SetTeachingTipSource(ElementTheme theme)
    {
        if (theme == ElementTheme.Light)
        {
            DragMediaPlayerElement.Source = _lightSourceDrag;
            ContextMenuMediaPlayerElement.Source = _lightSourceContextMenu;
        }
        else
        {
            DragMediaPlayerElement.Source = _darkSourceDrag;
            ContextMenuMediaPlayerElement.Source = _darkSourceContextMenu;
        }
    }

    private void OnLoaded(object sender, RoutedEventArgs e)
    {
        App.Flyout!.Activated += MainWindow_Activated;
    }

    private void Hide_Flyout(object sender, RoutedEventArgs routedEventArgs)
    {
        App.Flyout?.HideFlyout();
    }

    private void MainWindow_Activated(object sender, WindowActivatedEventArgs args)
    {
        VisualStateManager.GoToState(TitleBarCloseButton,
            args.WindowActivationState != WindowActivationState.Deactivated
                ? "WindowActivated"
                : "WindowDeactivated", false);
    }
}