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

        // [PreserveSig]
        // HResult GetPropertyDescriptionList(
        //     [In] ref PropertyKey keyType,
        //     [In] ref Guid riid,
        //     out IntPtr ppv);
        //
        // [PreserveSig]
        // HResult GetAttributes(
        //     [In] ShellItemAttributeOptions dwAttribFlags,
        //     [In] ShellFileGetAttributesOptions sfgaoMask,
        //     out ShellFileGetAttributesOptions psfgaoAttribs);

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
    internal class TaskbarInstance
    {
    }

    //[GeneratedComInterface]
    //[Guid("B4DB1657-70D7-485E-8E3E-6FCB5A5C1802")]
    //[InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
    //public partial interface IModalWindow
    //{
    //    [PreserveSig]
    //    HResult Show(IntPtr parent);
    //}

    //[GeneratedComInterface]
    //[Guid("42F85136-DB7E-439C-85F1-E4075D135FC8")]
    //[InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
    //public partial interface IFileDialog
    //{
    //    // Implemented by IModalWindow
    //    [PreserveSig]
    //    HResult Show(IntPtr parent);

    //    // Implemented by IFileDialog
    //    // Cannot marshal rgFilterSpec so using IntPtr.
    //    void SetFileTypes(
    //        uint cFileTypes,
    //        IntPtr rgFilterSpec);

    //    void SetFileTypeIndex(
    //        uint iFileType);

    //    void GetFileTypeIndex(
    //        out uint piFileType);

    //    void Advise(
    //        [MarshalAs(UnmanagedType.Interface)] IFileDialogEvents? pfde,
    //        out uint pdwCookie);

    //    void Unadvise(
    //        uint dwCookie);

    //    void SetOptions(
    //        FileOpenOptions fos);

    //    void GetOptions(
    //        out FileOpenOptions pfos);

    //    void SetDefaultFolder(
    //        [MarshalAs(UnmanagedType.Interface)] IShellItem? psi);

    //    void SetFolder([
    //        MarshalAs(UnmanagedType.Interface)] IShellItem? psi);

    //    void GetFolder(
    //        [MarshalAs(UnmanagedType.Interface)] out IShellItem ppsi);

    //    void GetCurrentSelection(
    //        [MarshalAs(UnmanagedType.Interface)] out IShellItem ppsi);

    //    void SetFileName(
    //        [MarshalAs(UnmanagedType.LPWStr)] string? pszName);

    //    void GetFileName(
    //        [MarshalAs(UnmanagedType.LPWStr)] out string pszName);

    //    void SetTitle(
    //        [MarshalAs(UnmanagedType.LPWStr)] string? pszTitle);

    //    void SetOkButtonLabel(
    //        [MarshalAs(UnmanagedType.LPWStr)] string pszText);

    //    void SetFileNameLabel(
    //        [MarshalAs(UnmanagedType.LPWStr)] string pszLabel);

    //    void GetResult(
    //        [MarshalAs(UnmanagedType.Interface)] out IShellItem ppsi);

    //    void AddPlace(
    //        [MarshalAs(UnmanagedType.Interface)] IShellItem? psi,
    //        FileDialogAddPlacement fdap);

    //    void SetDefaultExtension(
    //        [MarshalAs(UnmanagedType.LPWStr)] string? pszDefaultExtension);

    //    void Close(
    //        [MarshalAs(UnmanagedType.Error)] int hr);

    //    void SetClientGuid(
    //        ref Guid guid);

    //    void ClearClientData();

    //    // Not supported:  IShellItemFilter is not defined, converting to IntPtr.
    //    void SetFilter(
    //        IntPtr pFilter);
    //}

    //[GeneratedComInterface]
    //[Guid("D57C7288-D4AD-4768-BE02-9D969532D960")]
    //[InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
    //public partial interface IFileOpenDialog
    //{
    //    // Implemented by IModalWindow
    //    [PreserveSig]
    //    HResult Show(IntPtr parent);

    //    // Implemented by IFileDialog
    //    // Cannot marshal rgFilterSpec so using IntPtr.
    //    void SetFileTypes(
    //        uint cFileTypes,
    //        IntPtr rgFilterSpec);

    //    void SetFileTypeIndex(
    //        uint iFileType);

    //    void GetFileTypeIndex(
    //        out uint piFileType);

    //    void Advise(
    //        [MarshalAs(UnmanagedType.Interface)] IFileDialogEvents? pfde,
    //        out uint pdwCookie);

    //    void Unadvise(
    //        uint dwCookie);

    //    void SetOptions(
    //        FileOpenOptions fos);

    //    void GetOptions(
    //        out FileOpenOptions pfos);

    //    void SetDefaultFolder(
    //        [MarshalAs(UnmanagedType.Interface)] IShellItem? psi);

    //    void SetFolder([
    //        MarshalAs(UnmanagedType.Interface)] IShellItem? psi);

    //    void GetFolder(
    //        [MarshalAs(UnmanagedType.Interface)] out IShellItem ppsi);

    //    void GetCurrentSelection(
    //        [MarshalAs(UnmanagedType.Interface)] out IShellItem ppsi);

    //    void SetFileName(
    //        [MarshalAs(UnmanagedType.LPWStr)] string? pszName);

    //    void GetFileName(
    //        [MarshalAs(UnmanagedType.LPWStr)] out string pszName);

    //    void SetTitle(
    //        [MarshalAs(UnmanagedType.LPWStr)] string? pszTitle);

    //    void SetOkButtonLabel(
    //        [MarshalAs(UnmanagedType.LPWStr)] string pszText);

    //    void SetFileNameLabel(
    //        [MarshalAs(UnmanagedType.LPWStr)] string pszLabel);

    //    void GetResult(
    //        [MarshalAs(UnmanagedType.Interface)] out IShellItem ppsi);

    //    void AddPlace(
    //        [MarshalAs(UnmanagedType.Interface)] IShellItem? psi,
    //        FileDialogAddPlacement fdap);

    //    void SetDefaultExtension(
    //        [MarshalAs(UnmanagedType.LPWStr)] string? pszDefaultExtension);

    //    void Close(
    //        [MarshalAs(UnmanagedType.Error)] int hr);

    //    void SetClientGuid(
    //        ref Guid guid);

    //    void ClearClientData();

    //    // Not supported:  IShellItemFilter is not defined, converting to IntPtr.
    //    void SetFilter(
    //        IntPtr pFilter);

    //    //Implemented by IFileOpenDialog
    //    void GetResults(
    //        [MarshalAs(UnmanagedType.Interface)]
    //        out IShellItemArray ppenum);

    //    void GetSelectedItems(
    //        [MarshalAs(UnmanagedType.Interface)]
    //        out IShellItemArray ppsai);
    //}

    //[ComImport]
    //[Guid("DC1C5A9C-E88A-4DDE-A5A1-60F82A20AEF7")]
    //[ClassInterface(ClassInterfaceType.None)]
    //public class FileOpenDialog
    //{
    //}


    //[GeneratedComInterface]
    //[Guid("973510DB-7D7F-452B-8975-74A85828D354")]
    //[InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
    //public partial interface IFileDialogEvents
    //{
    //    [PreserveSig]
    //    HResult OnFileOk(
    //        [MarshalAs(UnmanagedType.Interface)] IFileDialog pfd);

    //    [PreserveSig]
    //    HResult OnFolderChanging(
    //        [MarshalAs(UnmanagedType.Interface)] IFileDialog pfd,
    //        [MarshalAs(UnmanagedType.Interface)] IShellItem? psiFolder);

    //    void OnFolderChange(
    //        [MarshalAs(UnmanagedType.Interface)] IFileDialog pfd);

    //    void OnSelectionChange(
    //        [MarshalAs(UnmanagedType.Interface)] IFileDialog pfd);

    //    void OnShareViolation(
    //        [MarshalAs(UnmanagedType.Interface)] IFileDialog pfd,
    //        [MarshalAs(UnmanagedType.Interface)] IShellItem psi,
    //        out FileDialogEventShareViolationResponse pResponse);

    //    void OnTypeChange(
    //        [MarshalAs(UnmanagedType.Interface)] IFileDialog pfd);

    //    void OnOverwrite(
    //        [MarshalAs(UnmanagedType.Interface)] IFileDialog pfd,
    //        [MarshalAs(UnmanagedType.Interface)] IShellItem psi,
    //        out FileDialogEventOverwriteResponse pResponse);
    //}
}