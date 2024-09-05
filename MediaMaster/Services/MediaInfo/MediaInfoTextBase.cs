using CommunityToolkit.WinUI.Controls;
using MediaMaster.Extensions;
using WinUI3Localizer;

namespace MediaMaster.Services.MediaInfo;

public abstract class MediaInfoTextBase(DockPanel parent) : MediaInfoControlBase(parent)
{
    private StackPanel? _stackPanel;
    protected TextBlock? Text;

    protected override void Setup()
    {
        _stackPanel = new StackPanel
        {
            Spacing = 4
        };
        _stackPanel.SetValue(DockPanel.DockProperty, Dock.Top);

        Title = GetTitleTextBlock();
        Text = new TextBlock
        {
            IsTextSelectionEnabled = true,
            Padding = new Thickness(11, 6, 8, 7),
            TextWrapping = TextWrapping.WrapWholeWords
        };
        _stackPanel.Children.Add(Title);
        _stackPanel.Children.Add(Text);
        Parent.Children.Add(_stackPanel);
    }

    protected override void Show()
    {
        if (_stackPanel != null)
        {
            _stackPanel.Visibility = Visibility.Visible;
        }
    }

    protected override void Hide()
    {
        if (_stackPanel != null)
        {
            _stackPanel.Visibility = Visibility.Collapsed;
        }
    }

    protected override void SetupTranslations()
    {
        if (Title != null && !TranslationKey.IsNullOrEmpty())
        {
            Uids.SetUid(Title, $"/Media/{TranslationKey}_Title");
        }
    }
}