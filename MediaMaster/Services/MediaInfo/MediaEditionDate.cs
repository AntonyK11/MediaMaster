using MediaMaster.DataBase;
using MediaMaster.DataBase.Models;
using MediaMaster.Extensions;
using Microsoft.UI.Xaml.Controls;

namespace MediaMaster.Services.MediaInfo;

public class MediaEditionDate(StackPanel parent) : MediaInfoTextBase(parent)
{
    public override string TranslationKey { get; set; } = "MediaEditionDate";

    public override void UpdateControlContent()
    {
        if (Text == null) return;
        Text.Text = GetDate(Medias);
    }

    public override bool ShowInfo(ICollection<Media> medias)
    {
        return medias.Count == 0 || !(IsCompact || GetDate(medias).IsNullOrEmpty());
    }

    public static string GetDate(ICollection<Media> medias)
    {
        if (medias.Count == 0) return "";

        var text = GetDate(medias.First());
        return medias.Any(media => GetDate(media) != text) ? "" : text;
    }

    public static string GetDate(Media media)
    {
        var modified = media.Modified.ToLocalTime();
        var date = modified;

        if (!media.Uri.IsWebsite())
        {
            date = File.GetLastWriteTime(media.Uri);
        }
        if (modified > date)
        {
            date = modified;
        }

        return $"{date.ToLongDateString()} {date.ToShortTimeString()} | {date.GetTimeDifference()}";
    }
    public override void MediaChanged(object? sender, MediaChangeArgs args)
    {
        var mediaIds = Medias.Select(m => m.MediaId).ToList();
        if (Medias.Count == 0 || !args.MediaIds.Intersect(mediaIds).Any()) return;
        Medias = args.Medias.Where(media => mediaIds.Contains(media.MediaId)).ToList();
        Initialize(Medias, IsCompact);
    }
}

