using MediaMaster.DataBase;
using MediaMaster.DataBase.Models;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;

namespace MediaMaster.Services.MediaInfo;

public class MediaNameCompact(StackPanel parent) : MediaInfoControlBase(parent)
{
    public override string TranslationKey { get; set; } = "";
    public TextBlock? Text;

    public override void UpdateControlContent()
    {
        if (Text == null) return;

        if (Medias.Count != 0)
        {
            Text.Text = Medias.First().Name;
            Text.SetValue(ToolTipService.ToolTipProperty, Medias.First().Name);
        }
        else
        {
            Text.Text = "No Media Selected";
        }
    }

    public override void Setup()
    {
        Text = new TextBlock
        {
            IsTextSelectionEnabled = true,
            TextWrapping = TextWrapping.WrapWholeWords,
            MaxLines = 2
        };
        Parent.Children.Add(Text);
    }

    public override void Show()
    {
        if (Text != null)
        {
            Text.Visibility = Visibility.Visible;
        }
    }

    public override void Hide()
    {
        if (Text != null)
        {
            Text.Visibility = Visibility.Collapsed;
        }
    }

    public override bool ShowInfo(ICollection<Media> medias)
    {
        return IsCompact;
    }

    public override void InvokeMediaChange()
    {
        if (Medias.Count == 0) return;
        MediaDbContext.InvokeMediaChange(this, MediaChangeFlags.MediaChanged | MediaChangeFlags.NameChanged, Medias);
    }

    public override void MediaChanged(object? sender, MediaChangeArgs args)
    {
        var mediaIds = Medias.Select(m => m.MediaId).ToList();
        if (Medias.Count == 0 || !args.MediaIds.Intersect(mediaIds).Any() || ReferenceEquals(sender, this) || !args.Flags.HasFlag(MediaChangeFlags.NameChanged)) return;
        Medias = args.Medias.Where(media => mediaIds.Contains(media.MediaId)).ToList();
        UpdateControlContent();
    }
}

