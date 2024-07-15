using MediaMaster.DataBase.Models;
using Microsoft.UI.Xaml.Controls;
using Microsoft.WindowsAPICodePack.Shell;
using Microsoft.WindowsAPICodePack.Shell.PropertySystem;

namespace MediaMaster.Services.MediaInfo;

public class MediaDuration(StackPanel parent) : MediaInfoTextBase(parent)
{
    public override string TranslationKey { get; set; } = "MediaVideoDuration";

    public override void Initialize(Media? media)
    {
        base.Initialize(media);

        if (Text == null || media == null) return;
        var duration = GetVideoDuration(media.FilePath);
        if (duration == null) return;
        var nonNullDuration = (TimeSpan)duration;
        nonNullDuration -= new TimeSpan(0, 0, 0, 0, nonNullDuration.Milliseconds, nonNullDuration.Microseconds);
        Text.Text = nonNullDuration.ToString();
    }

    public override bool ShowInfo(Media? media)
    {

        return media != null && GetVideoDuration(media.FilePath) != null;
    }

    public static TimeSpan? GetVideoDuration(string filePath)
    {
        using (var shell = ShellObject.FromParsingName(filePath))
        {
            IShellProperty? prop = shell?.Properties?.System?.Media.Duration;
            var t = (ulong?)prop?.ValueAsObject;
            if (t == null) return null;
            return TimeSpan.FromTicks((long)t);
        }
    }
}

