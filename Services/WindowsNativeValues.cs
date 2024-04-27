using System.Drawing;
using System.Runtime.InteropServices;

namespace MediaMaster.Services;

public static class WindowsNativeValues
{
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

    public const int
        WM_INITMENU = 0x0116,
        WM_SYSCOMMAND = 0x0112,
        TPM_RETURNCMD = 0x0100,
        WM_NCMOUSELEAVE = 0x02A2,
        WM_NCHITTEST = 0x0084;

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
}