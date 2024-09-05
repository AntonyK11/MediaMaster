using CommunityToolkit.WinUI.Controls;
using MediaMaster.Extensions;

namespace MediaMaster.Services.MediaInfo;

public class MediaAdditionDate(DockPanel parent) : MediaInfoTextBase(parent)
{
    protected override string TranslationKey => "MediaAdditionDate";

    protected override void UpdateControlContent()
    {
        if (Text == null) return;
        Text.Text = GetDate(Medias);
    }

    protected override bool ShowInfo(ICollection<Media> medias)
    {
        return medias.Count != 0 && !(IsCompact || GetDate(medias).IsNullOrEmpty());
    }

    private static string GetDate(ICollection<Media> medias)
    {
        if (medias.Count == 0) return "";

        var text = GetDate(medias.First());

        return medias.Any(media => GetDate(media) != text) ? "" : text;
    }

    private static string GetDate(Media media)
    {
        DateTime date = media.Added.ToLocalTime();
        return $"{date.ToLongDateString()} {date.ToShortTimeString()}";
    }
}