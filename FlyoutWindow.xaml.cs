using System.Diagnostics;
using System.Numerics;
using Windows.Graphics;
using MediaMaster.Interfaces.Services;
using Microsoft.UI.Windowing;
using Microsoft.UI.Xaml;
using WinUIEx;
using MediaMaster.Services.Navigation;
using MediaMaster.Views.Flyout;
using Windows.Foundation;
using CommunityToolkit.WinUI;
using Windows.UI.WindowManagement;
using static MediaMaster.Services.WindowsNativeValues;
using System.Runtime.InteropServices;
using MediaMaster.Services;
using Microsoft.UI;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media.Animation;
using Microsoft.UI.Xaml.Hosting;
using System.Windows.Forms;

namespace MediaMaster;

/// <summary>
///     An empty window that can be used on its own or navigated to within a Frame.
/// </summary>
public sealed partial class FlyoutWindow
{
    private DisplayArea DisplayArea => DisplayArea.GetFromWindowId(AppWindow.Id, DisplayAreaFallback.Nearest);
    private double DpiScale => (float)this.GetDpiForWindow() / 96;

    private const int WindowMargin = 12;
    private const int WindowWidth = 386;
    private const double WindowHeight = 486;

    private double WindowFrameHeight => WindowHeight + DisplayArea.OuterBounds.Height - DisplayArea.WorkArea.Height + WindowMargin * DpiScale;

    private double PosX => DisplayArea.WorkArea.Width - DpiScale * (WindowWidth + WindowMargin);

    private double PosY => DisplayArea.WorkArea.Height - DpiScale * (WindowHeight + WindowMargin);


    //private const int Steps = 26;
    //private int _currentStep;

    private readonly bool _windows10 = Environment.OSVersion.Version.Build < 22000;

    //private readonly DispatcherTimer _hideTimer = new() { Interval = TimeSpan.FromMilliseconds(1) };
    //private readonly DispatcherTimer _showTimer = new() {Interval = TimeSpan.FromMilliseconds(1) };

    //private double TopY => DisplayArea.WorkArea.Height - DpiScale * (WindowHeight + WindowMargin);
    //private double BottomY => DisplayArea.OuterBounds.Height;

    public new event TypedEventHandler<object, bool>? VisibilityChanged;

    public FlyoutWindow()
    {
        InitializeComponent();

        App.GetService<IThemeSelectorService>().ThemeChanged += Flyout_ThemeChanged;
        Flyout_ThemeChanged(null, App.GetService<IThemeSelectorService>().ActualTheme);

        this.MoveAndResize(PosX, PosY, WindowWidth, WindowFrameHeight);

        //_showTimer.Tick += (sender, e) =>
        //{
        //    this.SetForegroundWindow();
        //    var fraction = (double)_currentStep / Steps;
        //    var y = BottomY - (BottomY - TopY) * EaseOutQuad(fraction);
        //    AppWindow.Move(new PointInt32((int)PosX, (int)y));

        //    if (_currentStep == Steps)
        //    {
        //        IsAlwaysOnTop = true;
        //        _currentStep = 0;
        //        _showTimer.Stop();
        //        return;
        //    }

        //    _currentStep++;
        //};

        //_hideTimer.Tick += (sender, e) =>
        //{
        //    this.SetForegroundWindow();
        //    var fraction = (double)_currentStep / Steps;
        //    var y = TopY - (TopY - BottomY) * EaseInQuad(fraction);
        //    AppWindow.Move(new PointInt32((int)PosX, (int)y));

        //    if (_currentStep == Steps)
        //    {
        //        IsAlwaysOnTop = true;
        //        this.Hide();
        //        _currentStep = 0;
        //        _hideTimer.Stop();
        //        App.DispatcherQueue.EnqueueAsync(() => VisibilityChanged?.Invoke(this, false));
        //        return;
        //    }

        //    _currentStep++;
        //};

        Closed += (sender, e) =>
        {
            Hide_Flyout();
            e.Handled = true;
        };

        int nValue = (int)DWM_WINDOW_CORNER_PREFERENCE.DWMWCP_DEFAULT;
        WindowsApiService.DwmSetWindowAttribute(this.GetWindowHandle(), DWMWINDOWATTRIBUTE.DWMWA_WINDOW_CORNER_PREFERENCE, ref nValue, Marshal.SizeOf(typeof(int)));
    }

    private void Flyout_ThemeChanged(object? sender, ElementTheme theme)
    {
        ((FrameworkElement)Content).RequestedTheme = theme;
    }

    public void Hide_Flyout()
    {
        //if (_hideTimer.IsEnabled)
        //{
        //    return;
        //}

        //if (_showTimer.IsEnabled)
        //{
        //    _showTimer.Stop();
        //    _currentStep = Steps - _currentStep;
        //}

        //IsAlwaysOnTop = false;
        //_hideTimer.Start();
    }

    public void Show_Flyout()
    {
        //Activate();
        //App.DispatcherQueue.EnqueueAsync(() => VisibilityChanged?.Invoke(this, true));
        //if (_showTimer.IsEnabled)
        //{
        //    return;
        //}

        //if (_hideTimer.IsEnabled)
        //{
        //    _hideTimer.Stop();
        //    _currentStep = Steps - _currentStep;
        //}

        //IsAlwaysOnTop = false;
        //_showTimer.Start();
    }

    private bool? IsVisible = null;

    public void Toggle_Flyout()
    {
        //if (_showTimer.IsEnabled || ((this.GetWindowStyle() & WindowStyle.Visible) != 0 && !_hideTimer.IsEnabled))
        //{
        if (IsVisible is true)
        {
            IsVisible = false;
            this.SetForegroundWindow();
            //var y = PosY;
            //AppWindow.Move(new PointInt32((int)PosX, (int)y));

            //var compositor = Compositor;
            //var animation = compositor.CreateScalarKeyFrameAnimation();

            ////animation.InsertKeyFrame(0.0f, 486.0f);

            //var easing = Compositor.CreateCubicBezierEasingFunction(new Vector2(1f, 0), new Vector2(1f, 1f));
            ////animation.SetScalarParameter("ease", 0.5f);
            ////animation.SetScalarParameter("damp", 0.5f);

            //animation.InsertKeyFrame(1.0f, (float)WindowFrameHeight, easing);
            //animation.Duration = TimeSpan.FromMilliseconds(167);
            //animation.Target = "Translation.Y";

            //ContentGrid.StartAnimation(animation);

            //this.Hide();
            //Hide_Flyout();
            //VisualStateManager.GoToState(ContentGrid, "Closed1", true);
            //this.Hide();
            //var storyBoard = new Storyboard();
            //var animation = new PopOutThemeAnimation();
            //animation.TargetName = "ContentGrid";
            //animation.Duration = TimeSpan.FromSeconds(0.5);
            //storyBoard.Children.Add(animation);
            //storyBoard.Begin();
            App.DispatcherQueue.EnqueueAsync(() => VisibilityChanged?.Invoke(this, false));
        }
        else
        {
            Flyout.ShowAt(ContentGrid);
            Activate();
            this.SetForegroundWindow();
            IsAlwaysOnTop = false;

            //var animation = Compositor.CreateVector3KeyFrameAnimation();

            //animation.InsertKeyFrame(1.0f, new Vector3(0.0f, 486.0f, 0.0f));
            //animation.Duration = TimeSpan.FromSeconds(1);
            //animation.Target = "Translation";

            //var easing = Compositor.CreateCubicBezierEasingFunction(new Vector2(0.42f, 0), new Vector2(1f, 1f));
            //animation.SetScalarParameter("ease", 0.5f);
            //animation.SetScalarParameter("damp", 0.5f);
            //animation.Duration = TimeSpan.FromMilliseconds(500);

            //// Apply easing to the keyframe
            //animation.SetKeyFrameParameters(1.0f, easing);

            //var _rootVisual = Compositor.CreateSpriteVisual();
            //_rootVisual.Size = new Vector2(WindowWidth, WindowHeight);
            //_rootVisual.Brush = Compositor.CreateColorBrush(Colors.Transparent);

            //ElementCompositionPreview.SetElementChildVisual(ContentGrid, _rootVisual);

            //// Create animation
            //var animation = Compositor.CreateScalarKeyFrameAnimation();
            //animation.InsertKeyFrame(0.0f, WindowHeight);

            //// Add easing function
            //var easing = Compositor.CreateCubicBezierEasingFunction(new Vector2(0.42f, 0), new Vector2(1f, 1f));
            //animation.SetScalarParameter("ease", 0.5f);
            //animation.SetScalarParameter("damp", 0.5f);
            //animation.Duration = TimeSpan.FromMilliseconds(50);

            //animation.InsertKeyFrame(1.0f, 0, easing);

            //_rootVisual.StartAnimation("Offset.Y", animation);



            //var animation = Compositor.CreateScalarKeyFrameAnimation();

            //var easing = Compositor.CreateCubicBezierEasingFunction(new Vector2(0, 0), new Vector2(0, 1f));
            ////animation.SetScalarParameter("ease", 0.5f);
            ////animation.SetScalarParameter("damp", 0.5f);

            //if (IsVisible is null)
            //{
            //    animation.InsertKeyFrame(0.0f, (float)WindowFrameHeight, easing);
            //}
            //animation.InsertKeyFrame(1.0f, 0, easing);
            //animation.Duration = TimeSpan.FromMilliseconds(167);
            //animation.Target = "TopProperty";


            //ContentGrid.StartAnimation(animation);



            var duration = new Duration(TimeSpan.FromSeconds(1));
            var ease = new CubicEase { EasingMode = EasingMode.EaseOut };
            var animation = new DoubleAnimation
            {
                EasingFunction = ease,
                Duration = duration
            };

            var conStoryboard = new Storyboard();
            conStoryboard.Children.Add(animation);
            animation.From = PosY + WindowFrameHeight;
            animation.To = PosY;
            animation.EnableDependentAnimation = true;
            Storyboard.SetTarget(animation, ContentGrid);
            Storyboard.SetTargetProperty(animation, "PosY");
            conStoryboard.Begin();



            //Show_Flyout();
            //VisualStateManager.GoToState(ContentGrid, "Opened1", true);

            //var storyBoard = new Storyboard();
            //var animation = new PopInThemeAnimation();
            //animation.FromHorizontalOffset = PosX;
            //animation.FromVerticalOffset = TopY;
            //animation.TargetName = "ContentGrid";
            //animation.Duration = TimeSpan.FromSeconds(0.5);
            //storyBoard.Children.Add(animation);
            //storyBoard.Begin();

            App.GetService<FlyoutNavigationService>().NavigateTo(typeof(HomePage).FullName!);
            IsVisible = true;
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

class ContentFrame : Frame
{

    private DisplayArea DisplayArea => DisplayArea.GetFromWindowId(App.Flyout!.AppWindow.Id, DisplayAreaFallback.Nearest);
    private double DpiScale => (float)App.Flyout!.GetDpiForWindow() / 96;

    private const int WindowMargin = 12;
    private const int WindowWidth = 386;
    private double PosX => DisplayArea.WorkArea.Width - DpiScale * (WindowWidth + WindowMargin);

    public ContentFrame()
    {
        RegisterPropertyChangedCallback(PosYProperty, OnPropertyChanged);
    }

    private void OnPropertyChanged(DependencyObject sender, DependencyProperty dp)
    {
        App.Flyout?.Move((int)PosX, (int)PosY);
    }

    public static readonly DependencyProperty PosYProperty
        = DependencyProperty.Register(
            nameof(PosY),
            typeof(double),
            typeof(ContentFrame),
            new PropertyMetadata(0));

    public double PosY
    {
        get => (double)GetValue(PosYProperty);
        set => SetValue(PosYProperty, value);
    }
}