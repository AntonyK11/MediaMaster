using Microsoft.UI.Xaml;
using MediaMaster.Interfaces.Services;
using MediaMaster.Services;
using WinUIEx;
using H.NotifyIcon.EfficiencyMode;
using static MediaMaster.Services.WindowsNativeValues;
using static MediaMaster.Services.WindowsApiService;
using System.Runtime.InteropServices;

namespace MediaMaster;

public sealed partial class MainWindow
{
    public MainWindow()
    {
        InitializeComponent();

        App.GetService<IThemeSelectorService>().ThemeChanged += MainWindow_ThemeChanged;

        ExtendsContentIntoTitleBar = true;

        Closed += MainWindow_Closed;

        InitializeTheme();
    }

    private void MainWindow_Closed(object sender, WindowEventArgs args)
    {
        if (App.GetService<SettingsService>().LeaveAppRunning)
        {
            args.Handled = true;

            this.Minimize();
            this.Hide();
            EfficiencyModeUtilities.SetEfficiencyMode(true);
        }
        else
        {
            App.Shutdown();
        }
    }

    public void Show()
    {
        //// TODO: Sends the WM_NCHITTEST message to restore the close button hover state to normal because it gets stuck in the hover state when the window is hidden.
        //WindowsApiService.SendMessage(App.MainWindow.GetWindowHandle(), WindowsNativeValues.WM_NCHITTEST, 0, 0);

        EfficiencyModeUtilities.SetEfficiencyMode(false);

        this.Restore();
        this.SetForegroundWindow();
        BringToFront();
    }

    public void InitializeTheme()
    {
        MainWindow_ThemeChanged(null, App.GetService<IThemeSelectorService>().ActualTheme);
    }

    private void MainWindow_ThemeChanged(object? sender, ElementTheme theme)
    {
        ((FrameworkElement)Content).RequestedTheme = theme;

        if (Environment.OSVersion.Version.Build < 22000) return;

        // TODO: Fixes the mica effect not resizing fast enough by adding it to the non-client area of the window. Does not fix the client area not resizing correctly.
        // https://github.com/zhiyiYo/PyQt-Frameless-Window/blob/master/qframelesswindow/windows/window_effect.py#L85-L120
        // https://stackoverflow.com/questions/53000291/how-to-smooth-ugly-jitter-flicker-jumping-when-resizing-windows-especially-drag
        // https://github.com/microsoft/microsoft-ui-xaml/issues/5148
        var hWnd = this.GetWindowHandle();
        MARGINS margins = new() { cxLeftWidth = -1, cxRightWidth = -1, cyTopHeight = -1, cyBottomHeight = -1 };
        _ = DwmExtendFrameIntoClientArea(hWnd, ref margins);

        WINDOWCOMPOSITIONATTRIBDATA winCompAttrData = new()
        {
            Attribute = WINDOWCOMPOSITIONATTRIB.WCA_ACCENT_POLICY,
            Data = Marshal.UnsafeAddrOfPinnedArrayElement([(int)ACCENT_STATE.ACCENT_ENABLE_HOSTBACKDROP], 0),
            SizeOfData = sizeof(int)
        };

        if (theme == ElementTheme.Dark)
        {
            winCompAttrData.Attribute = WINDOWCOMPOSITIONATTRIB.WCA_USEDARKMODECOLORS;
        }
        _ = SetWindowCompositionAttribute(hWnd, ref winCompAttrData);

        var attribute = (Environment.OSVersion.Version.Build < 22523) ? DWMWINDOWATTRIBUTE.DWMWA_SYSTEMBACKDROP_TYPE_DEPRECATED : DWMWINDOWATTRIBUTE.DWMWA_SYSTEMBACKDROP_TYPE;
        var attributeValue = (Environment.OSVersion.Version.Build < 22523) ? 1 : (int)DWM_SYSTEMBACKDROP_TYPE.DWMSBT_MAINWINDOW;
        _ = DwmSetWindowAttribute(hWnd, attribute, ref attributeValue, sizeof(int));

        attributeValue = theme == ElementTheme.Dark ? 1 : 0;
        _ = DwmSetWindowAttribute(hWnd, DWMWINDOWATTRIBUTE.DWMWA_USE_IMMERSIVE_DARK_MODE, ref attributeValue, sizeof(int));
    }
}