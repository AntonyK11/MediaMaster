using CommunityToolkit.WinUI.Controls;
using MediaMaster.DataBase;
using WinUI3Localizer;

namespace MediaMaster.Services.MediaInfo;

public class MediaNameCompact(DockPanel parent) : MediaInfoControlBase(parent)
{
    private TextBlock? _text;
    protected override string TranslationKey => "";

    protected override void UpdateControlContent()
    {
        if (_text == null) return;

        if (Medias.Count != 0)
        {
            _text.Text = Medias.First().Name;
            _text.SetValue(ToolTipService.ToolTipProperty, Medias.First().Name);
        }
        else
        {
            _text.Text = "/Media/NoMediaSelected".GetLocalizedString();
        }
    }

    protected override void Setup()
    {
        _text = new TextBlock
        {
            IsTextSelectionEnabled = true,
            TextWrapping = TextWrapping.WrapWholeWords,
            MaxLines = 2
        };
        _text.SetValue(DockPanel.DockProperty, Dock.Top);

        Parent.Children.Add(_text);
    }

    protected override void Show()
    {
        if (_text != null)
        {
            _text.Visibility = Visibility.Visible;
        }
    }

    protected override void Hide()
    {
        if (_text != null)
        {
            _text.Visibility = Visibility.Collapsed;
        }
    }

    protected override bool ShowInfo(ICollection<Media> medias)
    {
        return IsCompact;
    }

    protected override void InvokeMediaChange()
    {
        if (Medias.Count == 0) return;
        MediaDbContext.InvokeMediaChange(this, MediaChangeFlags.MediaChanged | MediaChangeFlags.NameChanged, Medias);
    }

    protected override void MediaChanged(object? sender, MediaChangeArgs args)
    {
        List<int> mediaIds = Medias.Select(m => m.MediaId).ToList();
        
        if (Medias.Count == 0 ||
            !args.MediaIds.Intersect(mediaIds).Any() ||
            ReferenceEquals(sender, this) ||
            !args.Flags.HasFlag(MediaChangeFlags.NameChanged)) return;
        
        Medias = args.Medias.Where(media => mediaIds.Contains(media.MediaId)).ToList();
        UpdateControlContent();
    }
}