using CommunityToolkit.WinUI.Controls;

namespace MediaMaster.Services.MediaInfo;

internal class MediaInfoService
{
    public readonly ICollection<MediaInfoControlBase> MediaInfoControls = [];

    public MediaInfoService(DockPanel parent)
    {
        Register(new MediaName(parent));
        Register(new MediaNameCompact(parent));
        Register(new MediaDuration(parent));
        Register(new MediaFilePath(parent));
        Register(new MediaWebUrl(parent));
        Register(new MediaNotes(parent));
        Register(new MediaCreationDate(parent));
        Register(new MediaAdditionDate(parent));
        Register(new MediaEditionDate(parent));
        Register(new MediaTags(parent));
        Register(new MediaDelete(parent));
    }

    private void Register(MediaInfoControlBase mediaInfoControl)
    {
        if (MediaInfoControls.Any(p => p == mediaInfoControl))
        {
            throw new ArgumentException($"This type is already registered : {MediaInfoControls.First(p => p == mediaInfoControl)}");
        }

        MediaInfoControls.Add(mediaInfoControl);
    }

    public void SetMedia(ICollection<Media> medias, bool isCompact)
    {
        foreach (var mediaInfoControl in MediaInfoControls)
        {
            mediaInfoControl.Initialize(medias, isCompact);
        }
    }
}

