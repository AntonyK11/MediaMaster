using System.Diagnostics.CodeAnalysis;
using System.Drawing;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace MediaMaster.WIn32;

/// <summary>
///     Defines the native values used in Windows API.
/// </summary>
public static class WindowsNativeValues
{
    public const int
        WM_INITMENU = 0x0116,
        WM_SYSCOMMAND = 0x0112,
        TPM_RETURNCMD = 0x0100;

    public enum AccentState
    {
        AccentDisabled,
        AccentEnableGradient,
        AccentEnableTransparentGradient,
        // Aero effect
        AccentEnableBlurBehind,
        // Acrylic effect
        AccentEnableAcrylicBlurBehind,
        // Mica effect
        AccentEnableHostBackdrop,
        AccentInvalidState
    }

    public enum DwSystemBackdropType
    {
        Auto,
        None,
        MainWindow,
        TransientWindow,
        TabedWindow,
    }

    public enum DwmWindowAttribute
    {
        Ncrenderingenabled,
        Ncrenderingpolicy,
        Transitionsforcedisabled,
        AllowNcpaint = 4,
        CaptionButtonBounds = 5,
        NonclientRtlLayout = 6,
        ForceIconicRepresentation = 7,
        Flip3dPolicy = 8,
        ExtendedFrameBounds = 9,
        HasIconicBitmap = 10,
        DisallowPeek = 11,
        ExcludedFromPeek = 12,
        Cloak = 13,
        Cloaked = 14,
        FreezeRepresentation = 15,
        PassiveUpdateMode = 16,
        UseHostbackdropbrush = 17,
        UseImmersiveDarkMode = 20,
        WindowCornerPreference = 33,
        BorderColor = 34,
        CaptionColor = 35,
        TextColor = 36,
        VisibleFrameBorderThickness = 37,
        DwmWindowAttribute = 38,
        SystemBackdropType = 39,
        Last,
        SystemBackdropTypeDeprecated = 1029
    }

    public enum DWM_WINDOW_CORNER_PREFERENCE
    {
        DWMWCP_DEFAULT = 0,
        DWMWCP_DONOTROUND = 1,
        DWMWCP_ROUND = 2,
        DWMWCP_ROUNDSMALL = 3
    }

    public enum PreferredAppMode
    {
        Default,
        AllowDark,
        ForceDark,
        ForceLight,
        Max
    }
    
    
    public enum WindowCompositionAttribute
    {
        AccentPolicy = 19,
        UseDarkModeColors = 26,
    }

    [StructLayout(LayoutKind.Sequential)]
    public struct Margins
    {
        public int cxLeftWidth;
        public int cxRightWidth;
        public int cyTopHeight;
        public int cyBottomHeight;
    }

    [StructLayout(LayoutKind.Sequential)]
    public struct WindowCompositionAttributeData
    {
        public WindowCompositionAttribute Attribute;
        public IntPtr Data;
        public int SizeOfData;
    }

    [StructLayout(LayoutKind.Sequential)]
    public struct ImageListDrawParams
    {
        public int cbSize;
        public IntPtr himl;
        public int i;
        public IntPtr hdcDst;
        public int x;
        public int y;
        public int cx;
        public int cy;
        public int xBitmap;
        public int yBitmap;
        public int rgbBk;
        public int rgbFg;
        public int fStyle;
        public int dwRop;
        public int fState;
        public int Frame;
        public int crEffect;
    }

    [StructLayout(LayoutKind.Sequential)]
    public struct Size(int cx, int cy)
    {
        public int cx = cx;
        public int cy = cy;
    }

    [Flags]
    public enum SIIGBF
    {
        ResizeToFit = 0x000,
        BiggerSizeOk = 0x001,
        MemoryOnly = 0x002,
        IconOnly = 0x004,
        ThumbnailOnly = 0x008,
        InCacheOnly = 0x010,
        IconBackground = 0x080,
        ScaleUp = 0x100
    }

    [Flags]
    public enum SIGDN : uint
    {
        NormalDisplay = 0,
        ParentRelativeParsing = 0x80018001,
        ParentRelativeForAddressBar = 0x8001c001,
        DesktopAbsoluteParsing = 0x80028000,
        ParentRelativeEditing = 0x80031001,
        DesktopAbsoluteEditing = 0x8004c000,
        FileSystemPath = 0x80058000,
        Url = 0x80068000
    }

    [Flags]
    public enum HResult : uint
    {
        Ok = 0x0000,
        False = 0x0001,
        InvalidArguments = 0x80070057,
        OutOfMemory = 0x8007000E,
        NoInterface = 0x80004002,
        Fail = 0x80004005,
        ElementNotFound = 0x80070490,
        TypeElementNotFound = 0x8002802B,
        NoObject = 0x800401E5,
        Win32ErrorCanceled = 1223,
        Canceled = 0x800704C7,
        ResourceInUse = 0x800700AA,
        AccessDenied = 0x80030005
    }

    [StructLayout(LayoutKind.Sequential)]
    public struct BITMAP
    {
        public int bmType;
        public int bmWidth;
        public int bmHeight;
        public int bmWidthBytes;
        public short bmPlanes;
        public short bmBitsPixel;
        public IntPtr bmBits;
    }

    [StructLayout(LayoutKind.Sequential)]
    public struct BITMAPV5HEADER
    {
        public uint bV5Size;
        public int bV5Width;
        public int bV5Height;
        public short bV5Planes;
        public short bV5BitCount;
        public uint bV5Compression;
        public uint bV5SizeImage;
        public int bV5XPelsPerMeter;
        public int bV5YPelsPerMeter;
        public uint bV5ClrUsed;
        public uint bV5ClrImportant;
        public uint bV5RedMask;
        public uint bV5GreenMask;
        public uint bV5BlueMask;
        public uint bV5AlphaMask;
        public uint bV5CSType;
        public IntPtr bV5Endpoints;
        public uint bV5GammaRed;
        public uint bV5GammaGreen;
        public uint bV5GammaBlue;
        public uint bV5Intent;
        public uint bV5ProfileData;
        public uint bV5ProfileSize;
        public uint bV5Reserved;
    }

    public const int BI_BITFIELDS = 3;
    public const int DIB_RGB_COLORS = 0;


    internal struct GdiplusStartupInput
    {
        internal uint GdiplusVersion;

        internal nint DebugEventCallback;

        internal bool SuppressBackgroundThread;

        internal bool SuppressExternalCodecs;
    }

    internal struct GdiplusStartupOutput
    {
        internal nint NotificationHook;

        internal nint NotificationUnhook;
    }

    internal enum Status
    {
        Ok = 0,
        GenericError = 1,
        InvalidParameter = 2,
        OutOfMemory = 3,
        ObjectBusy = 4,
        InsufficientBuffer = 5,
        NotImplemented = 6,
        Win32Error = 7,
        WrongState = 8,
        Aborted = 9,
        FileNotFound = 10,
        ValueOverflow = 11,
        AccessDenied = 12,
        UnknownImageFormat = 13,
        FontFamilyNotFound = 14,
        FontStyleNotFound = 15,
        NotTrueTypeFont = 16,
        UnsupportedGdiplusVersion = 17,
        GdiplusNotInitialized = 18,
        PropertyNotFound = 19,
        PropertyNotSupported = 20,
        ProfileNotFound = 21,
    }
    internal enum GDI_IMAGE_TYPE : uint
    {
        IMAGE_BITMAP = 0U,
        IMAGE_CURSOR = 2U,
        IMAGE_ICON = 1U,
    }

    [Flags]
    internal enum IMAGE_FLAGS : uint
    {
        LR_CREATEDIBSECTION = 0x00002000,
        LR_DEFAULTCOLOR = 0x00000000,
        LR_DEFAULTSIZE = 0x00000040,
        LR_LOADFROMFILE = 0x00000010,
        LR_LOADMAP3DCOLORS = 0x00001000,
        LR_LOADTRANSPARENT = 0x00000020,
        LR_MONOCHROME = 0x00000001,
        LR_SHARED = 0x00008000,
        LR_VGACOLOR = 0x00000080,
        LR_COPYDELETEORG = 0x00000008,
        LR_COPYFROMRESOURCE = 0x00004000,
        LR_COPYRETURNORG = 0x00000004,
    }

    internal struct MENUINFO
    {
        internal uint cbSize;
        internal MENUINFO_MASK fMask;
        internal MENUINFO_STYLE dwStyle;
        internal uint cyMax;
        internal IntPtr hbrBack;
        internal uint dwContextHelpID;
        internal nuint dwMenuData;
    }

    [Flags]
    internal enum MENUINFO_MASK : uint
    {
        MIM_APPLYTOSUBMENUS = 0x80000000,
        MIM_BACKGROUND = 0x00000002,
        MIM_HELPID = 0x00000004,
        MIM_MAXHEIGHT = 0x00000001,
        MIM_MENUDATA = 0x00000008,
        MIM_STYLE = 0x00000010,
    }

    [Flags]
    internal enum MENUINFO_STYLE : uint
    {
        MNS_AUTODISMISS = 0x10000000,
        MNS_CHECKORBMP = 0x04000000,
        MNS_DRAGDROP = 0x20000000,
        MNS_MODELESS = 0x40000000,
        MNS_NOCHECK = 0x80000000,
        MNS_NOTIFYBYPOS = 0x08000000,
    }

    [Flags]
    internal enum IMAGELIST_CREATION_FLAGS : uint
    {
        ILC_MASK = 0x00000001,
        ILC_COLOR = 0x00000000,
        ILC_COLORDDB = 0x000000FE,
        ILC_COLOR4 = 0x00000004,
        ILC_COLOR8 = 0x00000008,
        ILC_COLOR16 = 0x00000010,
        ILC_COLOR24 = 0x00000018,
        ILC_COLOR32 = 0x00000020,
        ILC_PALETTE = 0x00000800,
        ILC_MIRROR = 0x00002000,
        ILC_PERITEMMIRROR = 0x00008000,
        ILC_ORIGINALSIZE = 0x00010000,
        ILC_HIGHQUALITYSCALE = 0x00020000,
    }

    [Flags]
    internal enum MENU_ITEM_FLAGS : uint
    {
        MF_BYCOMMAND = 0x00000000,
        MF_BYPOSITION = 0x00000400,
        MF_BITMAP = 0x00000004,
        MF_CHECKED = 0x00000008,
        MF_DISABLED = 0x00000002,
        MF_ENABLED = 0x00000000,
        MF_GRAYED = 0x00000001,
        MF_MENUBARBREAK = 0x00000020,
        MF_MENUBREAK = 0x00000040,
        MF_OWNERDRAW = 0x00000100,
        MF_POPUP = 0x00000010,
        MF_SEPARATOR = 0x00000800,
        MF_STRING = 0x00000000,
        MF_UNCHECKED = 0x00000000,
        MF_INSERT = 0x00000000,
        MF_CHANGE = 0x00000080,
        MF_APPEND = 0x00000100,
        MF_DELETE = 0x00000200,
        MF_REMOVE = 0x00001000,
        MF_USECHECKBITMAPS = 0x00000200,
        MF_UNHILITE = 0x00000000,
        MF_HILITE = 0x00000080,
        MF_DEFAULT = 0x00001000,
        MF_SYSMENU = 0x00002000,
        MF_HELP = 0x00004000,
        MF_RIGHTJUSTIFY = 0x00004000,
        MF_MOUSESELECT = 0x00008000,
        MF_END = 0x00000080,
    }


    [Flags]
    internal enum PEN_STYLE
    {
        PS_GEOMETRIC = 0x00010000,
        PS_COSMETIC = 0x00000000,
        PS_SOLID = 0x00000000,
        PS_DASH = 0x00000001,
        PS_DOT = 0x00000002,
        PS_DASHDOT = 0x00000003,
        PS_DASHDOTDOT = 0x00000004,
        PS_NULL = 0x00000005,
        PS_INSIDEFRAME = 0x00000006,
        PS_USERSTYLE = 0x00000007,
        PS_ALTERNATE = 0x00000008,
        PS_STYLE_MASK = 0x0000000F,
        PS_ENDCAP_ROUND = 0x00000000,
        PS_ENDCAP_SQUARE = 0x00000100,
        PS_ENDCAP_FLAT = 0x00000200,
        PS_ENDCAP_MASK = 0x00000F00,
        PS_JOIN_ROUND = 0x00000000,
        PS_JOIN_BEVEL = 0x00001000,
        PS_JOIN_MITER = 0x00002000,
        PS_JOIN_MASK = 0x0000F000,
        PS_TYPE_MASK = 0x000F0000,
    }

    internal enum BACKGROUND_MODE : uint
    {
        OPAQUE = 2U,
        TRANSPARENT = 1U,
    }

    [Flags]
    internal enum DRAW_TEXT_FORMAT : uint
    {
        DT_BOTTOM = 0x00000008,
        DT_CALCRECT = 0x00000400,
        DT_CENTER = 0x00000001,
        DT_EDITCONTROL = 0x00002000,
        DT_END_ELLIPSIS = 0x00008000,
        DT_EXPANDTABS = 0x00000040,
        DT_EXTERNALLEADING = 0x00000200,
        DT_HIDEPREFIX = 0x00100000,
        DT_INTERNAL = 0x00001000,
        DT_LEFT = 0x00000000,
        DT_MODIFYSTRING = 0x00010000,
        DT_NOCLIP = 0x00000100,
        DT_NOFULLWIDTHCHARBREAK = 0x00080000,
        DT_NOPREFIX = 0x00000800,
        DT_PATH_ELLIPSIS = 0x00004000,
        DT_PREFIXONLY = 0x00200000,
        DT_RIGHT = 0x00000002,
        DT_RTLREADING = 0x00020000,
        DT_SINGLELINE = 0x00000020,
        DT_TABSTOP = 0x00000080,
        DT_TOP = 0x00000000,
        DT_VCENTER = 0x00000004,
        DT_WORDBREAK = 0x00000010,
        DT_WORD_ELLIPSIS = 0x00040000,
    }

    internal struct RECT
    {
        internal int left;
        internal int top;
        internal int right;
        internal int bottom;

        internal RECT(Rectangle value) : this(value.Left, value.Top, value.Right, value.Bottom) { }
        internal RECT(Point location, System.Drawing.Size size) : this(location.X, location.Y, unchecked(location.X + size.Width), unchecked(location.Y + size.Height)) { }
        internal RECT(int left, int top, int right, int bottom)
        {
            this.left = left;
            this.top = top;
            this.right = right;
            this.bottom = bottom;
        }

        internal static RECT FromXYWH(int x, int y, int width, int height) => new(x, y, unchecked(x + width), unchecked(y + height));

        internal readonly int Width => unchecked(right - left);
        internal readonly int Height => unchecked(bottom - top);

        internal readonly int X => left;
        internal readonly int Y => top;

        internal readonly System.Drawing.Size Size => new(Width, Height);
    }

    [Flags]
    internal enum IMAGE_LIST_DRAW_STYLE : uint
    {
        ILD_NORMAL = 0x00000000,
        ILD_TRANSPARENT = 0x00000001,
        ILD_BLEND25 = 0x00000002,
        ILD_FOCUS = 0x00000002,
        ILD_BLEND50 = 0x00000004,
        ILD_SELECTED = 0x00000004,
        ILD_BLEND = 0x00000004,
        ILD_MASK = 0x00000010,
        ILD_IMAGE = 0x00000020,
        ILD_ROP = 0x00000040,
        ILD_OVERLAYMASK = 0x00000F00,
        ILD_PRESERVEALPHA = 0x00001000,
        ILD_SCALE = 0x00002000,
        ILD_DPISCALE = 0x00004000,
        ILD_ASYNC = 0x00008000,
    }

    [Flags]
    internal enum ICON_DRAW_STYLE : uint
    {
        DI_MASK = 0x0001,
        DI_IMAGE = 0x0002,
        DI_NORMAL = 0x0003,
        DI_COMPAT = 0x0004,
        DI_DEFAULTSIZE = 0x0008,
        DI_NOMIRROR = 0x0010
    }

    internal enum NOTIFY_ICON_MESSAGE : uint
    {
        NIM_ADD = 0U,
        NIM_MODIFY = 1U,
        NIM_DELETE = 2U,
        NIM_SETFOCUS = 3U,
        NIM_SETVERSION = 4U,
    }


    internal struct NOTIFYICONDATAW()
    {
        internal uint cbSize = 0;

        internal IntPtr hWnd = 0;

        internal uint uID = 0;

        internal NOTIFY_ICON_DATA_FLAGS uFlags = 0;

        internal uint uCallbackMessage = 0;

        internal IntPtr hIcon = 0;

        internal __char_128 szTip = default;

        internal NOTIFY_ICON_STATE dwState = 0;

        internal NOTIFY_ICON_STATE dwStateMask = 0;

        internal __char_256 szInfo = default;

        internal Union union = default;

        internal __char_64 szInfoTitle = default;

        internal NOTIFY_ICON_INFOTIP_FLAGS dwInfoFlags = NOTIFY_ICON_INFOTIP_FLAGS.NIIF_NONE;

        internal Guid guidItem = default;

        internal IntPtr hBalloonIcon = 0;

        [StructLayout(LayoutKind.Explicit)]
        internal struct Union
        {
            [FieldOffset(0)]
            internal uint uTimeout;

            [FieldOffset(0)]
            internal uint uVersion;
        }
    }

    [Flags]
    internal enum NOTIFY_ICON_DATA_FLAGS : uint
    {
        NIF_MESSAGE = 0x00000001,
        NIF_ICON = 0x00000002,
        NIF_TIP = 0x00000004,
        NIF_STATE = 0x00000008,
        NIF_INFO = 0x00000010,
        NIF_GUID = 0x00000020,
        NIF_REALTIME = 0x00000040,
        NIF_SHOWTIP = 0x00000080,
    }

    [Flags]
    internal enum NOTIFY_ICON_STATE : uint
    {
        NIS_HIDDEN = 0x00000001,
        NIS_SHAREDICON = 0x00000002,
    }

    [Flags]
    internal enum NOTIFY_ICON_INFOTIP_FLAGS : uint
    {
        NIIF_NONE = 0x00000000,
        NIIF_INFO = 0x00000001,
        NIIF_WARNING = 0x00000002,
        NIIF_ERROR = 0x00000003,
        NIIF_USER = 0x00000004,
        NIIF_ICON_MASK = 0x0000000F,
        NIIF_NOSOUND = 0x00000010,
        NIIF_LARGE_ICON = 0x00000020,
        NIIF_RESPECT_QUIET_TIME = 0x00000080,
    }

    [Flags]
    internal enum TRACK_POPUP_MENU_FLAGS : uint
    {
        TPM_LEFTBUTTON = 0x00000000,
        TPM_RIGHTBUTTON = 0x00000002,
        TPM_LEFTALIGN = 0x00000000,
        TPM_CENTERALIGN = 0x00000004,
        TPM_RIGHTALIGN = 0x00000008,
        TPM_TOPALIGN = 0x00000000,
        TPM_VCENTERALIGN = 0x00000010,
        TPM_BOTTOMALIGN = 0x00000020,
        TPM_HORIZONTAL = 0x00000000,
        TPM_VERTICAL = 0x00000040,
        TPM_NONOTIFY = 0x00000080,
        TPM_RETURNCMD = 0x00000100,
        TPM_RECURSE = 0x00000001,
        TPM_HORPOSANIMATION = 0x00000400,
        TPM_HORNEGANIMATION = 0x00000800,
        TPM_VERPOSANIMATION = 0x00001000,
        TPM_VERNEGANIMATION = 0x00002000,
        TPM_NOANIMATION = 0x00004000,
        TPM_LAYOUTRTL = 0x00008000,
        TPM_WORKAREA = 0x00010000,
    }

    internal struct DRAWITEMSTRUCT()
    {
        internal DRAWITEMSTRUCT_CTL_TYPE CtlType = 0;

        internal uint CtlID = 0;

        internal uint itemID = 0;

        internal ODA_FLAGS itemAction = 0;

        internal ODS_FLAGS itemState = 0;

        internal IntPtr hwndItem = 0;

        internal IntPtr hDC = 0;

        internal RECT rcItem = default;

        internal nuint itemData = 0;
    }

    internal enum ODA_FLAGS : uint
    {
        ODA_DRAWENTIRE = 1U,
        ODA_SELECT = 2U,
        ODA_FOCUS = 4U,
    }

    internal enum ODS_FLAGS : uint
    {
        ODS_SELECTED = 1U,
        ODS_GRAYED = 2U,
        ODS_DISABLED = 4U,
        ODS_CHECKED = 8U,
        ODS_FOCUS = 16U,
        ODS_DEFAULT = 32U,
        ODS_COMBOBOXEDIT = 4096U,
        ODS_HOTLIGHT = 64U,
        ODS_INACTIVE = 128U,
        ODS_NOACCEL = 256U,
        ODS_NOFOCUSRECT = 512U,
    }

    internal struct MEASUREITEMSTRUCT()
    {
        internal DRAWITEMSTRUCT_CTL_TYPE CtlType = 0;

        internal uint CtlID = 0;

        internal uint itemID = 0;

        internal uint itemWidth = 0;

        internal uint itemHeight = 0;

        internal nuint itemData = 0;
    }

    internal enum DRAWITEMSTRUCT_CTL_TYPE : uint
    {
        ODT_BUTTON = 4U,
        ODT_COMBOBOX = 3U,
        ODT_LISTBOX = 2U,
        ODT_LISTVIEW = 102U,
        ODT_MENU = 1U,
        ODT_STATIC = 5U,
        ODT_TAB = 101U,
    }

    [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
    internal struct __char_128
    {
        private const int SpanLength = 128;

        internal static int Length => SpanLength;

        internal unsafe fixed char Value[SpanLength];

        [UnscopedRef]
        internal unsafe ref char this[int index] => ref Value[index];

        [UnscopedRef]
        internal unsafe Span<char> AsSpan() => MemoryMarshal.CreateSpan(ref Value[0], SpanLength);

        [UnscopedRef]
        internal readonly unsafe ReadOnlySpan<char> AsReadOnlySpan() => MemoryMarshal.CreateReadOnlySpan(ref Unsafe.AsRef(in Value[0]), SpanLength);

        internal readonly bool Equals(ReadOnlySpan<char> value) => value.Length == SpanLength ? AsReadOnlySpan().SequenceEqual(value) : AsReadOnlySpan().SliceAtNull().SequenceEqual(value);

        internal readonly bool Equals(string value) => Equals(value.AsSpan());

        internal readonly string ToString(int length) => AsReadOnlySpan()[..length].ToString();

        public readonly override string ToString() => AsReadOnlySpan().SliceAtNull().ToString();

        public static implicit operator __char_128(string value) => value.AsSpan();

        public static implicit operator __char_128(ReadOnlySpan<char> value)
        {
            Unsafe.SkipInit(out __char_128 result);
            value.CopyTo(result.AsSpan());
            var initLength = value.Length;
            result.AsSpan()[initLength..(SpanLength - initLength)].Clear();
            return result;
        }
    }

    [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
    internal struct __char_256
    {
        private const int SpanLength = 256;

        internal static int Length => SpanLength;

        internal unsafe fixed char Value[SpanLength];

        [UnscopedRef]
        internal unsafe ref char this[int index] => ref Value[index];

        [UnscopedRef]
        internal unsafe Span<char> AsSpan() => MemoryMarshal.CreateSpan(ref Value[0], SpanLength);

        [UnscopedRef]
        internal readonly unsafe ReadOnlySpan<char> AsReadOnlySpan() => MemoryMarshal.CreateReadOnlySpan(ref Unsafe.AsRef(in Value[0]), SpanLength);

        internal readonly bool Equals(ReadOnlySpan<char> value) => value.Length == SpanLength ? AsReadOnlySpan().SequenceEqual(value) : AsReadOnlySpan().SliceAtNull().SequenceEqual(value);

        internal readonly bool Equals(string value) => Equals(value.AsSpan());

        internal readonly string ToString(int length) => AsReadOnlySpan()[..length].ToString();

        public readonly override string ToString() => AsReadOnlySpan().SliceAtNull().ToString();

        public static implicit operator __char_256(string value) => value.AsSpan();

        public static implicit operator __char_256(ReadOnlySpan<char> value)
        {
            Unsafe.SkipInit(out __char_256 result);
            value.CopyTo(result.AsSpan());
            var initLength = value.Length;
            result.AsSpan()[initLength..(SpanLength - initLength)].Clear();
            return result;
        }
    }

    [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
    internal struct __char_64
    {
        private const int SpanLength = 64;

        internal static int Length => SpanLength;

        internal unsafe fixed char Value[SpanLength];

        [UnscopedRef]
        internal unsafe ref char this[int index] => ref Value[index];

        [UnscopedRef]
        internal unsafe Span<char> AsSpan() => MemoryMarshal.CreateSpan(ref Value[0], SpanLength);

        [UnscopedRef]
        internal readonly unsafe ReadOnlySpan<char> AsReadOnlySpan() => MemoryMarshal.CreateReadOnlySpan(ref Unsafe.AsRef(in Value[0]), SpanLength);

        internal readonly bool Equals(ReadOnlySpan<char> value) => value.Length == SpanLength ? AsReadOnlySpan().SequenceEqual(value) : AsReadOnlySpan().SliceAtNull().SequenceEqual(value);

        internal readonly bool Equals(string value) => Equals(value.AsSpan());

        internal readonly string ToString(int length) => AsReadOnlySpan()[..length].ToString();

        public readonly override string ToString() => AsReadOnlySpan().SliceAtNull().ToString();


        public static implicit operator __char_64(string value) => value.AsSpan();


        public static implicit operator __char_64(ReadOnlySpan<char> value)
        {
            Unsafe.SkipInit(out __char_64 result);
            value.CopyTo(result.AsSpan());
            var initLength = value.Length;
            result.AsSpan()[initLength..(SpanLength - initLength)].Clear();
            return result;
        }
    }

    internal struct MENUITEMINFOW
    {
        internal uint cbSize;

        internal MENU_ITEM_MASK fMask;

        internal MENU_ITEM_TYPE fType;

        internal MENU_ITEM_STATE fState;

        internal uint wID;

        internal IntPtr hSubMenu;

        internal IntPtr hbmpChecked;

        internal IntPtr hbmpUnchecked;

        internal nuint dwItemData;

        internal IntPtr dwTypeData;

        internal uint cch;

        internal IntPtr hbmpItem;
    }

    [Flags]
    internal enum MENU_ITEM_MASK : uint
    {
        MIIM_BITMAP = 0x00000080,
        MIIM_CHECKMARKS = 0x00000008,
        MIIM_DATA = 0x00000020,
        MIIM_FTYPE = 0x00000100,
        MIIM_ID = 0x00000002,
        MIIM_STATE = 0x00000001,
        MIIM_STRING = 0x00000040,
        MIIM_SUBMENU = 0x00000004,
        MIIM_TYPE = 0x00000010,
    }

    [Flags]
    internal enum MENU_ITEM_TYPE : uint
    {
        MFT_BITMAP = 0x00000004,
        MFT_MENUBARBREAK = 0x00000020,
        MFT_MENUBREAK = 0x00000040,
        MFT_OWNERDRAW = 0x00000100,
        MFT_RADIOCHECK = 0x00000200,
        MFT_RIGHTJUSTIFY = 0x00004000,
        MFT_RIGHTORDER = 0x00002000,
        MFT_SEPARATOR = 0x00000800,
        MFT_STRING = 0x00000000,
    }

    [Flags]
    internal enum MENU_ITEM_STATE : uint
    {
        MFS_GRAYED = 0x00000003,
        MFS_DISABLED = 0x00000003,
        MFS_CHECKED = 0x00000008,
        MFS_HILITE = 0x00000080,
        MFS_ENABLED = 0x00000000,
        MFS_UNCHECKED = 0x00000000,
        MFS_UNHILITE = 0x00000000,
        MFS_DEFAULT = 0x00001000,
    }

    [Flags]
    internal enum TaskBarProgressState : uint
    {
        NoProgress = 0x00000000,
        Indeterminate = 0x00000001,
        Normal = 0x00000002,
        Error = 0x00000004,
        Paused = 0x00000008
    }
}

internal static class InlineArrayIndexerExtensions
{
    internal static ReadOnlySpan<char> SliceAtNull(this ReadOnlySpan<char> value)
    {
        var length = value.IndexOf('\0');
        return length < 0 ? value : value[..length];
    }
}