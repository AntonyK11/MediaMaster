using MediaMaster.DataBase.Models;
using MediaMaster.Extensions;
using Microsoft.UI.Xaml.Controls;

namespace MediaMaster.Services.MediaInfo;

public class MediaWebUrl(StackPanel parent) : MediaInfoTextBlockBase(parent)
{
    public override string TranslationKey { get; set; } = "MediaWebUrl";

    public override void UpdateControlContent()
    {
        if (EditableTextBlock == null || Media == null) return;
        EditableTextBlock.Text = Media.Uri;
    }

    public override void UpdateMediaProperty(Media media, string text)
    {
        media.Uri = text;
    }

    public override bool ShowInfo(Media? media, bool isCompact)
    {
        return media == null || (!isCompact && media.Uri.IsWebsite());
    }
}

