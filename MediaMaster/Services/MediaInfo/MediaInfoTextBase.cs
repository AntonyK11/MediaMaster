using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;

namespace MediaMaster.Services.MediaInfo;

public abstract class MediaInfoTextBase(StackPanel parent) : MediaInfoControlBase(parent)
{
    public TextBlock? Text;

    public override void Setup()
    {
        var stackPanel = new StackPanel
        {
            Spacing = 4
        };
        Title = GetTitle();
        Text = new TextBlock
        {
            IsTextSelectionEnabled = true,
            Padding = new Thickness(11,6,8,7),
            TextWrapping = TextWrapping.WrapWholeWords
        };
        stackPanel.Children.Add(Title);
        stackPanel.Children.Add(Text);
        Parent.Children.Add(stackPanel);
    }

    public override void Show()
    {
        base.Show();
        if (Text != null)
        {
            Text.Visibility = Visibility.Visible;
        }
    }

    public override void Hide()
    {
        base.Hide();
        if (Text != null)
        {
            Text.Visibility = Visibility.Collapsed;
        }
    }
}

