using MediaMaster.DataBase.Models;
using Microsoft.UI.Xaml.Controls;

namespace MediaMaster.Services.MediaInfo;

public class MediaCreationDate(StackPanel parent) : MediaInfoTextBase(parent)
{
    public override string TranslationKey { get; set; } = "MediaCreationDate";

    public override void UpdateControlContent()
    {
        if (Text == null || Media == null || !File.Exists(Media.Uri)) return;
        var date = File.GetCreationTime(Media.Uri);
        Text.Text = $"{date.ToLongDateString()} {date.ToShortTimeString()}";
    }

    public override bool ShowInfo(Media? media, bool isCompact)
    {
        return media == null || !(isCompact || !File.Exists(media.Uri));
    }
}

