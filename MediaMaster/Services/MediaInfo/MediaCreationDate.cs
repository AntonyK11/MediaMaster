using CommunityToolkit.WinUI.Controls;
using MediaMaster.DataBase;
using MediaMaster.Extensions;

namespace MediaMaster.Services.MediaInfo;

public sealed class MediaCreationDate(DockPanel parent) : MediaInfoTextBase(parent)
{
    protected override string TranslationKey => "MediaCreationDate";

    protected override void UpdateControlContent()
    {
        if (Text == null) return;
        Text.Text = GetDate(Medias);
    }

    protected override bool ShowInfo(ICollection<Media> medias)
    {
        return medias.Count != 0 && !GetDate(medias).IsNullOrEmpty();
    }

    private static string GetDate(ICollection<Media> medias)
    {
        if (medias.Count == 0) return "";

        var text = GetDate(medias.First());
        if (text == null) return "";

        return medias.Any(media => GetDate(media) != text) ? "" : text;
    }

    private static string? GetDate(Media media)
    {
        if (!Path.Exists(media.Uri)) return null;

        DateTime date = File.GetCreationTime(media.Uri);
        return $"{date.ToLongDateString()} {date.ToShortTimeString()}";
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