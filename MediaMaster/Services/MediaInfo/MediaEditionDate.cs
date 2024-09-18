using CommunityToolkit.WinUI.Controls;
using MediaMaster.DataBase;
using MediaMaster.Extensions;

namespace MediaMaster.Services.MediaInfo;

public class MediaEditionDate(DockPanel parent) : MediaInfoTextBase(parent)
{
    protected override string TranslationKey => "MediaEditionDate";

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
        return medias.Any(media => GetDate(media) != text) ? "" : text;
    }

    private static string GetDate(Media media)
    {
        DateTime modified = media.Modified.ToLocalTime();
        DateTime date = modified;

        if (Path.Exists(media.Uri))
        {
            date = File.GetLastWriteTime(media.Uri);
        }

        if (modified > date)
        {
            date = modified;
        }

        return $"{date.ToLongDateString()} {date.ToShortTimeString()} | {date.GetTimeDifference()}";
    }

    protected override void MediaChanged(object? sender, MediaChangeArgs args)
    {
        List<int> mediaIds = Medias.Select(m => m.MediaId).ToList();
        
        if (Medias.Count == 0 || !args.MediaIds.Intersect(mediaIds).Any()) return;
        
        Medias = args.Medias.Where(media => mediaIds.Contains(media.MediaId)).ToList();
        Initialize(Medias);
    }
}