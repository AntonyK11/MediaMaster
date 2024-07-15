using MediaMaster.DataBase.Models;
using MediaMaster.Extensions;
using Microsoft.UI.Xaml.Controls;

namespace MediaMaster.Services.MediaInfo;

public class MediaEditionDate(StackPanel parent) : MediaInfoTextBase(parent)
{
    public override string TranslationKey { get; set; } = "MediaEditionDate";

    public override void Initialize(Media? media)
    {
        base.Initialize(media);

        if (Text == null || media == null) return;
        var date = File.GetLastWriteTime(media.FilePath);
        var modified = media.Modified.ToLocalTime();
        if (modified > date)
        {
            date = modified;
        }

        Text.Text = $"{date.ToLongDateString()} {date.ToShortTimeString()} | {date.GetTimeDifference()}";
    }
}

