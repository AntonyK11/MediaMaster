using MediaMaster.DataBase;
using MediaMaster.DataBase.Models;
using Microsoft.UI.Xaml.Controls;

namespace MediaMaster.Services.MediaInfo;

public class MediaDescription(StackPanel parent) : MediaInfoTextBlockBase(parent)
{
    public override string TranslationKey { get; set; } = "MediaDescription";

    public override void UpdateControlContent()
    {
        if (EditableTextBlock == null || Media == null) return;
        EditableTextBlock.Text = Media.Description;
    }

    public override void UpdateMediaProperty(Media media, string text)
    {
        media.Description = text;
    }

    public override void InvokeMediaChange(Media media)
    {
        if (Media == null) return;
        MediaDbContext.InvokeMediaChange(MediaChangeFlags.MediaChanged | MediaChangeFlags.DescriptionChanged, Media);
    }

    public override void MediaChanged(MediaChangeArgs args)
    {
        if (Media == null || args.Media.MediaId != Media.MediaId || !args.Flags.HasFlag(MediaChangeFlags.DescriptionChanged)) return;
        Media = args.Media;
        UpdateControlContent();
    }
}

