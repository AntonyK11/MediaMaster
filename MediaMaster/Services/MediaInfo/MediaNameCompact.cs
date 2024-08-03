using MediaMaster.DataBase;
using MediaMaster.DataBase.Models;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;

namespace MediaMaster.Services.MediaInfo;

public class MediaNameCompact(StackPanel parent) : MediaInfoTextBase(parent)
{
    public override string TranslationKey { get; set; } = "";

    public override void UpdateControlContent()
    {
        if (Text == null || Media == null) return;
        Text.Text = Media.Name;
        Text.SetValue(ToolTipService.ToolTipProperty, Media.Name);
    }

    public override void Setup(bool isCompact)
    {
        Text = new TextBlock
        {
            IsTextSelectionEnabled = true,
            TextWrapping = TextWrapping.WrapWholeWords,
            MaxLines = 2
        };
        Parent.Children.Add(Text);
    }

    public override bool ShowInfo(Media? media, bool isCompact)
    {
        return media == null || isCompact;
    }

    public override void InvokeMediaChange(Media media)
    {
        if (Media == null) return;
        MediaDbContext.InvokeMediaChange(MediaChangeFlags.MediaChanged | MediaChangeFlags.NameChanged, Media);
    }

    public override void MediaChanged(MediaChangeArgs args)
    {
        if (Media == null || args.Media.MediaId != Media.MediaId || !args.Flags.HasFlag(MediaChangeFlags.NameChanged)) return;
        Media = args.Media;
        UpdateControlContent();
    }
}

