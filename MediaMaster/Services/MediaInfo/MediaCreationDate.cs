using CommunityToolkit.WinUI.Controls;
using MediaMaster.DataBase;
using MediaMaster.Extensions;

namespace MediaMaster.Services.MediaInfo;

public class MediaCreationDate(DockPanel parent) : MediaInfoTextBase(parent)
{
    public override string TranslationKey { get; set; } = "MediaCreationDate";

    public override void UpdateControlContent()
    {
        if (Text == null) return;
        Text.Text = GetDate(Medias);
    }

    public override bool ShowInfo(ICollection<Media> medias)
    {
        return medias.Count != 0 && !(IsCompact || GetDate(medias).IsNullOrEmpty());
    }

    public static string GetDate(ICollection<Media> medias)
    {
        if (medias.Count == 0) return "";

        var text = GetDate(medias.First());
        if (text == null) return "";

        return medias.Any(media => GetDate(media) != text) ? "" : text;
    }

    public static string? GetDate(Media media)
    {
        if (!Path.Exists(media.Uri)) return null;

        var date = File.GetCreationTime(media.Uri);
        return $"{date.ToLongDateString()} {date.ToShortTimeString()}";
    }

    public override void MediaChanged(object? sender, MediaChangeArgs args)
    {
        var mediaIds = Medias.Select(m => m.MediaId).ToList();
        if (Medias.Count == 0 || !args.MediaIds.Intersect(mediaIds).Any() || ReferenceEquals(sender, this) || !args.Flags.HasFlag(MediaChangeFlags.UriChanged)) return;
        Medias = args.Medias.Where(media => mediaIds.Contains(media.MediaId)).ToList();
        UpdateControlContent();
    }
}

