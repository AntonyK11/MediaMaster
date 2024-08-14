using CommunityToolkit.WinUI.Controls;
using EFCore.BulkExtensions;
using MediaMaster.DataBase;
using MediaMaster.DataBase.Models;
using MediaMaster.Extensions;
using MediaMaster.Interfaces.Services;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using WinUI3Localizer;

namespace MediaMaster.Services.MediaInfo;

public class MediaDelete(DockPanel parent) : MediaInfoControlBase(parent)
{
    public Button? Button;

    public override string TranslationKey { get; set; } = "MediaDelete";

    public override void Setup()
    {
        var grid = new Grid
        {
            Padding = new Thickness(0, 16, 0, 0)
        };
        grid.RowDefinitions.Add(new RowDefinition { Height = new GridLength(1, GridUnitType.Star)});
        grid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
        grid.SetValue(DockPanel.DockProperty, Dock.Bottom);

        Button = new Button
        {
            HorizontalAlignment = HorizontalAlignment.Center
        };
        Button.SetValue(Grid.RowProperty, 1);
        Button.Click += (_, _) => DeleteMedias();

        grid.Children.Add(Button);
        Parent.Children.Add(grid);
    }

    public override void SetupTranslations()
    {
        if (Button != null)
        {
            Uids.SetUid(Button, $"/Media/{TranslationKey}_Button");
        }
    }

    public override bool ShowInfo(ICollection<Media> medias)
    {
        return medias.Count != 0 && !IsCompact;
    }

    public override void Show()
    {
        if (Button != null)
        {
            Button.Visibility = Visibility.Visible;
        }
    }

    public override void Hide()
    {
        if (Button != null)
        {
            Button.Visibility = Visibility.Collapsed;
        }
    }

    public async void DeleteMedias()
    {
        if (App.MainWindow == null) return;

        ContentDialog dialog = new()
        {
            XamlRoot = App.MainWindow.Content.XamlRoot,
            DefaultButton = ContentDialogButton.Primary,
        };
        Uids.SetUid(dialog, "/Media/DeleteDialog");
        dialog.DefaultButton = ContentDialogButton.Close;
        dialog.RequestedTheme = App.GetService<IThemeSelectorService>().ActualTheme;
        App.GetService<IThemeSelectorService>().ThemeChanged += (_, theme) => { dialog.RequestedTheme = theme; };
        ContentDialogResult result = await dialog.ShowAndEnqueueAsync();

        if (result == ContentDialogResult.Primary)
        {
            await using (var database = new MediaDbContext())
            {
                await database.BulkDeleteAsync(Medias);
            }

            MediaDbContext.InvokeMediaChange(this, MediaChangeFlags.MediaRemoved, Medias);
        }
    }
}

