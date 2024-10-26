using CommunityToolkit.WinUI.Controls;
using MediaMaster.DataBase;

namespace MediaMaster.Services.MediaInfo;

public sealed class MediaNotes(DockPanel parent) : MediaInfoTextBlockBase(parent)
{
    protected override string TranslationKey => "MediaNotes";

    protected override void UpdateControlContent()
    {
        if (EditableTextBlock == null || Medias.Count == 0) return;
        var notes = Medias.First().Notes;

        if (Medias.Any(media => media.Notes != notes))
        {
            EditableTextBlock.Text = "";
            return;
        }

        EditableTextBlock.Text = notes;
    }

    protected override void UpdateMediaProperty(Media media, string text)
    {
        media.Notes = text;
    }

    protected override void InvokeMediaChange()
    {
        if (Medias.Count != 0) return;
        MediaDbContext.InvokeMediaChange(this, MediaChangeFlags.MediaChanged | MediaChangeFlags.NotesChanged, Medias);
    }

    protected override void MediaChanged(object? sender, MediaChangeArgs args)
    {
        HashSet<int> mediaIds = Medias.Select(m => m.MediaId).ToHashSet();
        
        if (Medias.Count == 0 ||
            !args.MediaIds.Intersect(mediaIds).Any() ||
            ReferenceEquals(sender, this) ||
            !args.Flags.HasFlag(MediaChangeFlags.NotesChanged)) return;
        
        Medias = args.Medias.Where(media => mediaIds.Contains(media.MediaId)).ToList();
        UpdateControlContent();
    }
}