using Windows.UI.Text;
using MediaMaster.DataBase.Models;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media;

namespace MediaMaster.Services.MediaInfo;

public abstract class MediaInfoControlBase(StackPanel parent)
{
    public Media? Media;
    public StackPanel Parent = parent;
    public bool FirstShown = true;
    public TextBlock? Title;

    public abstract string TranslationKey { get; set; }

    public virtual void Initialize(Media? media)
    {
        if (FirstShown)
        {
            FirstShown = false;
            Setup();
        }

        if (ShowInfo(media))
        {
            {
                Show();
            }
            Media = media;
            SetupTranslations();
        }
        else
        {
            Hide();
        }
    }

    public virtual TextBlock GetTitle()
    {
        return new TextBlock
        {
            Foreground = Application.Current.Resources["TextFillColorTertiaryBrush"] as Brush,
            FontWeight = new FontWeight(600),
            FontSize = 12
        };
    }

    public virtual bool ShowInfo(Media? media)
    {
        return true;
    }
    
    public abstract void Setup();

    public abstract void SetupTranslations();

    public virtual void Show()
    {
        if (Title != null)
        {
            Title.Visibility = Visibility.Visible;
        }
    }

    public virtual void Hide()
    {
        if (Title != null)
        {
            Title.Visibility = Visibility.Collapsed;
        }
    }
}

