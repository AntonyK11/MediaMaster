using System.Reflection;
using MediaMaster.Controls;
using MediaMaster.DataBase.Models;
using Microsoft.UI.Xaml.Controls;
using Microsoft.WindowsAPICodePack.Dialogs;
using WinUIEx;
using HorizontalAlignment = Microsoft.UI.Xaml.HorizontalAlignment;

namespace MediaMaster.Services.MediaInfo;

public class MediaFilePath(StackPanel parent) : MediaInfoTextBlockBase(parent)
{
    public override string TranslationKey { get; set; } = "MediaFilePath";

    public override void Initialize(Media? media)
    {
        base.Initialize(media);

        if (EditableTextBlock == null || media == null) return;
        EditableTextBlock.Text = media.FilePath;
    }

    public override EditableTextBlock GetEditableTextBlock()
    {
        var editableTextBlock = new EditableTextBlock
        {
            HorizontalAlignment = HorizontalAlignment.Stretch,
            ConfirmOnReturn = false,
            EditOnClick = false,
            EditOnDoubleClick = false
        };
        editableTextBlock.EditButtonPressed += PathTextBox_OnEdit;
        return editableTextBlock;
    }

    private async void PathTextBox_OnEdit(EditableTextBlock sender, string args)
    {
        if (EditableTextBlock == null) return;
        //using (OpenFileDialog dialog = new())
        //{
        //    dialog.InitialDirectory = Path.GetDirectoryName(EditableTextBlock.Text);
        //    dialog.FileName = Path.GetFileName(EditableTextBlock.Text);
        //    dialog.CheckFileExists = true;

        //    var hwnd = App.MainWindow.GetWindowHandle();
        //    var window = NativeWindow.FromHandle(hwnd);
        //    if (dialog.ShowDialog(window) == DialogResult.OK)
        //    {
        //        var file = dialog.FileName;
        //        Debug.WriteLine(file);
        //    }
        //}
        
        //IFileOpenDialog d = (IFileOpenDialog)new FileOpenDialog();

        //var guid = typeof(IShellItem).GUID;
        //var result = SHCreateItemFromParsingName(Path.GetDirectoryName(EditableTextBlock.Text), IntPtr.Zero, ref guid, out IShellItem initialDirectoryShellItem);
        //if (result != HResult.Ok)
        //{
        //    Debug.WriteLine("Failed to create shell item from directory");
        //}
        //else
        //{
        //    d.SetDefaultFolder(initialDirectoryShellItem);
        //}
        //d.SetFileName(Path.GetFileName(EditableTextBlock.Text));

        //result = d.Show(App.MainWindow.GetWindowHandle());

        //if (result == HResult.Ok)
        //{
        //    d.GetResult(out IShellItem item);
        //    item.GetDisplayName(SIGDN.FileSystemPath, out var pszFilePath);
        //    var filePath = Marshal.PtrToStringAuto(pszFilePath);
        //    Marshal.FreeCoTaskMem(pszFilePath);

        //    Debug.WriteLine(filePath);
        //}

        using (CommonOpenFileDialog dialog = new())
        {
            dialog.InitialDirectory = Path.GetDirectoryName(EditableTextBlock.Text);
            dialog.DefaultFileName = Path.GetFileName(EditableTextBlock.Text);
            dialog.EnsureFileExists = true;
            dialog.EnsurePathExists = true;

            // Use reflection to set the _parentWindow handle without needing to include PresentationFrameWork
            // Cannot use System.Windows.Forms.OpenFileDialog because it makes the app crash if the window is closed after the dialog in certain situations
            FieldInfo? fi = typeof(CommonFileDialog).GetField("_parentWindow", BindingFlags.NonPublic | BindingFlags.Instance);
            if (fi != null && App.MainWindow != null)
            {
                var hwnd = App.MainWindow.GetWindowHandle();
                fi.SetValue(dialog, hwnd);
            }

            if (dialog.ShowDialog() == CommonFileDialogResult.Ok)
            {
                var file = dialog.FileName;
                Debug.WriteLine(file);
            }
        }
    }

    public override void UpdateMediaProperty(ref Media media, string text)
    {
        media.FilePath = text;
    }
}

