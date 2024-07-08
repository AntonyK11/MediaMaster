using Windows.Storage.Pickers;
using MediaMaster.Controls;
using MediaMaster.DataBase.Models;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using WinUIEx;

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
        editableTextBlock.Edit += PathTextBox_OnEdit;
        return editableTextBlock;
    }

    private async void PathTextBox_OnEdit(EditableTextBlock sender, string args)
    {
        if (App.MainWindow == null) return;

        var openPicker = new FileOpenPicker();

        var hWnd = App.MainWindow.GetWindowHandle();

        WinRT.Interop.InitializeWithWindow.Initialize(openPicker, hWnd);

        openPicker.ViewMode = PickerViewMode.Thumbnail;

        //openPicker.SuggestedStartLocation = PickerLocationId.ComputerFolder Path.GetDirectoryName(PathTextBox.Text);
        openPicker.FileTypeFilter.Add("*");
        openPicker.CommitButtonText = "hello";

        var file = await openPicker.PickSingleFileAsync();
    }

    public override void UpdateMediaProperty(ref Media media, string text)
    {
        media.FilePath = text;
    }
}

