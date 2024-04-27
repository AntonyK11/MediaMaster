using MediaMaster.Interfaces.Services;
using Microsoft.UI.Windowing;
using Microsoft.UI.Xaml;
using WinUIEx;
using MediaMaster.Services.Navigation;
using MediaMaster.Views.Flyout;
using Windows.Foundation;
using CommunityToolkit.WinUI;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media.Animation;

namespace MediaMaster;

/// <summary>
///     An empty window that can be used on its own or navigated to within a Frame.
/// </summary>
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

    public new event TypedEventHandler<object, bool>? VisibilityChanged;

    private readonly Storyboard _showStoryboard = new();
    private readonly Storyboard _hideStoryboard = new();

    private readonly DoubleAnimation _hideAnimation;
    private readonly DoubleAnimation _showAnimation;

    public FlyoutWindow()
    {
        InitializeComponent();

        App.GetService<IThemeSelectorService>().ThemeChanged += Flyout_ThemeChanged;
        Flyout_ThemeChanged(null, App.GetService<IThemeSelectorService>().ActualTheme);

        this.MoveAndResize(PosX, BottomY, WindowWidth, WindowHeight);
        ContentGrid.PosY = BottomY;

        Closed += (sender, e) =>
        {
            Hide_Flyout();
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

    }

    private void Flyout_ThemeChanged(object? sender, ElementTheme theme)
    {
        ((FrameworkElement)Content).RequestedTheme = theme;
    }

    public void Hide_Flyout()
    {
        _isOpen = false;

        IsAlwaysOnTop = false;

        _showStoryboard.Stop();

        _hideAnimation.To = BottomY;
        _hideStoryboard.Begin();
    }

    public void Show_Flyout()
    {
        _isOpen = true;
        App.DispatcherQueue.EnqueueAsync(() => VisibilityChanged?.Invoke(this, true));

        Activate();
        App.GetService<FlyoutNavigationService>().NavigateTo(typeof(HomePage).FullName!);
        IsAlwaysOnTop = false;

        _hideStoryboard.Stop();

        _showAnimation.To = TopY;
        _showStoryboard.Begin();
    }

    private bool _isOpen;

    public void Toggle_Flyout()
    {
        if (_isOpen)
        {
            Hide_Flyout();
        }
        else
        {
            Show_Flyout();
        }
    }

    private void OnPropertyChanged(DependencyObject sender, DependencyProperty dp)
    {
        this.SetForegroundWindow();
        this.Move((int)PosX, (int)ContentGrid.PosY);
        sender.SetValue(dp, ContentGrid.PosY);
    }
}

internal class ContentFrame : Frame
{
    public static readonly DependencyProperty PosYProperty
        = DependencyProperty.Register(
            nameof(PosY),
            typeof(double),
            typeof(ContentFrame),
            new PropertyMetadata(0.0));

    public double PosY
    {
        get => (double)GetValue(PosYProperty);
        set => SetValue(PosYProperty, value);
    }
}