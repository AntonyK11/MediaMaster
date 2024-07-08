using MediaMaster.DataBase;
using MediaMaster.DataBase.Models;
using Microsoft.UI.Xaml.Controls;

namespace MediaMaster.Services.MediaInfo;

public abstract class MediaInfoControlBase(StackPanel parent)
{
    public Media? Media;
    public StackPanel Parent = parent;
    public bool FirstShown = true;

    public abstract string TranslationKey { get; set; }

    public virtual void Initialize(Media? media)
    {
        if (ShowInfo(media))
        {
            if (FirstShown)
            {
                FirstShown = false;
                Setup();
            }
            else
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

    public virtual bool ShowInfo(Media? media)
    {
        return true;
    }
    
    public abstract void Setup();

    public abstract void SetupTranslations();

    public abstract void Show();

    public abstract void Hide();

    public virtual async void UpdateMedia(string text)
    {
        if (Media == null) return;

        await using (var database = new MediaDbContext())
        {
            var media = await database.FindAsync<Media>(Media.MediaId);

            if (media == null) return;

            UpdateMediaProperty(ref media, text);

            await database.SaveChangesAsync();
        }
    }

    public virtual void UpdateMediaProperty(ref Media media, string text) { }
}

