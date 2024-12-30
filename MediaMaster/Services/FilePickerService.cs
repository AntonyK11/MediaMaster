using Microsoft.WindowsAPICodePack.Dialogs;
using WinUIEx;

namespace MediaMaster.Services;

public static class FilePickerService
{
    public static (CommonFileDialogResult, string?) OpenFilePicker(string? fileName = null, bool isFolderPicker = false)
    {
        using (CommonOpenFileDialog dialog = new())
        {
            if (fileName != null)
            {
                dialog.InitialDirectory = Path.GetDirectoryName(fileName);
                dialog.DefaultFileName = Path.GetFileNameWithoutExtension(fileName);
            }
            dialog.EnsureFileExists = true;
            dialog.EnsurePathExists = true;
            dialog.ShowHiddenItems = true;
            dialog.NavigateToShortcut = false;
            dialog.IsFolderPicker = isFolderPicker;

            var hwnd = IntPtr.Zero;
            if (App.MainWindow != null)
            {
                hwnd = App.MainWindow.GetWindowHandle();
            }

            var result = dialog.ShowDialog(hwnd);
            if (result == CommonFileDialogResult.Ok)
            {
                return (result, dialog.FileName);
            }
            return (result, null);
        }
    }

    public static (CommonFileDialogResult, string?) SaveFilePicker(string? fileName = null, string? defaultExtension = null, CommonFileDialogFilter? filter = null)
    {
        using (CommonSaveFileDialog dialog = new())
        {
            if (fileName != null)
            {
                dialog.DefaultFileName = fileName;
            }
            dialog.EnsureFileExists = true;
            dialog.EnsurePathExists = true;
            dialog.ShowHiddenItems = true;
            dialog.NavigateToShortcut = false;
            dialog.IsExpandedMode = true;
            if (filter != null)
            {
                dialog.DefaultExtension = defaultExtension;
            }
            if (filter != null)
            {
                dialog.Filters.Add(filter);
            }

            var hwnd = IntPtr.Zero;
            if (App.MainWindow != null)
            {
                hwnd = App.MainWindow.GetWindowHandle();
            }

            var result = dialog.ShowDialog(hwnd);
            if (result == CommonFileDialogResult.Ok)
            {
                return (result, dialog.FileName);
            }
            return (result, null);
        }
    }
}

