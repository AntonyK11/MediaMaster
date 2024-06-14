using System.Drawing;
using EFCore.BulkExtensions;
using MediaMaster.DataBase;
using MediaMaster.DataBase.Models;
using MediaMaster.Extensions;
using MediaMaster.Interfaces.Services;
using MediaMaster.ViewModels.Dialog;
using Microsoft.EntityFrameworkCore;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using WinUI3Localizer;

namespace MediaMaster.Views.Dialog;

public sealed partial class EditTagDialog : Page
{
    public EditTagDialogViewModel ViewModel = new();
    private readonly Tag? _tag;

    public EditTagDialog(int tagId)
    {
        InitializeComponent();

        using (var database = new MediaDbContext())
        {
            _tag = database.Tags.FirstOrDefault(t => t.TagId == tagId);
        }

        if (_tag == null) return;

        ViewModel.Name = _tag.Name;
        ViewModel.Shorthand = _tag.Shorthand ?? "";
        ViewModel.Color = _tag.Color.ToWindowsColor();

        TagView.TagId = tagId;

        Color color = ViewModel.Color.ToSystemColor();
        CheckColorContrast(color);
    }

    private void Confirm_Click(object sender, RoutedEventArgs e)
    {
        ViewModel.Color = ColorPicker.Color;
        Flyout.Hide();
    }

    private void Cancel_Click(object sender, RoutedEventArgs e)
    {
        ColorPicker.Color = ViewModel.Color;
        Flyout.Hide();
    }

    private void ColorPicker_OnColorChanged(ColorPicker sender, ColorChangedEventArgs args)
    {
        Color color = args.NewColor.ToSystemColor();
        CheckColorContrast(color);
    }

    private void CheckColorContrast(Color color)
    {
        var goodContrast = color.CalculateContrastRatio(color.CalculateColorText()) < 4.5;

        FontIcon.Visibility = goodContrast ? Visibility.Visible : Visibility.Collapsed;
    }

    public static async Task<(ContentDialogResult, EditTagDialog?)> ShowDialogAsync(int tagId)
    {
        if (App.MainWindow == null) return (ContentDialogResult.None, null);

        var editTagDialog = new EditTagDialog(tagId);
        ContentDialog dialog = new()
        {
            XamlRoot = App.MainWindow.Content.XamlRoot,
            DefaultButton = ContentDialogButton.Primary,
            Content = editTagDialog
        };
        Uids.SetUid(dialog, "Edit_Tag_Dialog");
        dialog.RequestedTheme = App.GetService<IThemeSelectorService>().Theme;
        App.GetService<IThemeSelectorService>().ThemeChanged += (_, theme) => { dialog.RequestedTheme = theme; };
        ContentDialogResult result = await dialog.ShowAndEnqueueAsync();
        return (result, editTagDialog);
    }

    public async Task SaveChangesAsync()
    {
        if (_tag == null) return;

        await using (MediaDbContext dataBase = new())
        {
            Tag? trackedTag =
                await dataBase.Tags.Include(m => m.Parents).FirstOrDefaultAsync(t => t.TagId == _tag.TagId);

            if (trackedTag != null)
            {
                trackedTag.Name = ViewModel.Name;
                trackedTag.Shorthand = ViewModel.Shorthand;
                trackedTag.Color = ViewModel.Color.ToSystemColor();

                await dataBase.SaveChangesAsync();

                HashSet<int> currentTagIds = trackedTag.Parents.Select(t => t.TagId).ToHashSet();
                HashSet<int> selectedTagIds = TagView.GetItemSource().Select(t => t.TagId).ToHashSet();

                List<int> tagsToAdd = selectedTagIds.Except(currentTagIds).ToList();
                List<int> tagsToRemove = currentTagIds.Except(selectedTagIds).ToList();

                // Bulk add new tags
                if (tagsToAdd.Count != 0)
                {
                    List<TagTag> newMediaTags = tagsToAdd.Select(tagId => new TagTag
                        { ParentsTagId = tagId, ChildrenTagId = trackedTag.TagId }).ToList();
                    await dataBase.BulkInsertAsync(newMediaTags);
                }

                // Bulk remove old tags
                if (tagsToRemove.Count != 0)
                {
                    List<TagTag> mediaTagsToRemove = await dataBase.TagTags
                        .Where(t => t.ChildrenTagId == trackedTag.TagId && tagsToRemove.Contains(t.ParentsTagId))
                        .ToListAsync();
                    await dataBase.BulkDeleteAsync(mediaTagsToRemove);
                }
            }
        }
    }
}