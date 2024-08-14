using CommunityToolkit.WinUI.Controls;
using MediaMaster.DataBase;
using MediaMaster.DataBase.Models;

namespace MediaMaster.Services.MediaInfo;

public class MediaDescription(DockPanel parent) : MediaInfoTextBlockBase(parent)
{
    public override string TranslationKey { get; set; } = "MediaDescription";

    public override void UpdateControlContent()
    {
        if (EditableTextBlock == null || Medias.Count == 0) return;
        var description = Medias.First().Description;

        if (Medias.Any(media => media.Description != description))
        {
            EditableTextBlock.Text = "";
            return;
        }
        EditableTextBlock.Text = description;
    }

    public override void UpdateMediaProperty(Media media, string text)
    {
        media.Description = text;
    }

    public override void InvokeMediaChange()
    {
        if (Medias.Count != 0) return;
        MediaDbContext.InvokeMediaChange(this, MediaChangeFlags.MediaChanged | MediaChangeFlags.DescriptionChanged, Medias);
    }

    public override void MediaChanged(object? sender, MediaChangeArgs args)
    {
        var mediaIds = Medias.Select(m => m.MediaId).ToList();
        if (Medias.Count == 0 || !args.MediaIds.Intersect(mediaIds).Any() || ReferenceEquals(sender, this) || !args.Flags.HasFlag(MediaChangeFlags.DescriptionChanged)) return;
        Medias = args.Medias.Where(media => mediaIds.Contains(media.MediaId)).ToList();
        UpdateControlContent();
    }
}

