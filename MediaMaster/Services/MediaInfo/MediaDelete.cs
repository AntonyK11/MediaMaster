using CommunityToolkit.WinUI.Animations;
using CommunityToolkit.WinUI.Controls;
using MediaMaster.DataBase;
using WinUI3Localizer;

namespace MediaMaster.Services.MediaInfo;

public sealed class MediaDelete(DockPanel parent) : MediaInfoControlBase(parent)
{
    private Grid? _grid;
    private Button? _button;

    protected override string TranslationKey => "MediaDelete";

    protected override void Setup()
    {
        _grid = new Grid
        {
            Padding = new Thickness(0, 16, 0, 0)
        };
        _grid.RowDefinitions.Add(new RowDefinition { Height = new GridLength(1, GridUnitType.Star) });
        _grid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
        _grid.SetValue(DockPanel.DockProperty, Dock.Bottom);

        AddAnimation(_grid, false);

        _button = new Button
        {
            HorizontalAlignment = HorizontalAlignment.Center
        };
        _button.SetValue(Grid.RowProperty, 1);
        _button.Click += (_, _) => DeleteMedias();

        _grid.Children.Add(_button);
        Parent.Children.Add(_grid);
    }

    protected override void SetupTranslations()
    {
        if (_button != null)
        {
            Uids.SetUid(_button, $"/Media/{TranslationKey}_Button");
        }
    }

    protected override bool ShowInfo(ICollection<Media> medias)
    {
        return medias.Count != 0;
    }

    protected override void Show()
    {
        if (_grid != null)
        {
            _grid.Visibility = Visibility.Visible;
        }
    }

    protected override void Hide()
    {
        if (_grid != null)
        {
            _grid.Visibility = Visibility.Collapsed;
        }
    }

    private async void DeleteMedias()
    {
        await MediaService.DeleteMedias(this, Medias);
    }
}