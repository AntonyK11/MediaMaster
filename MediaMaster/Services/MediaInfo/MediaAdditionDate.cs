using MediaMaster.DataBase.Models;
using Microsoft.UI.Xaml.Controls;

namespace MediaMaster.Services.MediaInfo;

public class MediaAdditionDate(StackPanel parent) : MediaInfoTextBase(parent)
{
    public override string TranslationKey { get; set; } = "MediaAdditionDate";

    public override void Initialize(Media? media)
    {
        base.Initialize(media);

        if (Text == null || media == null) return;
        var timeAdded = media.Added.ToLocalTime();
        Text.Text = $"{timeAdded.ToLongDateString()} {timeAdded.ToShortTimeString()}";
    }
}

