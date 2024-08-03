using System.Reflection;
using MediaMaster.Controls;
using MediaMaster.DataBase;
using MediaMaster.DataBase.Models;
using MediaMaster.Extensions;
using Microsoft.UI.Xaml.Controls;
using Microsoft.WindowsAPICodePack.Dialogs;
using WinUIEx;
using HorizontalAlignment = Microsoft.UI.Xaml.HorizontalAlignment;

namespace MediaMaster.Services.MediaInfo;

public class MediaFilePath(StackPanel parent) : MediaInfoTextBlockBase(parent)
{
    public override string TranslationKey { get; set; } = "MediaFilePath";

    public override void UpdateControlContent()
    {
        if (EditableTextBlock == null || Media == null) return;
        EditableTextBlock.Text = Media.Uri;
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

    private void PathTextBox_OnEdit(EditableTextBlock sender, string args)
    {
        if (EditableTextBlock == null) return;

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

    public override void UpdateMediaProperty(Media media, string text)
    {
        media.Uri = text;
    }

    public override bool ShowInfo(Media? media, bool isCompact)
    {
        return media == null || !(isCompact || media.Uri.IsWebsite());
    }

    public override void InvokeMediaChange(Media media)
    {
        if (Media == null) return;
        MediaDbContext.InvokeMediaChange(MediaChangeFlags.MediaChanged | MediaChangeFlags.UriChanged, Media);
    }

    public override void MediaChanged(MediaChangeArgs args)
    {
        if (Media == null || args.Media.MediaId != Media.MediaId || !args.Flags.HasFlag(MediaChangeFlags.UriChanged)) return;
        Media = args.Media;
        UpdateControlContent();
    }
}

