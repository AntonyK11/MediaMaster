using System.Runtime.InteropServices;
using static MediaMaster.Services.WindowsNativeValues;

namespace MediaMaster.Services;

/// <summary>
///     Provides access to Windows API functions.
/// </summary>
public static partial class WindowsApiService
{
    /// <summary>
    ///     Flushes the menu themes applied to the current application.
    /// </summary>
    [LibraryImport("uxtheme.dll", EntryPoint = "#136")]
    internal static partial int FlushMenuThemes();

    [LibraryImport("uxtheme.dll", EntryPoint = "#135")]
    internal static partial int SetPreferredAppMode(PreferredAppMode preferredAppMode);

    /// <summary>
    ///     Sends the specified message to a window or windows.
    /// </summary>
    /// <param name="hWnd"> A handle to the window whose window procedure will receive the message. </param>
    /// <param name="msg"> The message to be sent. </param>
    /// <param name="wp"> Additional message-specific information. </param>
    /// <param name="lp"> Additional message-specific information. </param>
    [LibraryImport("user32.dll", EntryPoint = "SendMessageA")]
    internal static partial IntPtr SendMessage(IntPtr hWnd, int msg, IntPtr wp, IntPtr lp);

    /// <summary>
    ///     Retrieves a handle to the system menu (also known as the window menu) of the specified window.
    /// </summary>
    /// <param name="hWnd"> The handle to the window whose system menu is to be retrieved. </param>
    /// <param name="bRevert"> Specifies whether the window menu should be restored to its default state. </param>
    /// <returns>
    ///     If bRevert is false, the return value is a handle to the window menu of the specified window.
    ///     If bRevert is true, the return value is NULL.
    /// </returns>
    [LibraryImport("user32.dll")]
    internal static partial IntPtr GetSystemMenu(IntPtr hWnd, [MarshalAs(UnmanagedType.Bool)] bool bRevert);

    /// <summary>
    ///     Displays a shortcut menu at the specified location and tracks the selection of items
    /// </summary>
    /// <param name="hMenu"> A handle to the shortcut menu to be displayed. </param>
    /// <param name="uFlags"> A set of flags that specify how the menu is to be displayed. </param>
    /// <param name="x"> The horizontal position, in screen coordinates, at which to display the menu. </param>
    /// <param name="y"> The vertical position, in screen coordinates, at which to display the menu. </param>
    /// <param name="nReserved"> Reserved; must be zero. </param>
    /// <param name="hWnd"> A handle to the window that owns the shortcut menu. </param>
    /// <param name="prcRect"> Ignored. </param>
    /// <returns>
    ///     If TPM_RETURNCMD is specified in the uFlags parameter, the return value is the menu-item identifier of the item
    ///     that the user selected.
    ///     If the user cancels the menu without making a selection, or if an error occurs, the return value is zero.
    ///     If TPM_RETURNCMD is not specified in the uFlags parameter, the return value is nonzero if the function succeeds and
    ///     zero if it fails.
    /// </returns>
    [LibraryImport("user32.dll")]
    internal static partial int TrackPopupMenu(IntPtr hMenu, uint uFlags, int x, int y, int nReserved, IntPtr hWnd,
        IntPtr prcRect);

    /// <summary>
    ///     Enables, disables, or grays the specified menu item.
    /// </summary>
    /// <param name="hMenu">A handle to the menu where the item is located. </param>
    /// <param name="uIdEnableItem">The menu item to be enabled, disabled, or grayed, as determined by the uEnable parameter. </param>
    /// <param name="uEnable"> Control whether the menu item is enabled, disabled, or grayed. </param>
    /// <returns>
    ///     The return value specifies the previous state of the menu item (it is either MF_DISABLED, MF_ENABLED, or
    ///     MF_GRAYED).
    ///     If the menu item does not exist, the return value is -1.
    /// </returns>
    [LibraryImport("user32.dll")]
    [return: MarshalAs(UnmanagedType.Bool)]
    internal static partial bool EnableMenuItem(IntPtr hMenu, uint uIdEnableItem, uint uEnable);

    [LibraryImport("user32.dll")]
    [return: MarshalAs(UnmanagedType.Bool)]
    internal static partial bool GetCursorPos(out POINT lpPoint);

    /// <summary>
    ///     Extends the window frame into the client area.
    /// </summary>
    /// <param name="hWnd"> The handle to the window in which the frame will be extended into the client area. </param>
    /// <param name="pMarInset">
    ///     A pointer to a <see cref="MARGINS" /> structure that describes the margins
    ///     to use when extending the frame into the client area.
    /// </param>
    /// <returns></returns>
    [LibraryImport("dwmapi.dll")]
    internal static partial int DwmExtendFrameIntoClientArea(IntPtr hWnd, ref MARGINS pMarInset);

    /// <summary>
    ///     Sets various information regarding DWM window attributes.
    /// </summary>
    /// <param name="hwnd"> The window whose information is to be changed. </param>
    /// <param name="pAttrData"> Pointer to a structure which both specifies and delivers the attribute data. </param>
    [LibraryImport("user32.dll")]
    internal static partial int SetWindowCompositionAttribute(IntPtr hwnd,
        ref WINDOWCOMPOSITIONATTRIBDATA pAttrData);

    /// <summary>
    ///     Sets the value of Desktop Window Manager (DWM) non-client rendering attributes for a window.
    /// </summary>
    /// <param name="hwnd"> The handle to the window for which the attribute value is to be set. </param>
    /// <param name="dwAttribute">
    ///     A flag describing which value to set, specified as a value of the
    ///     <see cref="DWMWINDOWATTRIBUTE" /> enumeration.
    ///     This parameter specifies which attribute to set, and the pvAttribute parameter points to the value to set.
    /// </param>
    /// <param name="pvAttribute">
    ///     A pointer to an object containing the attribute value to set. The type of the value set depends on the value of the
    ///     dwAttribute parameter.
    ///     The <see cref="DWMWINDOWATTRIBUTE" /> enumeration topic indicates, in the row for each flag,
    ///     what type of value you should pass a pointer to in the pvAttribute parameter.
    /// </param>
    /// <param name="cbAttribute">
    ///     The size, in bytes, of the attribute value being set via the pvAttribute parameter.
    ///     The type of the value set, and therefore its size in bytes, depends on the value of the dwAttribute parameter.
    /// </param>
    [LibraryImport("dwmapi.dll")]
    internal static partial int DwmSetWindowAttribute(IntPtr hwnd, DWMWINDOWATTRIBUTE dwAttribute,
        ref int pvAttribute, int cbAttribute);

    [LibraryImport("user32.dll")]
    [return: MarshalAs(UnmanagedType.Bool)]
    internal static partial bool ShowWindow(IntPtr hWnd, int nCmdShow);

    [LibraryImport("user32.dll")]
    [return: MarshalAs(UnmanagedType.Bool)]
    internal static partial bool SetForegroundWindow(IntPtr hWnd);

    [LibraryImport("user32.dll")]
    internal static partial IntPtr GetForegroundWindow();
}