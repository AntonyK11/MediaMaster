using MediaMaster.Interfaces.Services;
using MediaMaster.Services;
using WinUIEx;
using H.NotifyIcon.EfficiencyMode;
using MediaMaster.Services.Navigation;
using MediaMaster.Views;
using static MediaMaster.WIn32.WindowsNativeValues;
using static MediaMaster.WIn32.WindowsApiService;
//using Microsoft.UI.Windowing;
//using DisplayAreaFallback = Microsoft.UI.Windowing.DisplayAreaFallback;

namespace MediaMaster;


public sealed partial class MainWindow : WindowEx
{
    public MainWindow()
    {
        this.InitializeComponent();
        ExtendsContentIntoTitleBar = true;
        App.GetService<IThemeSelectorService>().ThemeChanged += MainWindow_ThemeChanged;

        ExtendsContentIntoTitleBar = true;

        Closed += MainWindow_Closed;
        MinHeight = 48 + 9;

        InitializeTheme();

        //DisplayArea displayArea = DisplayArea.GetFromWindowId(AppWindow.Id, DisplayAreaFallback.Nearest);
        //this.Move((int)((displayArea.WorkArea.Width - Width) / 5), (int)((displayArea.WorkArea.Height - Height) / 2));
    }

    private void MainWindow_Closed(object sender, WindowEventArgs args)
    {
        if (App.GetService<SettingsService>().LeaveAppRunning)
        {
            args.Handled = true;

            this.Minimize();
            this.Hide();
            App.GetService<MainNavigationService>().NavigateTo(typeof(HomePage).FullName!);
            App.GetService<IActivationService>().ShowAppRunningInBackgroundPopup();

            if (App.Flyout == null || !App.Flyout.IsOpen)
            {
                EfficiencyModeUtilities.SetEfficiencyMode(true);
            }
        }
        else if (App.Flyout == null || !App.Flyout.IsOpen)
        {
            App.Shutdown();
        }
        else
        {
            args.Handled = true;
            this.Minimize();
            this.Hide();
            App.GetService<MainNavigationService>().NavigateTo(typeof(HomePage).FullName!);
        }
    }

    public void Show()
    {
        EfficiencyModeUtilities.SetEfficiencyMode(false);

        if (!Visible)
        {
            this.Restore();
        }

        this.SetForegroundWindow();
    }

    public void InitializeTheme()
    {
        MainWindow_ThemeChanged(null, App.GetService<IThemeSelectorService>().ActualTheme);
    }

    private void MainWindow_ThemeChanged(object? sender, ElementTheme theme)
    {
        ((FrameworkElement)Content).RequestedTheme = theme;

        if (Environment.OSVersion.Version.Build < 22000) return;

        // Fixes the mica effect not resizing fast enough by adding it to the non-client area of the window. Does not fix the client area not resizing correctly.
        // https://github.com/zhiyiYo/PyQt-Frameless-Window/blob/master/qframelesswindow/windows/window_effect.py#L85-L120
        // https://stackoverflow.com/questions/53000291/how-to-smooth-ugly-jitter-flicker-jumping-when-resizing-windows-especially-drag
        // https://github.com/microsoft/microsoft-ui-xaml/issues/5148
        var hWnd = this.GetWindowHandle();

        Margins margins = new() { cxLeftWidth = -1, cxRightWidth = -1, cyTopHeight = -1, cyBottomHeight = -1 };
        DwmExtendFrameIntoClientArea(hWnd, ref margins);

        DwmWindowAttribute attribute = (Environment.OSVersion.Version.Build < 22523)
            ? DwmWindowAttribute.SystemBackdropTypeDeprecated // Undocumented Backdrop attribute
            : DwmWindowAttribute.DwmWindowAttribute;
        var attributeValue = Environment.OSVersion.Version.Build < 22523 ? 1 : (int)DwSystemBackdropType.MainWindow;
        DwmSetWindowAttribute(hWnd, attribute, ref attributeValue, sizeof(int));

        attributeValue = theme == ElementTheme.Dark ? 1 : 0;
        DwmSetWindowAttribute(hWnd, DwmWindowAttribute.UseImmersiveDarkMode, ref attributeValue, sizeof(int));
    }
}