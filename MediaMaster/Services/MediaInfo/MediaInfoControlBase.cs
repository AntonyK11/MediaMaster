using CommunityToolkit.WinUI.Animations;
using CommunityToolkit.WinUI.Controls;
using MediaMaster.DataBase;

namespace MediaMaster.Services.MediaInfo;

public abstract class MediaInfoControlBase
{
    protected readonly DockPanel Parent;
    private bool _firstShown = true;

    private bool _isVisible;
    protected ICollection<Media> Medias = [];
    protected TextBlock? Title;

    protected MediaInfoControlBase(DockPanel parent)
    {
        Parent = parent;
        MediaDbContext.MediasChanged += MediaChanged;
    }

    protected abstract string TranslationKey { get; }

    public void Initialize(ICollection<Media> medias)
    {
        if (_firstShown)
        {
            Setup();
            SetupTranslations();
            Hide();
            _firstShown = false;
        }

        if (ShowInfo(medias))
        {
            if (!_isVisible)
            {
                _isVisible = true;
                Show();
            }

            Medias = medias;
            UpdateControl();
        }
        else if (_isVisible)
        {
            _isVisible = false;
            Hide();
        }
    }

    protected virtual void UpdateControl()
    {
        UpdateControlContent();
    }

    protected virtual void UpdateControlContent() { }

    protected static TextBlock GetTitleTextBlock()
    {
        var textBlock = new TextBlock
        {
            Style = Application.Current.Resources["SmallTitleTextBlockStyle"] as Style
        };
        return textBlock;
    }

    protected virtual bool ShowInfo(ICollection<Media> medias)
    {
        return medias.Count != 0;
    }

    protected abstract void Setup();

    protected virtual void SetupTranslations() { }

    protected abstract void Show();

    protected abstract void Hide();

    protected virtual void InvokeMediaChange() { }

    protected virtual void MediaChanged(object? sender, MediaChangeArgs args) { }

    protected static void AddAnimation(FrameworkElement element, bool offsetAnimation = true)
    {
        ImplicitAnimationSet showSet = [
            new TranslationAnimation { Duration = TimeSpan.FromSeconds(0.1), From = "0, 17, 0", To = "0, 0, 0" },
            new OpacityAnimation { Duration = TimeSpan.FromSeconds(0.1), From = 0, To = 1 }
        ];
        element.SetValue(Implicit.ShowAnimationsProperty, showSet);

        ImplicitAnimationSet hideSet = [
            new TranslationAnimation { Duration = TimeSpan.FromSeconds(0.1), From = "0, 0, 0", To = "0, 17, 0" },
            new OpacityAnimation { Duration = TimeSpan.FromSeconds(0.1), From = 1, To = 0 },
        ];
        element.SetValue(Implicit.HideAnimationsProperty, hideSet);

        if (offsetAnimation)
        {
            ImplicitAnimationSet moveSet =
            [
                new OffsetAnimation { Duration = TimeSpan.FromSeconds(0.1) }
            ];
            element.SetValue(Implicit.AnimationsProperty, moveSet);
        }
    }
}