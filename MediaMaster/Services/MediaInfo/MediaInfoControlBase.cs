using MediaMaster.DataBase;
using MediaMaster.DataBase.Models;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;

namespace MediaMaster.Services.MediaInfo;

public abstract class MediaInfoControlBase
{
    public Media? Media;
    public StackPanel Parent;
    public bool FirstShown = true;
    public TextBlock? Title;

    public bool IsVisible;

    public abstract string TranslationKey { get; set; }

    protected MediaInfoControlBase(StackPanel parent)
    {
        Parent = parent;
        MediaDbContext.MediaChanged += MediaChanged;
    }

    public virtual void Initialize(Media? media, bool isCompact)
    {
        if (FirstShown)
        {
            FirstShown = false;
            Setup(isCompact);
            SetupTranslations();
        }

        if (ShowInfo(media, isCompact))
        {
            if (!IsVisible)
            {
                IsVisible = true;
                Show(isCompact);
            }
            Media = media;
            UpdateControl(isCompact);
        }
        else if (IsVisible)
        {
            IsVisible = false;
            Hide();
        }
    }

    public virtual void UpdateControl(bool isCompact)
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

    public virtual bool ShowInfo(Media? media, bool isCompact)
    {
        return media == null || !isCompact;
    }
    
    public abstract void Setup(bool isCompact);

    public abstract void SetupTranslations();

    public abstract void Show(bool isCompact);

    public abstract void Hide();

    public virtual void InvokeMediaChange(Media media) { }

    public virtual void MediaChanged(object?  sender, MediaChangeArgs args) { }
}

