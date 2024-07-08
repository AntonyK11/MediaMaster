using MediaMaster.DataBase.Models;
using Microsoft.UI.Xaml.Controls;

namespace MediaMaster.Services.MediaInfo;

public class MediaDescription(StackPanel parent) : MediaInfoTextBlockBase(parent)
{
    public override string TranslationKey { get; set; } = "MediaDescription";

    public override void Initialize(Media? media)
    {
        base.Initialize(media);

        if (EditableTextBlock == null || media == null) return;
        EditableTextBlock.Text = media.Description;
    }

    public override void UpdateMediaProperty(ref Media media, string text)
    {
        media.Description = text;
    }
}

