using System.Drawing;
using System.Runtime.InteropServices;
using static MediaMaster.Services.WindowsNativeValues;
using static MediaMaster.Services.WindowsNativeInterfaces;

namespace MediaMaster.Services;

/// <summary>
///     Provides access to Windows API functions.
/// </summary>
public static partial class WindowsApiService
{
    [LibraryImport("uxtheme.dll", EntryPoint = "#136")]
    internal static partial int FlushMenuThemes();

    [LibraryImport("uxtheme.dll", EntryPoint = "#135")]
    internal static partial int SetPreferredAppMode(PreferredAppMode preferredAppMode);

    [LibraryImport("user32.dll", EntryPoint = "SendMessageA")]
    internal static partial IntPtr SendMessage(IntPtr hWnd, int msg, IntPtr wp, IntPtr lp);

    [LibraryImport("user32.dll")]
    internal static partial IntPtr GetSystemMenu(IntPtr hWnd, [MarshalAs(UnmanagedType.Bool)] bool bRevert);

    [LibraryImport("user32.dll")]
    internal static partial int TrackPopupMenu(IntPtr hMenu, WindowsNativeValues.TRACK_POPUP_MENU_FLAGS uFlags, int x, int y, int nReserved, IntPtr hWnd, IntPtr prcRect);

    [LibraryImport("user32.dll")]
    [return: MarshalAs(UnmanagedType.Bool)]
    internal static partial bool EnableMenuItem(IntPtr hMenu, uint uIdEnableItem, WindowsNativeValues.MENU_ITEM_FLAGS uEnable);

    [LibraryImport("dwmapi.dll")]
    internal static partial int DwmExtendFrameIntoClientArea(IntPtr hWnd, ref Margins pMarInset);

    [LibraryImport("user32.dll")]
    internal static partial int SetWindowCompositionAttribute(IntPtr hwnd, ref WindowCompositionAttributeData pAttrData);


    [LibraryImport("dwmapi.dll")]
    internal static partial int DwmSetWindowAttribute(IntPtr hwnd, DwmWindowAttribute dwAttribute, ref int pvAttribute, int cbAttribute);

    [LibraryImport("user32.dll")]
    internal static partial IntPtr GetForegroundWindow();
    
    [LibraryImport("shell32.dll", SetLastError = true, StringMarshalling = StringMarshalling.Utf16)]
    internal static partial HResult SHCreateItemFromParsingName(string path, IntPtr pbc, ref Guid riid, out IShellItem shellItem);

    [LibraryImport("gdi32.dll")]
    [return: MarshalAs(UnmanagedType.Bool)]
    public static partial bool DeleteObject(IntPtr hObject);

    [LibraryImport("gdi32.dll", EntryPoint = "GetObjectA")]
    internal static partial int GetObject(IntPtr hgdiobj, int cbBuffer, out BITMAP lpvObject);

    [LibraryImport("gdi32.dll")]
    internal static partial IntPtr CreateCompatibleDC(IntPtr hdc);

    [LibraryImport("gdi32.dll")]
    internal static partial IntPtr SelectObject(IntPtr hdc, IntPtr hgdiobj);

    [LibraryImport("gdi32.dll")]
    [return: MarshalAs(UnmanagedType.Bool)]
    internal static partial bool DeleteDC(IntPtr hdc);

    [LibraryImport("gdi32.dll")]
    internal static partial int GetDIBits(IntPtr hdc, IntPtr hbm, uint start, uint cLines, byte[] lpvBits, ref BITMAPV5HEADER lpbmi, uint usage);

    [LibraryImport("user32.dll")]
    [return: MarshalAs(UnmanagedType.Bool)]
    internal static partial bool IsWindow(IntPtr hWnd);

    [LibraryImport("GDI32.dll")]
    internal static partial IntPtr CreateSolidBrush(IntPtr color);

    [DllImport("gdiplus.dll")]
    internal static extern Status GdiplusStartup(ref UIntPtr token, in GdiplusStartupInput input, ref GdiplusStartupOutput output);

    [LibraryImport("USER32.dll", EntryPoint = "LoadImageW", SetLastError = true, StringMarshalling = StringMarshalling.Utf16)]
    internal static partial IntPtr LoadImage(IntPtr hInst, string name, WindowsNativeValues.GDI_IMAGE_TYPE type, int cx, int cy, WindowsNativeValues.IMAGE_FLAGS fuLoad);

    [LibraryImport("GDI32.dll", EntryPoint = "CreateFontW", StringMarshalling = StringMarshalling.Utf16)]
    internal static partial IntPtr CreateFont(int cHeight, int cWidth, int cEscapement, int cOrientation, int cWeight, uint bItalic, uint bUnderline, uint bStrikeOut, uint iCharSet, uint iOutPrecision, uint iClipPrecision, uint iQuality, uint iPitchAndFamily, string pszFaceName);

    [LibraryImport("USER32.dll", SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    internal static partial bool SetMenuInfo(IntPtr param0, in WindowsNativeValues.MENUINFO param1);

    [LibraryImport("gdiplus.dll", StringMarshalling = StringMarshalling.Utf16)]
    internal static unsafe partial Status GdipCreateBitmapFromFile(string filename, ref IntPtr* bitmap);

    [LibraryImport("gdiplus.dll")]
    internal static unsafe partial Status GdipCreateHBITMAPFromBitmap(IntPtr* bitmap, ref IntPtr hbmReturn, IntPtr background);

    [LibraryImport("gdiplus.dll")]
    internal static unsafe partial Status GdipDisposeImage(IntPtr* image);

    [LibraryImport("COMCTL32.dll")]
    [return: MarshalAs(UnmanagedType.Bool)]
    internal static partial bool ImageList_Destroy(IntPtr himl);

    [LibraryImport("COMCTL32.dll")]
    internal static partial IntPtr ImageList_Create(int cx, int cy, IMAGELIST_CREATION_FLAGS flags, int cInitial, int cGrow);

    [LibraryImport("COMCTL32.dll")]
    internal static partial int ImageList_AddMasked(IntPtr himl, IntPtr hbmImage, IntPtr crMask);

    [LibraryImport("USER32.dll", SetLastError = true)]
    internal static partial IntPtr CreatePopupMenu();

    [LibraryImport("USER32.dll", EntryPoint = "AppendMenuW", SetLastError = true, StringMarshalling = StringMarshalling.Utf16)]
    [return: MarshalAs(UnmanagedType.Bool)]
    internal static partial bool AppendMenu(IntPtr hMenu, WindowsNativeValues.MENU_ITEM_FLAGS uFlags, nuint uIDNewItem, string lpNewItem);

    [LibraryImport("USER32.dll", EntryPoint = "ModifyMenuW", SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    internal static partial bool ModifyMenu(IntPtr hMnu, uint uPosition, WindowsNativeValues.MENU_ITEM_FLAGS uFlags, nuint uIDNewItem, IntPtr lpNewItem);

    [DllImport("USER32.dll", SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    internal static extern bool GetCursorPos(out Point lpPoint);

    [LibraryImport("USER32.dll")]
    [return: MarshalAs(UnmanagedType.Bool)]
    internal static partial bool SetForegroundWindow(IntPtr hWnd);

    [LibraryImport("USER32.dll", EntryPoint = "PostMessageW", SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    internal static partial bool PostMessage(IntPtr hWnd, uint Msg, IntPtr wParam, UIntPtr lParam);

    [LibraryImport("USER32.dll", EntryPoint = "FindWindowW", SetLastError = true, StringMarshalling = StringMarshalling.Utf16)]
    internal static partial IntPtr FindWindow(string lpClassName, string lpWindowName);

    [LibraryImport("GDI32.dll")]
    internal static partial IntPtr CreatePen(PEN_STYLE iStyle, int cWidth, IntPtr color);

    [LibraryImport("GDI32.dll")]
    [return: MarshalAs(UnmanagedType.Bool)]
    internal static partial bool RoundRect(IntPtr hdc, int left, int top, int right, int bottom, int width, int height);

    [LibraryImport("GDI32.dll")]
    internal static partial int SetBkMode(IntPtr hdc, BACKGROUND_MODE mode);

    [LibraryImport("GDI32.dll")]
    internal static partial IntPtr SetTextColor(IntPtr hdc, IntPtr color);

    [LibraryImport("USER32.dll", EntryPoint = "DrawTextW", StringMarshalling = StringMarshalling.Utf16)]
    internal static partial int DrawText(IntPtr hdc, string lpchText, int cchText, ref RECT lprc, DRAW_TEXT_FORMAT format);

    [DllImport("GDI32.dll")]
    [return: MarshalAs(UnmanagedType.Bool)]
    internal static extern bool MoveToEx(IntPtr hdc, int x, int y, out Point lppt);

    [LibraryImport("GDI32.dll")]
    [return: MarshalAs(UnmanagedType.Bool)]
    internal static partial bool LineTo(IntPtr hdc, int x, int y);

    [LibraryImport("COMCTL32.dll")]
    [return: MarshalAs(UnmanagedType.Bool)]
    internal static partial bool ImageList_Draw(IntPtr himl, int i, IntPtr hdcDst, int x, int y, IMAGE_LIST_DRAW_STYLE fStyle);

    [LibraryImport("USER32.dll", SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    internal static partial bool DestroyIcon(IntPtr hIcon);

    [LibraryImport("gdiplus.dll")]
    internal static partial void GdiplusShutdown(UIntPtr token);

    [DllImport("SHELL32.dll", EntryPoint = "Shell_NotifyIconW")]
    [return: MarshalAs(UnmanagedType.Bool)]
    internal static extern bool Shell_NotifyIcon(NOTIFY_ICON_MESSAGE dwMessage, in NOTIFYICONDATAW lpData);

    [DllImport("User32.dll", EntryPoint = "RegisterWindowMessageA")]
    internal static extern uint RegisterWindowMessage(string lpString);

    [LibraryImport("USER32.dll", EntryPoint = "GetMenuItemInfoW", SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    internal static partial bool GetMenuItemInfo(IntPtr hmenu, uint item, [MarshalAs(UnmanagedType.Bool)] bool fByPosition, ref MENUITEMINFOW lpmii);

    [LibraryImport("USER32.dll", EntryPoint = "SetMenuItemInfoW", SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    internal static partial bool SetMenuItemInfo(IntPtr hmenu, uint item, [MarshalAs(UnmanagedType.Bool)] bool fByPositon, in MENUITEMINFOW lpmii);

    [LibraryImport("OLE32.dll")]
    internal static partial HResult OleInitialize();

    [LibraryImport("OLE32.dll")]
    internal static partial void OleUninitialize();
}