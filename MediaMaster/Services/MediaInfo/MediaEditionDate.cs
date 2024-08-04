using MediaMaster.DataBase;
using MediaMaster.Extensions;
using Microsoft.UI.Xaml.Controls;

namespace MediaMaster.Services.MediaInfo;

public class MediaEditionDate(StackPanel parent) : MediaInfoTextBase(parent)
{
    public override string TranslationKey { get; set; } = "MediaEditionDate";

    public override void UpdateControlContent()
    {
        if (Text == null || Media == null) return;

        var modified = Media.Modified.ToLocalTime();
        var date = modified;

        if (!Media.Uri.IsWebsite())
        {
            date = File.GetLastWriteTime(Media.Uri);
        }
        if (modified > date)
        {
            date = modified;
        }

        Text.Text = $"{date.ToLongDateString()} {date.ToShortTimeString()} | {date.GetTimeDifference()}";
    }

    public override void MediaChanged(object? sender, MediaChangeArgs args)
    {
        if (Media == null || args.Media.MediaId != Media.MediaId) return;
        Media = args.Media;
        UpdateControlContent();
    }
}

