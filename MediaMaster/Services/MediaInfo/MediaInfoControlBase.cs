using Windows.UI.Text;
using MediaMaster.DataBase.Models;
using MediaMaster.Interfaces.Services;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media;
using WinUICommunity;

namespace MediaMaster.Services.MediaInfo;

public abstract class MediaInfoControlBase(StackPanel parent)
{
    public Media? Media;
    public StackPanel Parent = parent;
    public bool FirstShown = true;
    public TextBlock? Title;

    public bool IsVisible;

    public abstract string TranslationKey { get; set; }

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
            UpdateControl(media, isCompact);
        }
        else if (IsVisible)
        {
            IsVisible = false;
            Hide();
        }
    }

    public abstract void UpdateControl(Media? media, bool isCompact);

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
}

