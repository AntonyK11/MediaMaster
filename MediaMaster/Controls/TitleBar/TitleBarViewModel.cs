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

namespace MediaMaster.Controls;

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

    [ObservableProperty] public bool _isFocused;

    private static readonly AppWindow? AppWindow = App.MainWindow?.AppWindow;

    private static readonly AppWindowTitleBar? TitleBar = AppWindow?.TitleBar;

    private readonly TitleBarControl _titleBar;

    public TitleBarViewModel(TitleBarControl titleBar)
    {
        if (App.MainWindow != null)
        {
            App.MainWindow.ExtendsContentIntoTitleBar = true;
            App.MainWindow.Activated += MainWindow_Activated;
        }

        _titleBar = titleBar;
    }
    public void MainWindow_Activated(object sender, WindowActivatedEventArgs args)
    {
        IsFocused = args.WindowActivationState != WindowActivationState.Deactivated;
    }

    public void UpdateTitleBar(ElementTheme actualTheme)
    {
        _ = WindowsApiService.FlushMenuThemes();
        _ = WindowsApiService.SetPreferredAppMode(actualTheme == ElementTheme.Dark
            ? WindowsNativeValues.PreferredAppMode.ForceDark
            : WindowsNativeValues.PreferredAppMode.ForceLight);

        ResourceDictionary? themeColor = _titleBar.Resources.ThemeDictionaries[actualTheme.ToString()] as ResourceDictionary;

        if (themeColor == null || TitleBar == null) return;

        TitleBar.ButtonBackgroundColor = (Color)themeColor["TitleBarButtonBackgroundColor"];
        TitleBar.ButtonForegroundColor = (Color)themeColor["TitleBarButtonForegroundColor"];
        TitleBar.ButtonHoverBackgroundColor = (Color)themeColor["TitleBarButtonHoverBackgroundColor"];
        TitleBar.ButtonHoverForegroundColor = (Color)themeColor["TitleBarButtonHoverForegroundColor"];
        TitleBar.ButtonInactiveBackgroundColor = (Color)themeColor["TitleBarButtonInactiveBackgroundColor"];
        TitleBar.ButtonInactiveForegroundColor = (Color)themeColor["TitleBarButtonInactiveForegroundColor"];
        TitleBar.ButtonPressedBackgroundColor = (Color)themeColor["TitleBarButtonPressedBackgroundColor"];
        TitleBar.ButtonPressedForegroundColor = (Color)themeColor["TitleBarButtonPressedForegroundColor"];
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
    public void SetDragRegionTitleBar()
    {
        if (AppWindow == null || TitleBar == null) return;

        List<RectInt32> rects = [];
        var scale = _titleBar.XamlRoot.RasterizationScale;

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

        var titleBarIcon = _titleBar.FindDescendants().OfType<Viewbox>().FirstOrDefault(x => x.Name is "AppIconElement");
        if (titleBarIcon != null)
        {
            rects.Add(GetRect(titleBarIcon, scale));
        }

        var nonClientInputSrc = InputNonClientPointerSource.GetForWindowId(AppWindow.Id);
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

    public void AppIcon_LeftClick(object sender, PointerRoutedEventArgs args)
    {
        if (AppWindow == null) return;

        if (args.Pointer.PointerDeviceType == PointerDeviceType.Mouse)
        {
            PointerPointProperties? properties = args.GetCurrentPoint((UIElement)sender).Properties;
            if (properties.IsRightButtonPressed) return;
        }

        var scaleAdjustment = _titleBar.XamlRoot.RasterizationScale;

        GeneralTransform ttv = ((UIElement) sender).TransformToVisual(App.MainWindow!.Content);
        Point screenCords = ttv.TransformPoint(new Point(0, 0));
        Point menuPos = new(AppWindow.Position.X + screenCords.X * scaleAdjustment,
            AppWindow.Position.Y + _titleBar.ActualHeight * scaleAdjustment);
        ShowMenu(menuPos);
    }

    public static void AppIcon_RightClick(object sender, RightTappedRoutedEventArgs args)
    {
        var pos = args.GetPosition((UIElement)sender);
        ShowMenu(new Point(pos.X, pos.X));
    }

    /// <summary>
    ///     Handles the click event on the application icon.
    ///     When the icon is clicked, the system menu is opened at the position of the mouse cursor with the appropriate items
    ///     enabled/disabled.
    /// </summary>
    /// <param name="pos"> The position of the mouse cursor. </param>
    private static void ShowMenu(Point pos)
    {
        if (App.MainWindow == null) return;

        var hWnd = App.MainWindow.GetWindowHandle();
        var hMenu = WindowsApiService.GetSystemMenu(hWnd, false);

        if (App.MainWindow.WindowState == WindowState.Normal)
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
}