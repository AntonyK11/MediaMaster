using System.Drawing;
using System.Runtime.InteropServices;
using Microsoft.UI.Xaml;
using WinUIEx;
using MediaMaster.Interfaces.Services;
using MediaMaster.Extensions;
using Microsoft.UI.Xaml.Controls;
using WinUIEx.Messaging;
using static MediaMaster.Services.WindowsApiService;
using static MediaMaster.Services.WindowsNativeValues;


namespace MediaMaster.Services;

internal class TrayIconService
{
    public static int LOWORD(int n)
    {
        return n & 0xffff;
    }

    public const int FW_DONTCARE = 0;
    public const int FW_THIN = 100;
    public const int FW_EXTRALIGHT = 200;
    public const int FW_LIGHT = 300;
    public const int FW_NORMAL = 400;
    public const int FW_MEDIUM = 500;
    public const int FW_SEMIBOLD = 600;
    public const int FW_BOLD = 700;
    public const int FW_EXTRABOLD = 800;
    public const int FW_HEAVY = 900;

    public const int DEFAULT_QUALITY = 0;
    public const int DRAFT_QUALITY = 1;
    public const int PROOF_QUALITY = 2;
    public const int NONANTIALIASED_QUALITY = 3;
    public const int ANTIALIASED_QUALITY = 4;
    public const int CLEARTYPE_QUALITY = 5;
    public const int CLEARTYPE_NATURAL_QUALITY = 6;

    public const int DEFAULT_PITCH = 0;
    public const int FIXED_PITCH = 1;
    public const int VARIABLE_PITCH = 2;
    public const int MONO_FONT = 8;

    public const int OUT_DEFAULT_PRECIS = 0;
    public const int OUT_STRING_PRECIS = 1;
    public const int OUT_CHARACTER_PRECIS = 2;
    public const int OUT_STROKE_PRECIS = 3;
    public const int OUT_TT_PRECIS = 4;
    public const int OUT_DEVICE_PRECIS = 5;
    public const int OUT_RASTER_PRECIS = 6;
    public const int OUT_TT_ONLY_PRECIS = 7;
    public const int OUT_OUTLINE_PRECIS = 8;
    public const int OUT_SCREEN_OUTLINE_PRECIS = 9;
    public const int OUT_PS_ONLY_PRECIS = 10;

    public const int CLIP_DEFAULT_PRECIS = 0;
    public const int CLIP_CHARACTER_PRECIS = 1;
    public const int CLIP_STROKE_PRECIS = 2;
    public const int CLIP_MASK = 0xf;
    public const int CLIP_LH_ANGLES = (1 << 4);
    public const int CLIP_TT_ALWAYS = (2 << 4);
    public const int CLIP_DFA_DISABLE = (4 << 4);
    public const int CLIP_EMBEDDED = (8 << 4);

    public const int ANSI_CHARSET = 0;
    public const int DEFAULT_CHARSET = 1;
    public const int SYMBOL_CHARSET = 2;

        /* Font Families */
    public const int FF_DONTCARE = (0 << 4);  /* Don't care or don't know. */
    public const int FF_ROMAN = (1 << 4);  /* Variable stroke width, serifed. */
    /* Times Roman, Century Schoolbook, etc. */
    public const int FF_SWISS = (2 << 4);  /* Variable stroke width, sans-serifed. */
    /* Helvetica, Swiss, etc. */
    public const int FF_MODERN = (3 << 4);  /* Constant stroke width, serifed or sans-serifed. */
    /* Pica, Elite, Courier, etc. */
    public const int FF_SCRIPT = (4 << 4);  /* Cursive, etc. */
    public const int FF_DECORATIVE = (5 << 4);  /* Old English, etc. */

    public const int WM_USER = 0x0400;
    public const int WM_TRAYMOUSEMESSAGE = WM_USER + 1024;

    public const int WM_CONTEXTMENU = 0x007B;
    public const int WM_LBUTTONDOWN = 0x0201;
    public const int WM_LBUTTONUP = 0x0202;
    public const int WM_LBUTTONDBLCLK = 0x0203;
    public const int WM_RBUTTONDOWN = 0x0204;
    public const int WM_RBUTTONUP = 0x0205;
    public const int WM_ENTERMENULOOP = 0x0211;
    public const int WM_EXITMENULOOP = 0x0212;
    public const int WM_INITMENUPOPUP = 0x0117;
    public const int WM_UNINITMENUPOPUP = 0x0125;

    public const int WM_DRAWITEM = 0x002B;
    public const int WM_MEASUREITEM = 0x002C;

    IntPtr hFontMenu = IntPtr.Zero;
    int m_SizeBitmap = 11;

    private IntPtr hIcon;
    WindowMessageMonitor? monitor;
    WindowMessageMonitor? monitor2;

    private Color _darkBackgroundColor = Color.FromArgb(44, 44, 44);
    private Color _lightBackgroundColor = Color.FromArgb(249, 249, 249);
    private IntPtr _darkBackgroundHBrush;
    private IntPtr _lightBackgroundHBrush;

    private Color _darkBackgroundPointerOverBrush = Color.FromArgb(56, 56, 56);
    private Color _lightBackgroundPointerOverBrush = Color.FromArgb(240, 240, 240);

    private Color _darkForegroundBrush = Color.FromArgb(255, 255, 255);
    private Color _lightForegroundBrush = Color.FromArgb(26, 26, 26);

    private Color _darkForegroundBrushDisabled = Color.FromArgb(121, 121, 121);
    private Color _lightForegroundBrushDisabled = Color.FromArgb(159, 159, 159);

    private Color _darkDividerStroke = Color.FromArgb(61, 61, 61);
    private Color _lightDividerStroke = Color.FromArgb(234, 234, 234);

    public Color BackgroundColor = Color.Empty;
    public IntPtr BackgroundHBrush = IntPtr.Zero;
    public Color BackgroundBrushPointerOver = Color.Empty;

    public Color ForegroundBrush = Color.Empty;
    private Color ForegroundBrushDisabled = Color.Empty;

    public Color DividerStrokeBrush = Color.Empty;

    public IntPtr trayHMenu;
    public ICollection<IntPtr> hMenus = [];
    UIntPtr m_initToken = UIntPtr.Zero;
    IntPtr hImageList = IntPtr.Zero;

    public IntPtr MessageWindow;

    private uint TaskbarRestartMessageId;

    public TrayIconService()
    {
        var window = new Window();
        window.MoveAndResize(-10000, -10000, 0, 0);
        window.Activate();
        window.Hide();
        MessageWindow = window.GetWindowHandle();

        _darkBackgroundHBrush = CreateSolidBrush((IntPtr)ColorTranslator.ToWin32(_darkBackgroundColor));
        _lightBackgroundHBrush = CreateSolidBrush((IntPtr)ColorTranslator.ToWin32(_lightBackgroundColor));

        GdiplusStartupInput input = new()
        {
            GdiplusVersion = 1,
            SuppressBackgroundThread = false,
            SuppressExternalCodecs = false
        };
        GdiplusStartupOutput output = new();

        Status nStatus = GdiplusStartup(ref m_initToken, in input, ref output);

        TaskbarRestartMessageId = RegisterWindowMessage("TaskbarCreated");

        monitor = new WindowMessageMonitor(MessageWindow); // Cannot use SetWindowSubclass as it make the app crash for some reason
        monitor.WindowMessageReceived += WindowSubClass;

        if (App.MainWindow != null)
        {
            monitor2 = new WindowMessageMonitor(App.MainWindow);
            monitor2.WindowMessageReceived += WindowSubClass;
        }
        hIcon = LoadImage(IntPtr.Zero, @"Assets\WindowIcon.ico", GDI_IMAGE_TYPE.IMAGE_ICON, 32, 32, IMAGE_FLAGS.LR_LOADFROMFILE);

        hFontMenu = CreateFont(19, 0, 0, 0, FW_DONTCARE, 0, 0, 0, DEFAULT_CHARSET, OUT_TT_PRECIS, CLIP_DEFAULT_PRECIS,
            CLEARTYPE_NATURAL_QUALITY, VARIABLE_PITCH | FF_DONTCARE, "Segoe UI");

        trayHMenu = CreateMenu();

        SetTheme(App.GetService<IThemeSelectorService>().ActualTheme);
        App.GetService<IThemeSelectorService>().ThemeChanged += (_, theme) => SetTheme(theme);
    }

    [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
    public struct ODM_DATA
    {
        public bool hasIcon;
        public IntPtr hBitmap;
        [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 128)]
        public string text;
    }

    private void SetTheme(ElementTheme theme)
    {
        var dark = theme == ElementTheme.Dark;

        BackgroundColor = dark ? _darkBackgroundColor : _lightBackgroundColor;
        BackgroundHBrush = dark ? _darkBackgroundHBrush : _lightBackgroundHBrush;
        BackgroundBrushPointerOver = dark ? _darkBackgroundPointerOverBrush : _lightBackgroundPointerOverBrush;

        ForegroundBrush = dark ? _darkForegroundBrush : _lightForegroundBrush;
        ForegroundBrushDisabled = dark ? _darkForegroundBrushDisabled : _lightForegroundBrushDisabled;

        DividerStrokeBrush = dark ? _darkDividerStroke : _lightDividerStroke;

        foreach (var menu in hMenus)
        {
            UpdateMenuBackground(menu);
        }

        SetupImageList(dark ? "Dark" : "Light");
    }

    public void UpdateMenuBackground(IntPtr hMenu)
    {
        var info = new MENUINFO
        {
            cbSize = (uint)Marshal.SizeOf(typeof(MENUINFO)),
            fMask = MENUINFO_MASK.MIM_BACKGROUND,
            hbrBack = BackgroundHBrush
        };
        SetMenuInfo(hMenu, in info);
    }

    public unsafe void SetupImageList(string theme)
    {
        IntPtr closeIconHBitmap = IntPtr.Zero;
        IntPtr minimizeIconHBitmap = IntPtr.Zero;
        IntPtr maximizeIconHBitmap = IntPtr.Zero;
        IntPtr restoreIconHBitmap = IntPtr.Zero;

        IntPtr image = new IntPtr();
        IntPtr* hImage = &image;
        var nStatus = GdipCreateBitmapFromFile($@"{AppContext.BaseDirectory}Assets\Restore{theme}.png", ref hImage);
        if (nStatus == Status.Ok)
        {
            GdipCreateHBITMAPFromBitmap(hImage, ref restoreIconHBitmap, ColorTranslator.ToWin32(BackgroundColor));
            GdipDisposeImage(hImage);
        }
        nStatus = GdipCreateBitmapFromFile($@"{AppContext.BaseDirectory}Assets\Minimize{theme}.png", ref hImage);
        if (nStatus == Status.Ok)
        {
            GdipCreateHBITMAPFromBitmap(hImage, ref minimizeIconHBitmap, ColorTranslator.ToWin32(BackgroundColor));
            GdipDisposeImage(hImage);
        }
        nStatus = GdipCreateBitmapFromFile($@"{AppContext.BaseDirectory}Assets\Maximize{theme}.png", ref hImage);
        if (nStatus == Status.Ok)
        {
            GdipCreateHBITMAPFromBitmap(hImage, ref maximizeIconHBitmap, ColorTranslator.ToWin32(BackgroundColor));
            GdipDisposeImage(hImage);
        }
        nStatus = GdipCreateBitmapFromFile($@"{AppContext.BaseDirectory}Assets\Close{theme}.png", ref hImage);
        if (nStatus == Status.Ok)
        {
            GdipCreateHBITMAPFromBitmap(hImage, ref closeIconHBitmap, ColorTranslator.ToWin32(BackgroundColor));
            GdipDisposeImage(hImage);
        }

        if (hImageList != IntPtr.Zero)
        {
            ImageList_Destroy(hImageList);
        }

        hImageList = ImageList_Create(m_SizeBitmap, m_SizeBitmap, IMAGELIST_CREATION_FLAGS.ILC_COLOR32 | IMAGELIST_CREATION_FLAGS.ILC_MASK, 1, 0);
        _ = ImageList_AddMasked(hImageList, restoreIconHBitmap, ColorTranslator.ToWin32(BackgroundColor));
        _ = ImageList_AddMasked(hImageList, minimizeIconHBitmap, ColorTranslator.ToWin32(BackgroundColor));
        _ = ImageList_AddMasked(hImageList, maximizeIconHBitmap, ColorTranslator.ToWin32(BackgroundColor));
        _ = ImageList_AddMasked(hImageList, closeIconHBitmap, ColorTranslator.ToWin32(BackgroundColor));
    }

    public void SetInTray()
    {
        TrayMessage(MessageWindow, "Right-Click for options", hIcon, IntPtr.Zero,
            NOTIFY_ICON_MESSAGE.NIM_ADD, NOTIFY_ICON_INFOTIP_FLAGS.NIIF_NONE, null, null, 0);
    }

    public void RemoveFromTray()
    {
        TrayMessage(MessageWindow, "Right-Click for options", hIcon, IntPtr.Zero,
            NOTIFY_ICON_MESSAGE.NIM_DELETE, NOTIFY_ICON_INFOTIP_FLAGS.NIIF_NONE, null, null, 0);
    }

    public IntPtr CreateMenu()
    {
        IntPtr hMenu = CreatePopupMenu();

        AppendMenu(hMenu, MENU_ITEM_FLAGS.MF_STRING, 1, "Show window");
        AppendMenu(hMenu, MENU_ITEM_FLAGS.MF_SEPARATOR, 0, "");
        AppendMenu(hMenu, MENU_ITEM_FLAGS.MF_STRING, 2, "Exit");

        ODM_DATA odmd = new()
        {
            text = "Show window",
            hBitmap = IntPtr.Zero
        };
        var pODMD = Marshal.AllocHGlobal(Marshal.SizeOf(odmd));
        Marshal.StructureToPtr(odmd, pODMD, false);
        ModifyMenu(hMenu, 1, MENU_ITEM_FLAGS.MF_BYCOMMAND | MENU_ITEM_FLAGS.MF_OWNERDRAW, 1, pODMD);

        odmd = new ODM_DATA
        {
            text = "",
            hBitmap = IntPtr.Zero
        };
        pODMD = Marshal.AllocHGlobal(Marshal.SizeOf(odmd));
        Marshal.StructureToPtr(odmd, pODMD, false);
        ModifyMenu(hMenu, 0, MENU_ITEM_FLAGS.MF_BYCOMMAND | MENU_ITEM_FLAGS.MF_OWNERDRAW | MENU_ITEM_FLAGS.MF_SEPARATOR, 0, pODMD);
        odmd = new ODM_DATA
        {
            text = "Exit",
            hBitmap = IntPtr.Zero
        };
        pODMD = Marshal.AllocHGlobal(Marshal.SizeOf(odmd));
        Marshal.StructureToPtr(odmd, pODMD, false);
        ModifyMenu(hMenu, 2, MENU_ITEM_FLAGS.MF_BYCOMMAND | MENU_ITEM_FLAGS.MF_OWNERDRAW, 2, pODMD);

        UpdateMenuBackground(hMenu);
        hMenus.Add(hMenu);
        return hMenu;
    }

    private void WindowSubClass(object? sender, WindowMessageEventArgs args)
    {
        IntPtr hWnd = args.Message.Hwnd;
        uint uMsg = args.Message.MessageId;

        UIntPtr wParam = args.Message.WParam;
        IntPtr lParam = args.Message.LParam;
        if (uMsg == TaskbarRestartMessageId)
        {
            RemoveFromTray();
            SetInTray();
            return;
        }

        switch (uMsg)
        {
            case WM_TRAYMOUSEMESSAGE:
                {
                    switch (LOWORD((int)lParam))
                    {
                        case WM_CONTEXTMENU:
                        case WM_RBUTTONUP:
                            {
                                GetCursorPos(out var ptCursor);

                                // https://web.archive.org/web/20121015064650/https://support.microsoft.com/kb/135788
                                SetForegroundWindow(hWnd);
                                PostMessage(hWnd, 0, 0, 0);
                                var nCmd = TrackPopupMenu(trayHMenu, TRACK_POPUP_MENU_FLAGS.TPM_LEFTALIGN | TRACK_POPUP_MENU_FLAGS.TPM_LEFTBUTTON | TRACK_POPUP_MENU_FLAGS.TPM_RIGHTBUTTON | TRACK_POPUP_MENU_FLAGS.TPM_RETURNCMD, ptCursor.X, ptCursor.Y, 0, hWnd, IntPtr.Zero);
                                PostMessage(hWnd, 0, 0, 0);
                                if (nCmd != 0 && App.MainWindow != null)
                                {
                                    if (nCmd == 0x1)
                                    {
                                        App.MainWindow.Show();
                                    }
                                    else if (nCmd == 0x2)
                                    {
                                        App.Shutdown();
                                    }
                                }

                                args.Handled = true;
                                break;
                            }
                        case WM_LBUTTONUP:
                            {
                                App.Flyout?.Toggle_Flyout();
                                args.Handled = true;
                                break;
                            }
                    }
                }
                break;
            case WM_DRAWITEM:
                {
                    DRAWITEMSTRUCT? disNull = (DRAWITEMSTRUCT?)Marshal.PtrToStructure(lParam, typeof(DRAWITEMSTRUCT));
                    if (disNull == null) return;
                    DRAWITEMSTRUCT dis = (DRAWITEMSTRUCT)disNull;

                    ODM_DATA? odmdNull = (ODM_DATA?)Marshal.PtrToStructure((IntPtr)dis.itemData, typeof(ODM_DATA));
                    if (odmdNull == null) return;
                    ODM_DATA odmd = (ODM_DATA)odmdNull;

                    IntPtr hPen;
                    IntPtr hPenOld;
                    IntPtr hBrush;
                    IntPtr hBrushOld;

                    var menu_hwnd = FindWindow("#32768", "");
                    var preference = (int)DWM_WINDOW_CORNER_PREFERENCE.DWMWCP_ROUND;
                    DwmSetWindowAttribute(menu_hwnd, DwmWindowAttribute.WindowCornerPreference, ref preference, sizeof(uint));

                    //Debug.WriteLine(0x00000001 | 0x00000002 | 0x00000004);
                    //DWM_BLURBEHIND bb = new()
                    //{
                    //    dwFlags = 0x00000001 | (uint)(0x00000002 | 0x00000004),
                    //    fEnable = true,
                    //    hRgnBlur = PInvoke.CreateRectRgn(0, 0, -1, -1),
                    //    fTransitionOnMaximized = true
                    //};
                    //var result = PInvoke.DwmEnableBlurBehindWindow(menu_hwnd, &bb);
                    //Debug.WriteLine(result.Succeeded);
                    //PInvoke.DeleteObject(bb.hRgnBlur);

                    //MARGINS margins = new() { cxLeftWidth = -1, cxRightWidth = -1, cyTopHeight = -1, cyBottomHeight = -1 };
                    //PInvoke.DwmExtendFrameIntoClientArea(menu_hwnd, in margins);

                    //DWMWINDOWATTRIBUTE attribute = (Environment.OSVersion.Version.Build < 22523)
                    //    ? (DWMWINDOWATTRIBUTE)1029 // Undocumented BACKDROP attribute
                    //    : DWMWINDOWATTRIBUTE.DwmWindowAttribute;
                    //var attributeValue = (Environment.OSVersion.Version.Build < 22523) ? 1 : (int)WindowsNativeValues.DwSystemBackdropType.TransientWindow;
                    //result = PInvoke.DwmSetWindowAttribute(menu_hwnd, attribute, &attributeValue, sizeof(int));
                    //Debug.WriteLine(result.Succeeded);

                    //WindowsNativeValues.WindowCompositionAttributeData winCompAttrData = new()
                    //{
                    //    Attribute = WindowsNativeValues.WindowCompositionAttribute.AccentPolicy,
                    //    Data = Marshal.UnsafeAddrOfPinnedArrayElement([(int)WindowsNativeValues.AccentState.AccentEnableAcrylicBlurBehind], 0),
                    //    SizeOfData = sizeof(int)
                    //};
                    //WindowsApiService.SetWindowCompositionAttribute(menu_hwnd, ref winCompAttrData);

                    //attributeValue = 1;
                    //PInvoke.DwmSetWindowAttribute(menu_hwnd, DWMWINDOWATTRIBUTE.DWMWA_USE_IMMERSIVE_DARK_MODE, &attributeValue, sizeof(int));

                    RECT rcBack = dis.rcItem;
                    rcBack.top += 2;
                    rcBack.bottom -= 2;
                    rcBack.left += 2;
                    rcBack.right -= 2;

                    if ((dis.itemState & ODS_FLAGS.ODS_SELECTED) != 0 && (dis.itemState & ODS_FLAGS.ODS_GRAYED) == 0)
                    {
                        hPen = CreatePen(PEN_STYLE.PS_SOLID, 1, (IntPtr)ColorTranslator.ToWin32(BackgroundBrushPointerOver));
                        hPenOld = SelectObject(dis.hDC, hPen);

                        hBrush = CreateSolidBrush((IntPtr)ColorTranslator.ToWin32(BackgroundBrushPointerOver));
                        hBrushOld = SelectObject(dis.hDC, hBrush);
                    }
                    else
                    {
                        hPen = CreatePen(PEN_STYLE.PS_SOLID, 1, (IntPtr)ColorTranslator.ToWin32(BackgroundColor));
                        hPenOld = SelectObject(dis.hDC, hPen);

                        hBrush = CreateSolidBrush((IntPtr)ColorTranslator.ToWin32(BackgroundColor));
                        hBrushOld = SelectObject(dis.hDC, hBrush);
                    }
                    RoundRect(dis.hDC, rcBack.left, rcBack.top, rcBack.right, rcBack.bottom, 8, 8);

                    SelectObject(dis.hDC, hPenOld);
                    DeleteObject(hPen);

                    SelectObject(dis.hDC, hBrushOld);
                    DeleteObject(hBrush);

                    if (hFontMenu != IntPtr.Zero)
                    {
                        SelectObject(dis.hDC, hFontMenu);
                    }
                    SetBkMode(dis.hDC, BACKGROUND_MODE.TRANSPARENT);


                    if (odmd.text != "")
                    {
                        if ((dis.itemState & ODS_FLAGS.ODS_GRAYED) != 0)
                        {
                            SetTextColor(dis.hDC, (IntPtr)ColorTranslator.ToWin32(ForegroundBrushDisabled));
                        }
                        else
                        {
                            SetTextColor(dis.hDC, (IntPtr)ColorTranslator.ToWin32(ForegroundBrush));
                        }

                        RECT rcText = dis.rcItem;
                        if (odmd.hasIcon)
                        {
                            rcText.left += 27;
                        }
                        rcText.left += 14;
                        DrawText(dis.hDC, odmd.text.Replace("\t", "       "), -1, ref rcText, DRAW_TEXT_FORMAT.DT_SINGLELINE | DRAW_TEXT_FORMAT.DT_VCENTER);
                    }
                    else
                    {
                        hPen = CreatePen(PEN_STYLE.PS_SOLID, 1, (IntPtr)ColorTranslator.ToWin32(DividerStrokeBrush));
                        hPenOld = SelectObject(dis.hDC, hPen);
                        MoveToEx(dis.hDC, dis.rcItem.left, dis.rcItem.top + (dis.rcItem.bottom - dis.rcItem.top) / 2, out _);
                        LineTo(dis.hDC, dis.rcItem.right, dis.rcItem.top + (dis.rcItem.bottom - dis.rcItem.top) / 2);

                        SelectObject(dis.hDC, hPenOld);
                        DeleteObject(hPen);
                    }

                    if (odmd.hBitmap != IntPtr.Zero)
                    {
                        var index = odmd.hBitmap switch
                        {
                            0x9 => 0,
                            0xb => 2,
                            0xa => 4,
                            0x8 => 6,
                            _ => 0
                        };

                        if ((dis.itemState & ODS_FLAGS.ODS_GRAYED) != 0)
                        {
                            index += 1;
                        }

                        ImageList_Draw(
                            hImageList, 
                            index,
                            dis.hDC, 
                            dis.rcItem.left + 14, 
                            dis.rcItem.top + 10, 
                            IMAGE_LIST_DRAW_STYLE.ILD_TRANSPARENT
                            );
                    }
                }
                break;
            case WM_MEASUREITEM:
                {
                    MEASUREITEMSTRUCT? misNull = (MEASUREITEMSTRUCT?)Marshal.PtrToStructure(lParam, typeof(MEASUREITEMSTRUCT));
                    if (misNull == null) return;
                    MEASUREITEMSTRUCT mis = (MEASUREITEMSTRUCT)misNull;

                    ODM_DATA? odmdNull = (ODM_DATA?)Marshal.PtrToStructure((IntPtr)mis.itemData, typeof(ODM_DATA));
                    if (odmdNull == null) return;
                    ODM_DATA odmd = (ODM_DATA)odmdNull;

                    mis.itemWidth = 78;
                    if (odmd.text == "")
                    {
                        mis.itemHeight = 3;
                    }
                    else
                    {
                        mis.itemHeight = 32;
                        var tb = new TextBlock { Text = odmd.text, FontSize = 14 };
                        tb.Measure(new Windows.Foundation.Size(double.PositiveInfinity, double.PositiveInfinity));
                        var width = tb.DesiredSize.Width + 13;
                        if (odmd.hBitmap != IntPtr.Zero)
                        {
                            width += 29;
                        }
                        mis.itemWidth = (uint)width;
                    }
                    Marshal.StructureToPtr(mis, lParam, false);
                }
                break;
        }
    }

    ~TrayIconService()
    {
        RemoveFromTray();

        if (hFontMenu != IntPtr.Zero)
        {
            DeleteObject(hFontMenu);
        }

        if (hIcon != IntPtr.Zero)
        {
            DestroyIcon(hIcon);
        }

        if (hImageList != IntPtr.Zero)
        {
            ImageList_Destroy(hImageList);
        }

        GdiplusShutdown(m_initToken);
        monitor?.Dispose();
        monitor2?.Dispose();
    }

    internal static bool TrayMessage(IntPtr hWnd, string? sMessage, IntPtr hIcon, IntPtr hBalloonIcon, NOTIFY_ICON_MESSAGE nMessage, NOTIFY_ICON_INFOTIP_FLAGS dwInfoFlags, string? sInfo, string? sTitle, int nTimeOut)
    {
        NOTIFYICONDATAW nid = new()
        {
            //nid.cbSize = 956;
            /*if (IsOsVistaOrLater())
            nid.cbSize = sizeof(NOTIFYICONDATA);
            else
            nid.cbSize = NOTIFYICONDATA_V3_SIZE;*/
            guidItem = new Guid(),
            hWnd = hWnd,
            uFlags = NOTIFY_ICON_DATA_FLAGS.NIF_MESSAGE | NOTIFY_ICON_DATA_FLAGS.NIF_INFO,
            uCallbackMessage = WM_TRAYMOUSEMESSAGE
        };

        if (!sMessage.IsNullOrEmpty())
        {
            nid.szTip = sMessage!;
            nid.uFlags |= NOTIFY_ICON_DATA_FLAGS.NIF_TIP;
        }
        if (hIcon != IntPtr.Zero)
        {
            nid.hIcon = hIcon;
            nid.uFlags |= NOTIFY_ICON_DATA_FLAGS.NIF_ICON;
        }
        if ((dwInfoFlags & NOTIFY_ICON_INFOTIP_FLAGS.NIIF_USER) != 0)
        {
            nid.hIcon = hIcon;
            nid.hBalloonIcon = hBalloonIcon;
            nid.uFlags |= NOTIFY_ICON_DATA_FLAGS.NIF_ICON;
        }
        nid.dwInfoFlags = dwInfoFlags;
        if (dwInfoFlags != 0 && sInfo != null && sTitle != null)
        {
            nid.szInfo = sInfo;
            nid.szInfoTitle = sTitle;
        }
        else
        {
            nid.szInfo = "";
            nid.szInfoTitle = "";
        }

        //nid.cbSize = (uint)sizeof(NOTIFYICONDATAW);
        nid.union.uVersion = 4;


        //if (nMessage == NOTIFY_ICON_MESSAGE.NIM_ADD)
        //{
        //    PInvoke.Shell_NotifyIcon(NOTIFY_ICON_MESSAGE.NIM_SETVERSION, nid);
        //}
        var result = Shell_NotifyIcon(nMessage, nid);

        //if (nMessage == NOTIFY_ICON_MESSAGE.NIM_ADD)
        //{
        //    var r = PInvoke.Shell_NotifyIcon(NOTIFY_ICON_MESSAGE.NIM_SETVERSION, nid);
        //}

        return result;
    }
}

