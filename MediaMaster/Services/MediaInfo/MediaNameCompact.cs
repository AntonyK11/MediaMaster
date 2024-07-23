using MediaMaster.DataBase.Models;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;

namespace MediaMaster.Services.MediaInfo;

public class MediaNameCompact(StackPanel parent) : MediaInfoTextBase(parent)
{
    public override string TranslationKey { get; set; } = "";

    public override void UpdateControl(Media? media, bool isCompact)
    {
        if (Text == null || media == null) return;
        Text.Text = media.Name;
        Text.SetValue(ToolTipService.ToolTipProperty, media.Name);
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
}

