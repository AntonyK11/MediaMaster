using System.Runtime.InteropServices;
using System.Runtime.InteropServices.Marshalling;
using static MediaMaster.WIn32.WindowsNativeValues;

namespace MediaMaster.WIn32;

public static partial class WindowsNativeInterfaces
{
    [GeneratedComInterface]
    [Guid("46EB5926-582E-4017-9FDF-E8998DAA0950")]
    [InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
    public partial interface IImageList
    {
        [PreserveSig]
        HResult Add(
            IntPtr hbmImage,
            IntPtr hbmMask,
            out int pi);

        [PreserveSig]
        HResult ReplaceIcon(
            int i,
            IntPtr hicon,
            out int pi);

        [PreserveSig]
        HResult SetOverlayImage(
            int iImage,
            int iOverlay);

        [PreserveSig]
        HResult Replace(
            int i,
            IntPtr hbmImage,
            IntPtr hbmMask);

        [PreserveSig]
        HResult AddMasked(
            IntPtr hbmImage,
            int crMask,
            out int pi);

        [PreserveSig]
        HResult Draw(
            ref ImageListDrawParams pimldp);

        [PreserveSig]
        HResult Remove(
            int i);

        [PreserveSig]
        HResult GetIcon(
            int i,
            int flags,
            out IntPtr picon);
    }

	[GeneratedComInterface]
	[InterfaceType( ComInterfaceType.InterfaceIsIUnknown )]
	[Guid( "43826d1e-e718-42ee-bc55-a1e261c37bfe" )]
    public partial interface IShellItem
	{
		void BindToHandler(
            IntPtr pbc,
            Guid bhid,
            Guid riid,
			out IntPtr ppv );

		void GetParent(
            [MarshalAs(UnmanagedType.Interface)] out IShellItem ppsi);

		void GetDisplayName(
            SIGDN sigdnName,
            out IntPtr ppszName);

		void GetAttributes(
            uint sfgaoMask,
            out uint psfgaoAttribs);

		void Compare(
            [MarshalAs(UnmanagedType.Interface)] IShellItem psi,
            uint hint,
            out int piOrder);
	}

    [GeneratedComInterface]
    [Guid("bcc18b79-ba16-442f-80c4-8a59c30c463b")]
    [InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
    public partial interface IShellItemImageFactory
    {
        [PreserveSig]
        HResult GetImage(
            Size size,
            SIIGBF flags,
            out IntPtr phbm );
    }

    [GeneratedComInterface]
    [Guid("B63EA76D-1F85-456F-A19C-48159EFA858B")]
    [InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
    public partial interface IShellItemArray
    {
        [PreserveSig]
        HResult BindToHandler(
            [MarshalAs(UnmanagedType.Interface)] IntPtr pbc,
            ref Guid rbhid,
            ref Guid riid,
            out IntPtr ppvOut);

        [PreserveSig]
        HResult GetPropertyStore(
            int Flags,
            ref Guid riid,
            out IntPtr ppv);

        [PreserveSig]
        HResult GetCount(out uint pdwNumItems);

        [PreserveSig]
        HResult GetItemAt(
            uint dwIndex,
            [MarshalAs(UnmanagedType.Interface)] out IShellItem? ppsi);

        // Not supported: IEnumShellItems (will use GetCount and GetItemAt instead).
        [PreserveSig]
        HResult EnumItems(
            [MarshalAs(UnmanagedType.Interface)] out IntPtr ppenumShellItems);
    }

    [GeneratedComInterface]
    [Guid("ea1afb91-9e28-4b86-90e9-9e9f8a5eefaf")]
    [InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
    internal partial interface ITaskbarList3
    {
        // ITaskbarList
        [PreserveSig]
        HResult HrInit();
        [PreserveSig]
        HResult AddTab(IntPtr hwnd);
        [PreserveSig]
        HResult DeleteTab(IntPtr hwnd);
        [PreserveSig]
        HResult ActivateTab(IntPtr hwnd);
        [PreserveSig]
        HResult SetActiveAlt(IntPtr hwnd);

        // ITaskbarList2
        [PreserveSig]
        HResult MarkFullscreenWindow(IntPtr hwnd, [MarshalAs(UnmanagedType.Bool)] bool fFullscreen);

        // ITaskbarList3
        [PreserveSig]
        HResult SetProgressValue(IntPtr hwnd, ulong ullCompleted, ulong ullTotal);
        [PreserveSig]
        HResult SetProgressState(IntPtr hwnd, TaskBarProgressState state);
    }

    [ComImport]
    [Guid("56fdf344-fd6d-11d0-958a-006097c9a090")]
    [ClassInterface(ClassInterfaceType.None)]
    internal class TaskbarInstance;
}