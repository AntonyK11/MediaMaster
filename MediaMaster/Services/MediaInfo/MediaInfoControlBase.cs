using CommunityToolkit.WinUI.Controls;
using MediaMaster.DataBase;

namespace MediaMaster.Services.MediaInfo;

public abstract class MediaInfoControlBase
{
    public ICollection<Media> Medias = [];
    public readonly DockPanel Parent;
    public bool FirstShown = true;
    public bool IsCompact;
    public TextBlock? Title;

    public bool IsVisible;

    public abstract string TranslationKey { get; set; }

    protected MediaInfoControlBase(DockPanel parent)
    {
        Parent = parent;
        MediaDbContext.MediasChanged += MediaChanged;
    }

    public virtual void Initialize(ICollection<Media> medias, bool isCompact)
    {
        IsCompact = isCompact;
        if (FirstShown)
        {
            Setup();
            SetupTranslations();
            Hide();
            FirstShown = false;
        }

        if (ShowInfo(medias))
        {
            if (!IsVisible)
            {
                IsVisible = true;
                Show();
            }
            Medias = medias;
            UpdateControl();
        }
        else if (IsVisible)
        {
            IsVisible = false;
            Hide();
        }
    }

    public virtual void UpdateControl()
    {
        UpdateControlContent();
    }

    public virtual void UpdateControlContent() { }

    public virtual TextBlock GetTitle()
    {
        var textBlock = new TextBlock
        {
            Style = Application.Current.Resources["SmallTitleTextBlockStyle"] as Style
        };
        return textBlock;
    }

    public virtual bool ShowInfo(ICollection<Media> medias)
    {
        return medias.Count != 0 && !IsCompact;
    }
    
    public abstract void Setup();

    public virtual void SetupTranslations() { }

    public abstract void Show();

    public abstract void Hide();

    public virtual void InvokeMediaChange() { }

    public virtual void MediaChanged(object?  sender, MediaChangeArgs args) { }
}

