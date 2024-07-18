using MediaMaster.DataBase.Models;
using MediaMaster.Extensions;
using Microsoft.UI.Xaml.Controls;

namespace MediaMaster.Services.MediaInfo;

public class MediaCreationDate(StackPanel parent) : MediaInfoTextBase(parent)
{
    public override string TranslationKey { get; set; } = "MediaCreationDate";

    public override void Initialize(Media? media)
    {
        base.Initialize(media);

        if (Text == null || media == null || media.Uri.IsWebsite()) return;
        var date = File.GetCreationTime(media.Uri);
        Text.Text = $"{date.ToLongDateString()} {date.ToShortTimeString()}";
    }

    public override bool ShowInfo(Media? media)
    {
        return media == null || !media.Uri.IsWebsite();
    }
}

