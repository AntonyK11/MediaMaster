using Microsoft.UI.Xaml.Controls;

namespace MediaMaster.Services.MediaInfo;

public class MediaAdditionDate(StackPanel parent) : MediaInfoTextBase(parent)
{
    public override string TranslationKey { get; set; } = "MediaAdditionDate";

    public override void UpdateControlContent()
    {
        if (Text == null || Media == null) return;
        var timeAdded = Media.Added.ToLocalTime();
        Text.Text = $"{timeAdded.ToLongDateString()} {timeAdded.ToShortTimeString()}";
    }
}

