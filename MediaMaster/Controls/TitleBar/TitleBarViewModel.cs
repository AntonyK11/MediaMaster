using System.Runtime.InteropServices;
using Windows.Foundation;
using Windows.Graphics;
using Windows.UI;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.WinUI;
using MediaMaster.Services;
using Microsoft.UI.Input;
using Microsoft.UI.Windowing;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Media;
using WinUIEx;
using Point = Windows.Foundation.Point;
using static MediaMaster.WIn32.WindowsApiService;
using static MediaMaster.WIn32.WindowsNativeValues;
using WindowsApiService = MediaMaster.WIn32.WindowsApiService;

namespace MediaMaster.Controls;

// https://github.com/microsoft/WindowsAppSDK/issues/2943
// https://github.com/ysc3839/win32-darkmode/blob/master/win32-darkmode/DarkMode.h
// https://gist.github.com/rounk-ctrl/b04e5622e30e0d62956870d5c22b7017
// https://learn.microsoft.com/en-us/windows/apps/develop/title-bar?tabs=wasdk#interactive-content
// https://stackoverflow.com/questions/21825352/how-to-open-window-system-menu-on-right-click
public sealed partial class TitleBarViewModel : ObservableObject
{
    private static readonly AppWindow? AppWindow = App.MainWindow?.AppWindow;

    private static readonly AppWindowTitleBar? TitleBar = AppWindow?.TitleBar;

    private readonly TitleBarControl _titleBar;

    [ObservableProperty] public partial bool IsFocused { get; set; }
    [ObservableProperty] public partial Thickness Margin { get; set; }

    public TitleBarViewModel(TitleBarControl titleBar)
    {
        if (App.MainWindow != null)
        {
            App.MainWindow.ExtendsContentIntoTitleBar = true;
            App.MainWindow.Activated += MainWindow_Activated;
        }

        _titleBar = titleBar;
    }

    private void MainWindow_Activated(object sender, WindowActivatedEventArgs args)
    {
        IsFocused = args.WindowActivationState != WindowActivationState.Deactivated;
    }

    public void UpdateTitleBar(ElementTheme actualTheme)
    {
        _ = FlushMenuThemes();
        _ = SetPreferredAppMode(actualTheme == ElementTheme.Dark
            ? PreferredAppMode.ForceDark
            : PreferredAppMode.ForceLight);

        var themeColor = _titleBar.Resources.ThemeDictionaries[actualTheme.ToString()] as ResourceDictionary;

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
    public void SetDragRegionTitleBar()
    {
        if (AppWindow == null || TitleBar == null) return;

        List<RectInt32> rects = [];
        var scale = _titleBar.XamlRoot.RasterizationScale;

        Button? navigationViewBackButton = App.MainWindow?.Content
            .FindDescendants()
            .OfType<Button>()
            .FirstOrDefault(x => x.Name is "NavigationViewBackButton");
        
        if (navigationViewBackButton != null)
        {
            rects.Add(GetRect(navigationViewBackButton, scale));
        }

        Button? togglePaneButton = App.MainWindow?.Content
            .FindDescendants()
            .OfType<Button>()
            .FirstOrDefault(x => x.Name is "TogglePaneButton");
        
        Grid? togglePaneButtonGrid = togglePaneButton?
            .FindDescendants()
            .OfType<Grid>()
            .FirstOrDefault(x => x.Name is "LayoutRoot");
        
        if (togglePaneButtonGrid != null)
        {
            rects.Add(GetRect(togglePaneButtonGrid, scale));
        }

        Image? titleBarIcon = _titleBar
            .FindDescendants()
            .OfType<Image>()
            .FirstOrDefault(x => x.Name is "AppIconElement");
        
        if (titleBarIcon != null)
        {
            rects.Add(GetRect(titleBarIcon, scale));
        }

        InputNonClientPointerSource? nonClientInputSrc = InputNonClientPointerSource.GetForWindowId(AppWindow.Id);
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
        Rect bounds =
            transformElement.TransformBounds(
                new Rect(0, 0, frameworkElement.ActualWidth, frameworkElement.ActualHeight));

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

        GeneralTransform ttv = ((UIElement)sender).TransformToVisual(App.MainWindow!.Content);
        Point screenCords = ttv.TransformPoint(new Point(0, 0));
        Point menuPos = new(
            AppWindow.Position.X + (screenCords.X - 8) * scaleAdjustment,
            AppWindow.Position.Y + _titleBar.ActualHeight * scaleAdjustment);
        
        ShowMenu(menuPos);
    }

    public void AppIcon_RightClick(object sender, RightTappedRoutedEventArgs args)
    {
        if (AppWindow == null) return;

        var scaleAdjustment = _titleBar.XamlRoot.RasterizationScale;

        Point pos = args.GetPosition(App.MainWindow!.Content);
        Point menuPos = new(
            AppWindow.Position.X + (pos.X + 9) * scaleAdjustment,
            AppWindow.Position.Y + (pos.Y + 2) * scaleAdjustment);
        
        ShowMenu(menuPos);
    }

    /// <summary>
    ///     Handles the click event on the application icon.
    ///     When the icon is clicked, the system menu is opened at the position of the mouse cursor
    /// </summary>
    /// <param name="pos"> The position of the mouse cursor. </param>
    private static void ShowMenu(Point pos)
    {
        if (App.MainWindow == null) return;

        var x = (int)pos.X;
        var y = (int)pos.Y;
        nint lParam = (y << 16 | x);

        Task.Run(() => SendMessage(App.MainWindow.GetWindowHandle(), WM_NCRBUTTONUP, 2, lParam));
    }
}