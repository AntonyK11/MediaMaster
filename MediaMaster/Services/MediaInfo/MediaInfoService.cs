using MediaMaster.DataBase.Models;
using Microsoft.UI.Xaml.Controls;

namespace MediaMaster.Services.MediaInfo;

internal class MediaInfoService
{
    public readonly ICollection<MediaInfoControlBase> MediaInfoControls = [];

    public MediaInfoService(StackPanel parent)
    {
        Register(new MediaName(parent));
        Register(new MediaNameCompact(parent));
        Register(new MediaDuration(parent));
        Register(new MediaFilePath(parent));
        Register(new MediaWebUrl(parent));
        Register(new MediaDescription(parent));
        Register(new MediaCreationDate(parent));
        Register(new MediaAdditionDate(parent));
        Register(new MediaEditionDate(parent));
        Register(new MediaTags(parent));
    }

    private void Register(MediaInfoControlBase mediaInfoControl)
    {
        if (MediaInfoControls.Any(p => p == mediaInfoControl))
        {
            throw new ArgumentException($"This type is already registered : {MediaInfoControls.First(p => p == mediaInfoControl)}");
        }

        MediaInfoControls.Add(mediaInfoControl);
    }

    public void SetMedia(Media? media, bool isCompact)
    {
        foreach (var mediaInfoControl in MediaInfoControls)
        {
            mediaInfoControl.Initialize(media, isCompact);
        }
    }
}

