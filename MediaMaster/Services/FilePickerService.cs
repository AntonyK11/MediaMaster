using Microsoft.WindowsAPICodePack.Dialogs;
using System.Reflection;
using WinUIEx;

namespace MediaMaster.Services;

public static class FilePickerService
{
    public static (CommonFileDialogResult, string?) OpenFilePicker(string? fileName = null, bool isFolderPicker = false)
    {
        // Cannot use System.Windows.Forms.OpenFileDialog because it makes the app crash if the window is closed after the dialog in certain situations
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

            // Use reflection to set the _parentWindow handle without needing to include PresentationFrameWork
            FieldInfo? fi = typeof(CommonFileDialog).GetField("_parentWindow", BindingFlags.NonPublic | BindingFlags.Instance);
            if (fi != null && App.MainWindow != null)
            {
                var hwnd = App.MainWindow.GetWindowHandle();
                fi.SetValue(dialog, hwnd);
            }

            var result = dialog.ShowDialog();
            if (result == CommonFileDialogResult.Ok)
            {
                return (result, dialog.FileName);
            }
            return (result, null);
        }
    }

    public static (CommonFileDialogResult, string?) SaveFilePicker(string? fileName = null, string? defaultExtension = null, CommonFileDialogFilter? filter = null)
    {
        // Cannot use System.Windows.Forms.SaveFileDialog because it makes the app crash if the window is closed after the dialog in certain situations
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

            // Use reflection to set the _parentWindow handle without needing to include PresentationFrameWork
            FieldInfo? fi = typeof(CommonFileDialog).GetField("_parentWindow", BindingFlags.NonPublic | BindingFlags.Instance);
            if (fi != null && App.MainWindow != null)
            {
                var hwnd = App.MainWindow.GetWindowHandle();
                fi.SetValue(dialog, hwnd);
            }

            var result = dialog.ShowDialog();
            if (result == CommonFileDialogResult.Ok)
            {
                return (result, dialog.FileName);
            }
            return (result, null);
        }
    }
}

