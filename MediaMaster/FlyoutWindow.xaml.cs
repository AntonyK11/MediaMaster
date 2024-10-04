using MediaMaster.Interfaces.Services;
using Microsoft.UI.Windowing;
using WinUIEx;
using MediaMaster.Services.Navigation;
using MediaMaster.Views.Flyout;
using Windows.Foundation;
using Microsoft.UI.Xaml.Media.Animation;
using CommunityToolkit.WinUI;
using DependencyPropertyGenerator;
using H.NotifyIcon.EfficiencyMode;
using MediaMaster.Services;
using Microsoft.UI.Xaml.Navigation;

namespace MediaMaster;

public sealed partial class FlyoutWindow
{
    private const int WindowMargin = 12;
    private const int WindowWidth = 386;
    private const double WindowHeight = 486;

    private double DpiScale => (float)this.GetDpiForWindow() / 96;

    private DisplayArea DisplayArea => DisplayArea.GetFromWindowId(AppWindow.Id, DisplayAreaFallback.Nearest);
    private double PosX => DisplayArea.WorkArea.Width - DpiScale * (WindowWidth + WindowMargin);
    private double TopY => DisplayArea.WorkArea.Height - DpiScale * (WindowHeight + WindowMargin);
    private double BottomY => DisplayArea.OuterBounds.Height;

    private readonly bool _windows10 = Environment.OSVersion.Version.Build < 22000;

    private readonly Storyboard _showStoryboard = new();
    private readonly Storyboard _hideStoryboard = new();

    private readonly DoubleAnimation _hideAnimation;
    private readonly DoubleAnimation _showAnimation;

    public new event TypedEventHandler<object, bool>? VisibilityChanged;
    public bool AutoClose;

    public FlyoutWindow()
    {
        InitializeComponent();

        App.GetService<FlyoutNavigationService>().Navigated += (_, args) =>
        {
            if (args.NavigationMode == NavigationMode.Back)
            {
                AutoClose = false;
            }
        };

        App.GetService<IThemeSelectorService>().ThemeChanged += Flyout_ThemeChanged;
        Flyout_ThemeChanged(null, App.GetService<IThemeSelectorService>().ActualTheme);

        this.MoveAndResize(PosX, BottomY, WindowWidth, WindowHeight);
        ContentGrid.PosY = BottomY;

        Closed += (_, e) =>
        {
            HideFlyout();
            e.Handled = true;
        };

        ContentGrid.RegisterPropertyChangedCallback(ContentFrame.PosYProperty, OnPropertyChanged);

        var duration = new Duration(TimeSpan.FromMilliseconds(167));


        _hideAnimation = new DoubleAnimation
        {
            EasingFunction = new CubicEase { EasingMode = EasingMode.EaseIn },
            Duration = duration
        };

        _hideStoryboard.Children.Add(_hideAnimation);
        _hideAnimation.EnableDependentAnimation = true;
        Storyboard.SetTarget(_hideAnimation, ContentGrid);
        Storyboard.SetTargetProperty(_hideAnimation, "PosY");

        _hideStoryboard.Completed += (sender, e) =>
        {
            this.Hide();
            App.DispatcherQueue.EnqueueAsync(() => VisibilityChanged?.Invoke(this, false));
        };

        _showAnimation = new DoubleAnimation
        {
            EasingFunction = new CubicEase { EasingMode = EasingMode.EaseOut },
            Duration = duration
        };

        _showStoryboard.Children.Add(_showAnimation);
        _showAnimation.EnableDependentAnimation = true;
        Storyboard.SetTarget(_showAnimation, ContentGrid);
        Storyboard.SetTargetProperty(_showAnimation, "PosY");

        _showStoryboard.Completed += (sender, e) =>
        {
            IsAlwaysOnTop = true;
        };
        ExtendsContentIntoTitleBar = true;
    }

    private void Flyout_ThemeChanged(object? sender, ElementTheme theme)
    {
        ((FrameworkElement)Content).RequestedTheme = theme;
    }

    public void HideFlyout()
    {
        IsOpen = false;

        IsAlwaysOnTop = false;

        _showStoryboard.Stop();

        _hideAnimation.To = BottomY;
        _hideStoryboard.Begin();

        App.GetService<FlyoutNavigationService>().GoBack();
        if (App.MainWindow == null || !App.MainWindow.GetWindowStyle().HasFlag(WindowStyle.Visible))
        {
            if (App.GetService<SettingsService>().LeaveAppRunning)
            {
                EfficiencyModeUtilities.SetEfficiencyMode(true);
            }
            else
            {
                App.Shutdown();
            }
        }
    }

    public void ShowFlyout()
    {
        EfficiencyModeUtilities.SetEfficiencyMode(false);

        IsOpen = true;
        App.DispatcherQueue.EnqueueAsync(() => VisibilityChanged?.Invoke(this, true));

        Activate();
        App.GetService<FlyoutNavigationService>().NavigateTo(typeof(HomePage).FullName!);
        IsAlwaysOnTop = false;

        _hideStoryboard.Stop();

        _showAnimation.To = TopY;
        _showStoryboard.Begin();
    }

    public bool IsOpen { get; private set; }

    public void ToggleFlyout()
    {
        if (IsOpen)
        {
            HideFlyout();
        }
        else
        {
            ShowFlyout();
        }
    }

    private void OnPropertyChanged(DependencyObject sender, DependencyProperty dp)
    {
        this.SetForegroundWindow();
        this.Move((int)PosX, (int)ContentGrid.PosY);
        sender.SetValue(dp, ContentGrid.PosY);
    }
}

[DependencyProperty("PosY", typeof(double), DefaultValue = 0.0)]
internal partial class ContentFrame : Frame;