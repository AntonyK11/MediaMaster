using MediaMaster.DataBase;
using MediaMaster.DataBase.Models;
using MediaMaster.Extensions;
using Microsoft.UI.Xaml.Controls;
using Microsoft.WindowsAPICodePack.Shell;
using Microsoft.WindowsAPICodePack.Shell.PropertySystem;

namespace MediaMaster.Services.MediaInfo;

public class MediaDuration(StackPanel parent) : MediaInfoTextBase(parent)
{
    public override string TranslationKey { get; set; } = "MediaDuration";

    public override void UpdateControlContent()
    {
        if (Text == null) return;
        Text.Text = GetDuration(Medias);
    }

    public override bool ShowInfo(ICollection<Media> medias)
    {
        return medias.Count != 0 && !(IsCompact || GetDuration(medias).IsNullOrEmpty());
    }

    public string GetDuration(ICollection<Media> medias)
    {
        if (medias.Count == 0) return "";

        var duration = GetDuration(medias.First());
        
        return Medias.Any(media => GetDuration(media) != duration) ? "" : duration;
    }

    public static string GetDuration(Media media)
    {
        using (var shell = ShellObject.FromParsingName(media.Uri))
        {
            IShellProperty? prop = shell?.Properties?.System?.Media.Duration;
            var t = (ulong?)prop?.ValueAsObject;
            if (t == null) return "";

            var nonNullDuration = TimeSpan.FromTicks((long)t);
            return new TimeSpan(nonNullDuration.Days, nonNullDuration.Hours, nonNullDuration.Minutes, nonNullDuration.Seconds).ToString();
        }
    }

    public override void InvokeMediaChange()
    {
        if (Medias.Count == 0) return;
        MediaDbContext.InvokeMediaChange(this, MediaChangeFlags.MediaChanged | MediaChangeFlags.UriChanged, Medias);
    }

    public override void MediaChanged(object? sender, MediaChangeArgs args)
    {
        var mediaIds = Medias.Select(m => m.MediaId).ToList();
        if (Medias.Count == 0 || !args.MediaIds.Intersect(mediaIds).Any() || ReferenceEquals(sender, this) || !args.Flags.HasFlag(MediaChangeFlags.UriChanged)) return;
        Medias = args.Medias.Where(media => mediaIds.Contains(media.MediaId)).ToList();
        UpdateControlContent();
    }
}

