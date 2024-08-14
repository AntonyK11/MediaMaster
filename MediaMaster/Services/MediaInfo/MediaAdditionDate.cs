using CommunityToolkit.WinUI.Controls;
using MediaMaster.DataBase.Models;
using MediaMaster.Extensions;

namespace MediaMaster.Services.MediaInfo;

public class MediaAdditionDate(DockPanel parent) : MediaInfoTextBase(parent)
{
    public override string TranslationKey { get; set; } = "MediaAdditionDate";

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

        return medias.Any(media => GetDate(media) != text) ? "" : text;
    }

    public static string GetDate(Media media)
    {
        var date = media.Added.ToLocalTime();
        return $"{date.ToLongDateString()} {date.ToShortTimeString()}";
    }
}

