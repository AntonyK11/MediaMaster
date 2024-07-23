using MediaMaster.DataBase.Models;
using MediaMaster.Extensions;
using Microsoft.UI.Xaml.Controls;

namespace MediaMaster.Services.MediaInfo;

public class MediaWebUrl(StackPanel parent) : MediaInfoTextBlockBase(parent)
{
    public override string TranslationKey { get; set; } = "MediaWebUrl";

    public override void UpdateControl(Media? media, bool isCompact)
    {
        if (EditableTextBlock == null || media == null) return;
        EditableTextBlock.Text = media.Uri;
    }

    public override void UpdateMediaProperty(ref Media media, string text)
    {
        media.Uri = text;
    }

    public override bool ShowInfo(Media? media, bool isCompact)
    {
        return media == null || (!isCompact && media.Uri.IsWebsite());
    }
}

