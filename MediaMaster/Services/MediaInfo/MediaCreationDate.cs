using MediaMaster.DataBase.Models;
using Microsoft.UI.Xaml.Controls;
using WinUI3Localizer;

namespace MediaMaster.Services.MediaInfo;

public class MediaCreationDate(StackPanel parent) : MediaInfoTextBase(parent)
{
    public override string TranslationKey { get; set; } = "MediaCreationDate";

    public override void Initialize(Media? media)
    {
        base.Initialize(media);

        if (Text == null || media == null) return;
        var date = File.GetCreationTime(media.FilePath);
        Text.Text = $"{date.ToLongDateString()} {date.ToShortTimeString()}";
    }

    public override void SetupTranslations()
    {
        if (Title != null)
        {
            Uids.SetUid(Title, $"/Media/{TranslationKey}_Title");
        }
    }
}

