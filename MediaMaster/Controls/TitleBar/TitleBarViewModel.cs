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

    [ObservableProperty] private bool _isFocused;
    [ObservableProperty] private Thickness _margin;

    public TitleBarViewModel(TitleBarControl titleBar)
    {
        if (App.MainWindow != null)
        {
            App.MainWindow.ExtendsContentIntoTitleBar = true;
            App.MainWindow.Activated += MainWindow_Activated;

            var hWnd = App.MainWindow.GetWindowHandle();
            var hMenu = GetSystemMenu(hWnd, false);

            SetMenuItemInfo(hMenu, 0xF120);
            SetMenuItemInfo(hMenu, 0xF010);
            SetMenuItemInfo(hMenu, 0xF000);
            SetMenuItemInfo(hMenu, 0xF020);
            SetMenuItemInfo(hMenu, 0xF030);
            SetMenuItemInfo(hMenu, 0, true);
            SetMenuItemInfo(hMenu, 0xF060);

            App.GetService<TrayIconService>().UpdateMenuBackground(hMenu);
            App.GetService<TrayIconService>().hMenus.Add(hMenu);
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
            AppWindow.Position.X + screenCords.X * scaleAdjustment,
            AppWindow.Position.Y + _titleBar.ActualHeight * scaleAdjustment);
        
        ShowMenu(menuPos);
    }

    public void AppIcon_RightClick(object sender, RightTappedRoutedEventArgs args)
    {
        if (AppWindow == null) return;

        var scaleAdjustment = _titleBar.XamlRoot.RasterizationScale;

        Point pos = args.GetPosition(App.MainWindow!.Content);
        Point menuPos = new(
            AppWindow.Position.X + pos.X * scaleAdjustment,
            AppWindow.Position.Y + pos.Y * scaleAdjustment);
        
        ShowMenu(menuPos);
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
        var hMenu = GetSystemMenu(hWnd, false);

        SetMenuItemInfo(hMenu, 0xF120);
        SetMenuItemInfo(hMenu, 0xF010);
        SetMenuItemInfo(hMenu, 0xF000);
        SetMenuItemInfo(hMenu, 0xF020);
        SetMenuItemInfo(hMenu, 0xF030);
        SetMenuItemInfo(hMenu, 0, true);
        SetMenuItemInfo(hMenu, 0xF060);

        if (App.MainWindow.WindowState == WindowState.Normal)
        {
            _ = EnableMenuItem(hMenu, 0xF120, MENU_ITEM_FLAGS.MF_GRAYED); // Restore Disabled
            _ = EnableMenuItem(hMenu, 0xF010, MENU_ITEM_FLAGS.MF_ENABLED); // Move Enabled
            _ = EnableMenuItem(hMenu, 0xF000, MENU_ITEM_FLAGS.MF_ENABLED); // Size Enabled
            _ = EnableMenuItem(hMenu, 0xF020, MENU_ITEM_FLAGS.MF_ENABLED); // Minimize Enabled
            _ = EnableMenuItem(hMenu, 0xF030, MENU_ITEM_FLAGS.MF_ENABLED); // Maximize Enabled
            _ = EnableMenuItem(hMenu, 0xF060, MENU_ITEM_FLAGS.MF_ENABLED); // Close Enabled
        }
        else
        {
            _ = EnableMenuItem(hMenu, 0xF120, MENU_ITEM_FLAGS.MF_ENABLED); // Restore Enabled
            _ = EnableMenuItem(hMenu, 0xF010, MENU_ITEM_FLAGS.MF_GRAYED); // Move Disabled
            _ = EnableMenuItem(hMenu, 0xF000, MENU_ITEM_FLAGS.MF_GRAYED); // Size Disabled
            _ = EnableMenuItem(hMenu, 0xF020, MENU_ITEM_FLAGS.MF_ENABLED); // Minimize Enabled
            _ = EnableMenuItem(hMenu, 0xF030, MENU_ITEM_FLAGS.MF_GRAYED); // Maximize Disabled
            _ = EnableMenuItem(hMenu, 0xF060, MENU_ITEM_FLAGS.MF_ENABLED); // Close Enabled
        }

        var scaleAdjustment = App.MainWindow.Content.XamlRoot.RasterizationScale;

        _ = SendMessage(hWnd, WM_INITMENU, hMenu, IntPtr.Zero);
        var cmd = TrackPopupMenu(hMenu, TRACK_POPUP_MENU_FLAGS.TPM_RETURNCMD, (int)(pos.X + 9 * scaleAdjustment),
            (int)(pos.Y + 2 * scaleAdjustment), 0, hWnd, IntPtr.Zero);
        if (cmd > 0)
        {
            _ = SendMessage(hWnd, WM_SYSCOMMAND, cmd, IntPtr.Zero);
        }
    }

    private static void SetMenuItemInfo(IntPtr hMenu, uint item, bool separator = false)
    {
        if (separator)
        {
            TrayIconService.ODM_DATA odmd = new()
            {
                text = "",
                hBitmap = IntPtr.Zero,
                hasIcon = true
            };
            var pODMD = Marshal.AllocHGlobal(Marshal.SizeOf(odmd));
            Marshal.StructureToPtr(odmd, pODMD, false);
            ModifyMenu(hMenu, item,
                MENU_ITEM_FLAGS.MF_BYCOMMAND | MENU_ITEM_FLAGS.MF_OWNERDRAW | MENU_ITEM_FLAGS.MF_SEPARATOR, item,
                pODMD);
            return;
        }

        var mif = new MENUITEMINFOW
        {
            cbSize = (uint)Marshal.SizeOf<MENUITEMINFOW>(),
            fMask = MENU_ITEM_MASK.MIIM_TYPE,
            fType = MENU_ITEM_TYPE.MFT_BITMAP,
            dwTypeData = new IntPtr()
        };
        // First call to get the length of the string
        GetMenuItemInfo(hMenu, item, false, ref mif);
        mif.cch += 1;
        var ptr = Marshal.AllocHGlobal((int)mif.cch * sizeof(char));
        mif.dwTypeData = ptr;
        // Second call to get the actual string
        GetMenuItemInfo(hMenu, item, false, ref mif);

        if (mif.fType == MENU_ITEM_TYPE.MFT_STRING && !separator)
        {
            var text = Marshal.PtrToStringUni(mif.dwTypeData);

            if (text != null)
            {
                TrayIconService.ODM_DATA odmd = new()
                {
                    text = text,
                    hBitmap = mif.hbmpItem,
                    hasIcon = true
                };
                var pODMD = Marshal.AllocHGlobal(Marshal.SizeOf(odmd));
                Marshal.StructureToPtr(odmd, pODMD, false);

                ModifyMenu(hMenu, item, MENU_ITEM_FLAGS.MF_BYCOMMAND | MENU_ITEM_FLAGS.MF_OWNERDRAW, item, pODMD);
            }
        }
        else
        {
            var itemInfo1 = new MENUITEMINFOW
            {
                cbSize = (uint)Marshal.SizeOf<MENUITEMINFOW>(),
                fMask = MENU_ITEM_MASK.MIIM_TYPE,
                fType = MENU_ITEM_TYPE.MFT_OWNERDRAW
            };
            WindowsApiService.SetMenuItemInfo(hMenu, item, false, itemInfo1);
        }

        Marshal.FreeHGlobal(ptr);

        var itemInfo = new MENUITEMINFOW
        {
            cbSize = (uint)Marshal.SizeOf<MENUITEMINFOW>(),
            fMask = MENU_ITEM_MASK.MIIM_BITMAP,
            hbmpItem = IntPtr.Zero
        };
        WindowsApiService.SetMenuItemInfo(hMenu, item, false, itemInfo);
    }
}