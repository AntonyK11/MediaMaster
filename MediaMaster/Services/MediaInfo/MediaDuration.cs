using CommunityToolkit.WinUI.Controls;
using MediaMaster.DataBase;
using MediaMaster.Extensions;
using Microsoft.WindowsAPICodePack.Shell;
using Microsoft.WindowsAPICodePack.Shell.PropertySystem;

namespace MediaMaster.Services.MediaInfo;

public sealed class MediaDuration(DockPanel parent) : MediaInfoTextBase(parent)
{
    protected override string TranslationKey => "MediaDuration";

    protected override void UpdateControlContent()
    {
        if (Text == null) return;
        Text.Text = GetDuration(Medias);
    }

    protected override bool ShowInfo(ICollection<Media> medias)
    {
        return medias.Count != 0 && !GetDuration(medias).IsNullOrEmpty();
    }

    private string GetDuration(ICollection<Media> medias)
    {
        if (medias.Count == 0) return "";

        var duration = GetDuration(medias.First());

        return Medias.Any(media => GetDuration(media) != duration) ? "" : duration;
    }

    private static string GetDuration(Media media)
    {
        if (!Path.Exists(media.Uri)) return "";

        using (ShellObject? shell = ShellObject.FromParsingName(media.Uri))
        {
            IShellProperty? prop = shell?.Properties?.System?.Media.Duration;
            var t = (ulong?)prop?.ValueAsObject;
            if (t == null) return "";

            TimeSpan nonNullDuration = TimeSpan.FromTicks((long)t);
            
            return new TimeSpan(
                nonNullDuration.Days,
                nonNullDuration.Hours,
                nonNullDuration.Minutes,
                nonNullDuration.Seconds).ToString();
        }
    }

    protected override void InvokeMediaChange()
    {
        if (Medias.Count == 0) return;
        MediaDbContext.InvokeMediaChange(this, MediaChangeFlags.MediaChanged | MediaChangeFlags.UriChanged, Medias);
    }

    protected override void MediaChanged(object? sender, MediaChangeArgs args)
    {
        List<int> mediaIds = Medias.Select(m => m.MediaId).ToList();
        
        if (Medias.Count == 0 ||
            !args.MediaIds.Intersect(mediaIds).Any() ||
            ReferenceEquals(sender, this) ||
            !args.Flags.HasFlag(MediaChangeFlags.UriChanged)) return;
        
        Medias = args.Medias.Where(media => mediaIds.Contains(media.MediaId)).ToList();
        UpdateControlContent();
    }
}