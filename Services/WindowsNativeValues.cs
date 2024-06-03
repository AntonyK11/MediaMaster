using CommunityToolkit.WinUI.Animations;
using System.Drawing;
using System.Runtime.InteropServices;
using System.Runtime.InteropServices.Marshalling;

namespace MediaMaster.Services;

public static partial class WindowsNativeValues
{
    public const string IShellItem2Guid = "7E9FB0D3-919F-4307-AB2E-9B1860310C93";

    public const int
        WM_INITMENU = 0x0116,
        WM_SYSCOMMAND = 0x0112,
        TPM_RETURNCMD = 0x0100,
        WM_NCMOUSELEAVE = 0x02A2,
        WM_NCHITTEST = 0x0084,

        SHGFI_SYSICONINDEX = 0x000004000,
        SHGFI_LARGEICON = 0x000000000,

        SHIL_SMALL = 0x1,
        SHIL_SYSSMALL = 0x3,
        SHIL_LARGE = 0x0,
        SHIL_EXTRALARGE = 0x2,
        SHIL_JUMBO = 0x4,

        ILD_TRANSPARENT = 0x00000001,
        ILD_ASYNC = 0x00008000;

    public enum ACCENT_STATE
    {
        ACCENT_DISABLED,
        ACCENT_ENABLE_GRADIENT,
        ACCENT_ENABLE_TRANSPARENTGRADIENT,
        ACCENT_ENABLE_BLURBEHIND, // Aero effect
        ACCENT_ENABLE_ACRYLICBLURBEHIND, // Acrylic effect
        ACCENT_ENABLE_HOSTBACKDROP, // Mica effect
        ACCENT_INVALID_STATE
    }

    public enum DWM_SYSTEMBACKDROP_TYPE
    {
        DWMSBT_AUTO,
        DWMSBT_NONE,
        DWMSBT_MAINWINDOW,
        DWMSBT_TRANSIENTWINDOW,
        DWMSBT_TABBEDWINDOW
    }

    public enum DWMWINDOWATTRIBUTE
    {
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
        DWMWA_USE_IMMERSIVE_DARK_MODE = 20,
        DWMWA_WINDOW_CORNER_PREFERENCE = 33,
        DWMWA_BORDER_COLOR,
        DWMWA_CAPTION_COLOR,
        DWMWA_TEXT_COLOR,
        DWMWA_VISIBLE_FRAME_BORDER_THICKNESS,
        DWMWA_SYSTEMBACKDROP_TYPE,
        DWMWA_LAST,
        DWMWA_SYSTEMBACKDROP_TYPE_DEPRECATED = 1029
    }

    public enum PreferredAppMode
    {
        Default,
        AllowDark,
        ForceDark,
        ForceLight,
        Max
    }

    public enum WINDOWCOMPOSITIONATTRIB
    {
        WCA_ACCENT_POLICY = 19,
        WCA_USEDARKMODECOLORS = 26
    }

    public enum DWM_WINDOW_CORNER_PREFERENCE
    {
        DWMWCP_DEFAULT = 0,
        DWMWCP_DONOTROUND = 1,
        DWMWCP_ROUND = 2,
        DWMWCP_ROUNDSMALL = 3
    }

    [StructLayout(LayoutKind.Sequential)]
    public struct MARGINS
    {
        public int cxLeftWidth;
        public int cxRightWidth;
        public int cyTopHeight;
        public int cyBottomHeight;
    }

    [StructLayout(LayoutKind.Sequential)]
    public struct WINDOWCOMPOSITIONATTRIBDATA
    {
        public WINDOWCOMPOSITIONATTRIB Attribute;
        public IntPtr Data;
        public int SizeOfData;
    }

    public struct POINT
    {
        /// <summary>
        ///     The x-coordinate of the point.
        /// </summary>
        public int x;

        /// <summary>
        ///     The x-coordinate of the point.
        /// </summary>
        public int y;

#if !UAP10_0
        public static implicit operator Point(POINT point)
        {
            return new Point(point.x, point.y);
        }

        public static implicit operator POINT(Point point)
        {
            return new POINT { x = point.X, y = point.Y };
        }
#endif
    }

    [GeneratedComInterface]
    [Guid("46EB5926-582E-4017-9FDF-E8998DAA0950")]
    [InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
    public partial interface IImageList
    {
        [PreserveSig]
        int Add(
            IntPtr hbmImage,
            IntPtr hbmMask,
            out int pi);

        [PreserveSig]
        int ReplaceIcon(
            int i,
            IntPtr hicon,
            out int pi);

        [PreserveSig]
        int SetOverlayImage(
            int iImage,
            int iOverlay);

        [PreserveSig]
        int Replace(
            int i,
            IntPtr hbmImage,
            IntPtr hbmMask);

        [PreserveSig]
        int AddMasked(
            IntPtr hbmImage,
            int crMask,
            out int pi);

        [PreserveSig]
        int Draw(
            ref IMAGELISTDRAWPARAMS pimldp);

        [PreserveSig]
        int Remove(
            int i);

        [PreserveSig]
        int GetIcon(
            int i,
            int flags,
            out IntPtr picon);
    }

    [StructLayout(LayoutKind.Sequential)]
    public struct IMAGELISTDRAWPARAMS
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
    public struct SHFILEINFO
    {
        public IntPtr hIcon;
        public int iIcon;
        public uint dwAttributes;
        [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 260)]
        public string szDisplayName;
        [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 80)]
        public string szTypeName;
    }

	[GeneratedComInterface]
	[InterfaceType( ComInterfaceType.InterfaceIsIUnknown )]
	[Guid( "43826d1e-e718-42ee-bc55-a1e261c37bfe" )]
	internal partial interface IShellItem
	{
		void BindToHandler(
            IntPtr pbc,
			Guid bhid,
			Guid riid,
			out IntPtr ppv );

		void GetParent( out IShellItem ppsi );
		void GetDisplayName( SIGDN sigdnName, out IntPtr ppszName );
		void GetAttributes( uint sfgaoMask, out uint psfgaoAttribs );
		void Compare( IShellItem psi, uint hint, out int piOrder );
	}

    [GeneratedComInterface]
    [Guid("bcc18b79-ba16-442f-80c4-8a59c30c463b")]
    [InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
    public partial interface IShellItemImageFactory
    {
        [PreserveSig]
        HResult GetImage(
            SIZE size,
            SIIGBF flags,
            out IntPtr phbm );
    }

    [StructLayout(LayoutKind.Sequential)]
    public struct SIZE(int cx, int cy)
    {
        public int cx = cx;
        public int cy = cy;
    }

    [Flags]
    public enum SIIGBF
    {
        SIIGBF_RESIZETOFIT = 0x000,
        SIIGBF_BIGGERSIZEOK = 0x001,
        SIIGBF_MEMORYONLY = 0x002,
        SIIGBF_ICONONLY = 0x004,
        SIIGBF_THUMBNAILONLY = 0x008,
        SIIGBF_INCACHEONLY = 0x010,
        SIIGBF_ICONBACKGROUND = 0x080,
        SIIGBF_SCALEUP = 0x100
    }

    [Flags]
    public enum SIGDN : uint
    {
        NORMALDISPLAY = 0,
        PARENTRELATIVEPARSING = 0x80018001,
        PARENTRELATIVEFORADDRESSBAR = 0x8001c001,
        DESKTOPABSOLUTEPARSING = 0x80028000,
        PARENTRELATIVEEDITING = 0x80031001,
        DESKTOPABSOLUTEEDITING = 0x8004c000,
        FILESYSPATH = 0x80058000,
        URL = 0x80068000
    }

    public enum HResult
    {
        Ok = 0x0000,
        False = 0x0001,
        InvalidArguments = unchecked((int)0x80070057),
        OutOfMemory = unchecked((int)0x8007000E),
        NoInterface = unchecked((int)0x80004002),
        Fail = unchecked((int)0x80004005),
        ElementNotFound = unchecked((int)0x80070490),
        TypeElementNotFound = unchecked((int)0x8002802B),
        NoObject = unchecked((int)0x800401E5),
        Win32ErrorCanceled = 1223,
        Canceled = unchecked((int)0x800704C7),
        ResourceInUse = unchecked((int)0x800700AA),
        AccessDenied = unchecked((int)0x80030005)
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
        public int bV5Size;
        public int bV5Width;
        public int bV5Height;
        public short bV5Planes;
        public short bV5BitCount;
        public int bV5Compression;
        public int bV5SizeImage;
        public int bV5XPelsPerMeter;
        public int bV5YPelsPerMeter;
        public int bV5ClrUsed;
        public int bV5ClrImportant;
        public int bV5RedMask;
        public int bV5GreenMask;
        public int bV5BlueMask;
        public int bV5AlphaMask;
        public int bV5CSType;
        public int bV5Endpoints1;
        public int bV5Endpoints2;
        public int bV5Endpoints3;
        public int bV5GammaRed;
        public int bV5GammaGreen;
        public int bV5GammaBlue;
        public int bV5Intent;
        public int bV5ProfileData;
        public int bV5ProfileSize;
        public int bV5Reserved;
    }

    public const int BI_BITFIELDS = 3;
    public const int DIB_RGB_COLORS = 0;
}