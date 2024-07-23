using MediaMaster.DataBase.Models;
using MediaMaster.Extensions;
using Microsoft.UI.Xaml.Controls;

namespace MediaMaster.Services.MediaInfo;

public class MediaCreationDate(StackPanel parent) : MediaInfoTextBase(parent)
{
    public override string TranslationKey { get; set; } = "MediaCreationDate";

    public override void UpdateControl(Media? media, bool isCompact)
    {
        if (Text == null || media == null || !File.Exists(media.Uri)) return;
        var date = File.GetCreationTime(media.Uri);
        Text.Text = $"{date.ToLongDateString()} {date.ToShortTimeString()}";
    }

    public override bool ShowInfo(Media? media, bool isCompact)
    {
        return media == null || !(isCompact || !File.Exists(media.Uri));
    }
}

