using MediaMaster.DataBase;
using MediaMaster.DataBase.Models;
using MediaMaster.Extensions;
using Microsoft.UI.Xaml.Controls;

namespace MediaMaster.Services.MediaInfo;

public class MediaWebUrl(StackPanel parent) : MediaInfoTextBlockBase(parent)
{
    public override string TranslationKey { get; set; } = "MediaWebUrl";

    public override void UpdateControlContent()
    {
        if (EditableTextBlock == null) return;
        EditableTextBlock.Text = Medias.FirstOrDefault()?.Uri ?? "";
    }

    public override void UpdateMediaProperty(Media media, string text)
    {
        media.Uri = text;
    }

    public override bool ShowInfo(ICollection<Media> medias)
    {
        return medias.Count == 0 || !(IsCompact || medias.Count != 1 || !medias.First().Uri.IsWebsite());
    }

    public override void MediaChanged(object? sender, MediaChangeArgs args)
    {
        var mediaIds = Medias.Select(m => m.MediaId).ToList();
        if (Medias.Count == 0 || !args.MediaIds.Intersect(mediaIds).Any() || ReferenceEquals(sender, this) || !args.Flags.HasFlag(MediaChangeFlags.UriChanged)) return;
        Medias = args.Medias.Where(media => mediaIds.Contains(media.MediaId)).ToList();
        UpdateControlContent();
    }
}

