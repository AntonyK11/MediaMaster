using Windows.Foundation;
using Windows.Graphics;
using Windows.UI;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.WinUI;
using Microsoft.UI.Input;
using Microsoft.UI.Windowing;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Media;
using WinUIEx;
using Microsoft.UI.Xaml.Controls;
using MediaMaster.Services;

namespace MediaMaster.Controls.VIewModels;

// https://github.com/microsoft/WindowsAppSDK/issues/2943
// https://github.com/ysc3839/win32-darkmode/blob/master/win32-darkmode/DarkMode.h
// https://gist.github.com/rounk-ctrl/b04e5622e30e0d62956870d5c22b7017
// https://learn.microsoft.com/en-us/windows/apps/develop/title-bar?tabs=wasdk#interactive-content
// https://stackoverflow.com/questions/21825352/how-to-open-window-system-menu-on-right-click
/// <summary>
///     Service that handles the title bar of the application.
/// </summary>
public partial class TitleBarViewModel : ObservableObject
{
    [ObservableProperty] public Thickness _margin = new();

    private static readonly AppWindow AppWindow = App.MainWindow!.AppWindow;

    private static readonly AppWindowTitleBar TitleBar = AppWindow.TitleBar;

    public TitleBarViewModel()
    {
        if (App.MainWindow != null)
        {
            App.MainWindow.ExtendsContentIntoTitleBar = true;
        }
    }

    public static void UpdateTitleBar(ElementTheme actualTheme)
    {
        _ = WindowsApiService.FlushMenuThemes();
        _ = WindowsApiService.SetPreferredAppMode(actualTheme == ElementTheme.Dark
            ? WindowsNativeValues.PreferredAppMode.ForceDark
            : WindowsNativeValues.PreferredAppMode.ForceLight);

        ResourceDictionary? themeColor =
            Application.Current.Resources.ThemeDictionaries[actualTheme.ToString()] as ResourceDictionary;

        if (themeColor == null) return;

        TitleBar.ButtonForegroundColor = (Color)themeColor["ButtonForegroundColor"];
        TitleBar.ButtonHoverBackgroundColor = (Color)themeColor["ButtonHoverBackgroundColor"];
        TitleBar.ButtonHoverForegroundColor = (Color)themeColor["ButtonHoverForegroundColor"];
        TitleBar.ButtonInactiveBackgroundColor = (Color)themeColor["ButtonInactiveBackgroundColor"];
        TitleBar.ButtonInactiveForegroundColor = (Color)themeColor["ButtonInactiveForegroundColor"];
        TitleBar.ButtonPressedBackgroundColor = (Color)themeColor["ButtonPressedBackgroundColor"];
        TitleBar.ButtonPressedForegroundColor = (Color)themeColor["ButtonPressedForegroundColor"];
    }

    public void SetLeftMargin(double margin)
    {
        Margin = new Thickness(margin, Margin.Top, Margin.Right, Margin.Bottom);
    }

    public void SetTopMargin(double margin)
    {
        Margin = new Thickness(Margin.Left, margin, Margin.Right, Margin.Bottom);
    }

    public void SetRightMargin(double margin)
    {
        Margin = new Thickness(Margin.Left, Margin.Top, margin, Margin.Bottom);
    }

    public void SetBottomMargin(double margin)
    {
        Margin = new Thickness(Margin.Left, Margin.Top, Margin.Right, margin);
    }

    
    /// <summary>
    ///     Sets the drag region for a custom title bar.
    /// </summary>
    /// <param name="appTitleBar"> The <see cref="TitleBarControl" /> that contains the application icon. </param>
    public static void SetDragRegionTitleBar(TitleBarControl appTitleBar)
    {
        var nonClientInputSrc = InputNonClientPointerSource.GetForWindowId(AppWindow.Id);
        List<RectInt32> rects = [];
        var scale = appTitleBar.XamlRoot.RasterizationScale;

        RectInt32 titleBarRect = new()
        {
            X = 0,
            Y = 0,
            Width = (int)(appTitleBar.ActualWidth * scale),
            Height = (int)(appTitleBar.ActualHeight * scale)
        };

        TitleBar.SetDragRectangles([titleBarRect]);
        var navigationViewBackButton = App.MainWindow?.Content.FindDescendants().OfType<Button>().FirstOrDefault(x => x.Name is "NavigationViewBackButton");
        if (navigationViewBackButton != null)
        {
            rects.Add(GetRect(navigationViewBackButton, scale));
        }

        var togglePaneButton = App.MainWindow?.Content.FindDescendants().OfType<Button>().FirstOrDefault(x => x.Name is "TogglePaneButton");
        var togglePaneButtonGrid = togglePaneButton?.FindDescendants().OfType<Grid>().FirstOrDefault(x => x.Name is "LayoutRoot");
        if (togglePaneButtonGrid != null)
        {
            rects.Add(GetRect(togglePaneButtonGrid, scale));
        }

        if (appTitleBar.Icon != null)
        {
            rects.Add(GetRect(appTitleBar.Icon, scale));
        }

        rects.Add(GetRect(appTitleBar.SearchBox, scale));

        nonClientInputSrc.SetRegionRects(NonClientRegionKind.Passthrough, [.. rects]);
    }

    /// <summary>
    ///     Gets a <see cref="RectInt32" /> from a <see cref="Rect" /> and a scale.
    /// </summary>
    /// <param name="frameworkElement"> The <see cref="FrameworkElement" /> to get the <see cref="RectInt32" /> from. </param>
    /// <param name="scale"> The scale to use. </param>
    /// <returns> The <see cref="RectInt32" /> that was created. </returns>
    private static RectInt32 GetRect(FrameworkElement frameworkElement, double scale)
    {
        GeneralTransform transformElement = frameworkElement.TransformToVisual(null);
        Rect bounds = transformElement.TransformBounds(new Rect(0, 0, frameworkElement.ActualWidth, frameworkElement.ActualHeight));

        return new RectInt32(
            (int)Math.Round(bounds.X * scale),
            (int)Math.Round(bounds.Y * scale),
            (int)Math.Round(bounds.Width * scale),
            (int)Math.Round(bounds.Height * scale)
        );
    }

    public static void AppIcon_LeftClick(PointerRoutedEventArgs args, TitleBarControl appTitleBar)
    {
        if (appTitleBar.Icon == null) return;

        if (args.Pointer.PointerDeviceType == PointerDeviceType.Mouse)
        {
            PointerPointProperties? properties = args.GetCurrentPoint(appTitleBar.Icon).Properties;
            if (properties.IsRightButtonPressed) return;
        }

        var scaleAdjustment = appTitleBar.XamlRoot.RasterizationScale;

        GeneralTransform ttv = appTitleBar.Icon.TransformToVisual(App.MainWindow!.Content);
        Point screenCords = ttv.TransformPoint(new Point(0, 0));
        Point menuPos = new(AppWindow.Position.X + screenCords.X * scaleAdjustment,
            AppWindow.Position.Y + appTitleBar.ActualHeight * scaleAdjustment);
        AppIcon_Click(menuPos);
    }

    public static void AppIcon_RightClick()
    {
        _ = WindowsApiService.GetCursorPos(out WindowsNativeValues.POINT pt);
        AppIcon_Click(new Point(pt.x, pt.y));
    }

    /// <summary>
    ///     Handles the click event on the application icon.
    ///     When the icon is clicked, the system menu is opened at the position of the mouse cursor with the appropriate items
    ///     enabled/disabled.
    /// </summary>
    /// <param name="pos"> The position of the mouse cursor. </param>
    private static void AppIcon_Click(Point pos)
    {
        var hWnd = App.MainWindow!.GetWindowHandle();
        var hMenu = WindowsApiService.GetSystemMenu(hWnd, false);

        if (App.MainWindow!.WindowState == WindowState.Normal)
        {
            _ = WindowsApiService.EnableMenuItem(hMenu, 0xF120, 0x0001); // Restore Disabled
            _ = WindowsApiService.EnableMenuItem(hMenu, 0xF010, 0x0000); // Move Enabled
            _ = WindowsApiService.EnableMenuItem(hMenu, 0xF000, 0x0000); // Size Enabled
            _ = WindowsApiService.EnableMenuItem(hMenu, 0xF020, 0x0000); // Minimize Enabled
            _ = WindowsApiService.EnableMenuItem(hMenu, 0xF030, 0x0000); // Maximize Enabled
            _ = WindowsApiService.EnableMenuItem(hMenu, 0xF060, 0x0000); // Close Enabled
        }
        else
        {
            _ = WindowsApiService.EnableMenuItem(hMenu, 0xF120, 0x0000); // Restore Enabled
            _ = WindowsApiService.EnableMenuItem(hMenu, 0xF010, 0x0001); // Move Disabled
            _ = WindowsApiService.EnableMenuItem(hMenu, 0xF000, 0x0001); // Size Disabled
            _ = WindowsApiService.EnableMenuItem(hMenu, 0xF020, 0x0000); // Minimize Enabled
            _ = WindowsApiService.EnableMenuItem(hMenu, 0xF030, 0x0001); // Maximize Disabled
            _ = WindowsApiService.EnableMenuItem(hMenu, 0xF060, 0x0000); // Close Enabled
        }

        _ = WindowsApiService.SendMessage(hWnd, WindowsNativeValues.WM_INITMENU, hMenu, IntPtr.Zero);
        var cmd = WindowsApiService.TrackPopupMenu(hMenu, WindowsNativeValues.TPM_RETURNCMD, (int)pos.X, (int)pos.Y, 0, hWnd, IntPtr.Zero);
        if (cmd > 0)
        {
            _ = WindowsApiService.SendMessage(hWnd, WindowsNativeValues.WM_SYSCOMMAND, cmd, IntPtr.Zero);
        }
    }

    public static void AppIcon_DoubleClick()
    {
        App.Shutdown();
    }
}