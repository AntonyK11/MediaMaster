using System.Runtime.InteropServices;

namespace MediaMaster.Services;

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
        NcRenderingEnabled,
        NcRenderingPolicy,
        TransitionsForceDisabled,
        DWMWA_NCRENDERING_ENABLED,
        DWMWA_NCRENDERING_POLICY,
        DWMWA_TRANSITIONS_FORCEDISABLED,
        DWMWA_ALLOW_NCPAINT,
        DWMWA_CAPTION_BUTTON_BOUNDS,
        DWMWA_NONCLIENT_RTL_LAYOUT,
        DWMWA_FORCE_ICONIC_REPRESENTATION,
        DWMWA_FLIP3D_POLICY,
        DWMWA_EXTENDED_FRAME_BOUNDS,
        DWMWA_HAS_ICONIC_BITMAP,
        DWMWA_DISALLOW_PEEK,
        DWMWA_EXCLUDED_FROM_PEEK,
        DWMWA_CLOAK,
        DWMWA_CLOAKED,
        DWMWA_FREEZE_REPRESENTATION,
        DWMWA_PASSIVE_UPDATE_MODE,
        DWMWA_USE_HOSTBACKDROPBRUSH,
        UseImmersiveDarkMode  = 20,
        DWMWA_WINDOW_CORNER_PREFERENCE = 33,
        DWMWA_BORDER_COLOR,
        DWMWA_CAPTION_COLOR,
        DWMWA_TEXT_COLOR,
        DWMWA_VISIBLE_FRAME_BORDER_THICKNESS,
        SystemBackdropType,
        DWMWA_LAST,
        SystemBackdropTypeDeprecated = 1029
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

    // [StructLayout(LayoutKind.Sequential)]
    // public struct Point
    // {
    //     public int x;
    //     
    //     public int y;
    //
    //     //public static implicit operator Point(POINT point)
    //     //{
    //     //    return new Point(point.x, point.y);
    //     //}
    //
    //     //public static implicit operator POINT(Point point)
    //     //{
    //     //    return new POINT { x = point.X, y = point.Y };
    //     //}
    // }

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

    //[Flags]
    //public enum FileOpenOptions : uint
    //{
    //    None = 0x0,
    //    OverwritePrompt = 0x2,
    //    StrictFileTypes = 0x4,
    //    NoChangeDir = 0x8,
    //    PickFolders = 0x20,
    //    ForceFileSystem = 0x40,
    //    AllNonStorageItems = 0x80,
    //    NoValidate = 0x100,
    //    AllowMultiSelect = 0x200,
    //    PathMustExist = 0x800,
    //    FileMustExist = 0x1000,
    //    CreatePrompt = 0x2000,
    //    ShareAware = 0x4000,
    //    NoReadOnlyReturn = 0x8000,
    //    NoTestFileCreate = 0x10000,
    //    HideMruPlaces = 0x20000,
    //    HidePinnedPlaces = 0x40000,
    //    NoDereferenceLinks = 0x100000,
    //    OkButtonNeedsInteraction = 0x200000,
    //    DontAddToRecent = 0x2000000,
    //    ForceShowHidden = 0x10000000,
    //    DefaultNoMiniMode = 0x20000000,
    //    ForcePreviewPaneOn = 0x40000000,
    //    SupportStreamableItems = 0x80000000
    //}

    //[StructLayout(LayoutKind.Sequential)]
    //public struct FilterSpec
    //{
    //    [MarshalAs(UnmanagedType.LPWStr)] public string pszName;
    //    [MarshalAs(UnmanagedType.LPWStr)] public string pszSpec;
    //}

    //public enum FileDialogAddPlacement
    //{
    //    Bottom = 0,
    //    Top = 1
    //}

    //public enum FileDialogEventOverwriteResponse
    //{
    //    Default = 0,
    //    Accept = 1,
    //    Refuse = 2
    //}

    //public enum FileDialogEventShareViolationResponse
    //{
    //    Default = 0,
    //    Accept = 1,
    //    Refuse = 2
    //}
}