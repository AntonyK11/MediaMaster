using Windows.Graphics;
using MediaMaster.Interfaces.Services;
using Microsoft.UI.Windowing;
using Microsoft.UI.Xaml;
using WinUIEx;
using MediaMaster.Services.Navigation;
using MediaMaster.Views.Flyout;
using Windows.Foundation;
using CommunityToolkit.WinUI;

namespace MediaMaster;

/// <summary>
///     An empty window that can be used on its own or navigated to within a Frame.
/// </summary>
public sealed partial class FlyoutWindow
{
    private const int WindowWidth = 386;
    private const int WindowHeight = 486;
    private const int WindowMargin = 12;

    private const int Steps = 26;
    private int _currentStep;

    private readonly bool _windows10 = Environment.OSVersion.Version.Build < 22000;

    private readonly DispatcherTimer _hideTimer = new() { Interval = TimeSpan.FromMilliseconds(1) };
    private readonly DispatcherTimer _showTimer = new() {Interval = TimeSpan.FromMilliseconds(1) };

    private double DpiScale => (float)this.GetDpiForWindow() / 96;

    private DisplayArea DisplayArea => DisplayArea.GetFromWindowId(AppWindow.Id, DisplayAreaFallback.Nearest);
    private double PosX => DisplayArea.WorkArea.Width - DpiScale * (WindowWidth + WindowMargin);
    private double TopY => DisplayArea.WorkArea.Height - DpiScale * (WindowHeight + WindowMargin);
    private double BottomY => DisplayArea.OuterBounds.Height;

    public new event TypedEventHandler<object, bool>? VisibilityChanged;

    public FlyoutWindow()
    {
        InitializeComponent();

        App.GetService<IThemeSelectorService>().ThemeChanged += Flyout_ThemeChanged;
        Flyout_ThemeChanged(null, App.GetService<IThemeSelectorService>().ActualTheme);

        this.MoveAndResize(PosX, BottomY, WindowWidth, WindowHeight);

        _showTimer.Tick += (sender, e) =>
        {
            this.SetForegroundWindow();
            var fraction = (double)_currentStep / Steps;
            var y = BottomY - (BottomY - TopY) * EaseOutQuad(fraction);
            AppWindow.Move(new PointInt32((int)PosX, (int)y));

            if (_currentStep == Steps)
            {
                IsAlwaysOnTop = true;
                _currentStep = 0;
                _showTimer.Stop();
                return;
            }

            _currentStep++;
        };

        _hideTimer.Tick += (sender, e) =>
        {
            this.SetForegroundWindow();
            var fraction = (double)_currentStep / Steps;
            var y = TopY - (TopY - BottomY) * EaseInQuad(fraction);
            AppWindow.Move(new PointInt32((int)PosX, (int)y));

            if (_currentStep == Steps)
            {
                IsAlwaysOnTop = true;
                this.Hide();
                _currentStep = 0;
                _hideTimer.Stop();
                App.DispatcherQueue.EnqueueAsync(() => VisibilityChanged?.Invoke(this, false));
                return;
            }

            _currentStep++;
        };

        Closed += (sender, e) =>
        {
            Hide_Flyout();
            e.Handled = true;
        };
    }

    private void Flyout_ThemeChanged(object? sender, ElementTheme theme)
    {
        ((FrameworkElement)Content).RequestedTheme = theme;
    }

    public void Hide_Flyout()
    {
        if (_hideTimer.IsEnabled)
        {
            return;
        }

        if (_showTimer.IsEnabled)
        {
            _showTimer.Stop();
            _currentStep = Steps - _currentStep;
        }

        IsAlwaysOnTop = false;
        _hideTimer.Start();
    }

    public void Show_Flyout()
    {
        Activate();
        App.DispatcherQueue.EnqueueAsync(() => VisibilityChanged?.Invoke(this, true));
        if (_showTimer.IsEnabled)
        {
            return;
        }

        if (_hideTimer.IsEnabled)
        {
            _hideTimer.Stop();
            _currentStep = Steps - _currentStep;
        }

        IsAlwaysOnTop = false;
        _showTimer.Start();
    }


    public void Toggle_Flyout()
    {
        if (_showTimer.IsEnabled || ((this.GetWindowStyle() & WindowStyle.Visible) != 0 && !_hideTimer.IsEnabled))
        {
            Hide_Flyout();
        }
        else
        {
            Show_Flyout();
            App.GetService<FlyoutNavigationService>().NavigateTo(typeof(HomePage).FullName!);
        }
    }

    private static double EaseOutQuad(double t)
    {
        return 1 - Math.Pow(1 - t, 4);
    }

    private static double EaseInQuad(double t)
    {
        return Math.Pow(t, 4);
    }
}