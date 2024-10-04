using CommunityToolkit.WinUI.Controls;

namespace MediaMaster.Services.MediaInfo;

internal sealed class MediaInfoService
{
    private readonly ICollection<MediaInfoControlBase> _mediaInfoControls = [];

    public MediaInfoService(DockPanel parent)
    {
        Register(new MediaName(parent));
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
        if (_mediaInfoControls.Any(p => p == mediaInfoControl))
        {
            throw new ArgumentException(
                $"This type is already registered : {_mediaInfoControls.First(p => p == mediaInfoControl)}");
        }

        _mediaInfoControls.Add(mediaInfoControl);
    }

    public void SetMedia(ICollection<Media> medias)
    {
        foreach (MediaInfoControlBase mediaInfoControl in _mediaInfoControls)
        {
            mediaInfoControl.Initialize(medias);
        }
    }
}