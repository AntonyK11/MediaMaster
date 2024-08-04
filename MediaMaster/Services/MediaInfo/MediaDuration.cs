using MediaMaster.DataBase;
using MediaMaster.DataBase.Models;
using Microsoft.UI.Xaml.Controls;
using Microsoft.WindowsAPICodePack.Shell;
using Microsoft.WindowsAPICodePack.Shell.PropertySystem;

namespace MediaMaster.Services.MediaInfo;

public class MediaDuration(StackPanel parent) : MediaInfoTextBase(parent)
{
    public override string TranslationKey { get; set; } = "MediaVideoDuration";

    public override void UpdateControlContent()
    {
        if (Text == null || Media == null) return;
        var duration = GetVideoDuration(Media.Uri);
        if (duration == null) return;
        var nonNullDuration = (TimeSpan)duration;
        nonNullDuration = new TimeSpan(nonNullDuration.Days, nonNullDuration.Hours, nonNullDuration.Minutes, nonNullDuration.Seconds);
        Text.Text = nonNullDuration.ToString();
    }

    public override bool ShowInfo(Media? media, bool isCompact)
    {
        return media == null || (!isCompact && GetVideoDuration(media.Uri) != null);
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

    public override void InvokeMediaChange(Media media)
    {
        if (Media == null) return;
        MediaDbContext.InvokeMediaChange(this, MediaChangeFlags.MediaChanged | MediaChangeFlags.UriChanged, Media);
    }

    public override void MediaChanged(object? sender, MediaChangeArgs args)
    {
        if (Media == null || args.Media.MediaId != Media.MediaId || ReferenceEquals(sender, this) || !args.Flags.HasFlag(MediaChangeFlags.UriChanged)) return;
        Media = args.Media;
        UpdateControlContent();
    }
}

