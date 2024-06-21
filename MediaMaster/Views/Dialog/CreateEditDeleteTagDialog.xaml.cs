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

public sealed partial class CreateEditDeleteTagDialog : Page
{
    public EditTagDialogViewModel ViewModel = new();
    private readonly Tag? _tag;

    public CreateEditDeleteTagDialog(int? tagId = null, Tag? tag = null)
    {
        InitializeComponent();

        if (tagId != null)
        {
            using (var database = new MediaDbContext())
            {
                tag = database.Tags.FirstOrDefault(t => t.TagId == tagId);
            }
            TagView.TagId = tagId;
            _tag = tag;
        }
        else if (tag != null)
        {
            _ = TagView.UpdateItemSource(tag.Parents);
        }

        if (tag == null) return;
        AliasesListView.Strings = tag.Aliases;
        ViewModel.Name = tag.Name;
        ViewModel.Shorthand = tag.Shorthand;
        ViewModel.Color = tag.Color.ToWindowsColor();

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
        var badContrast = color.GetBackgroundColor(ElementTheme.Dark).CalculateContrastRatio(color.CalculateColorText(ElementTheme.Dark)) < 4.5
                           && color.GetBackgroundColor(ElementTheme.Light).CalculateContrastRatio(color.CalculateColorText(ElementTheme.Light)) < 4.5;

        ContrastIcon.Visibility = badContrast ? Visibility.Visible : Visibility.Collapsed;
    }

    public static async Task<(ContentDialogResult, CreateEditDeleteTagDialog?)> ShowDialogAsync(int? tagId = null, Tag? tag = null)
    {
        if (App.MainWindow == null) return (ContentDialogResult.None, null);

        var tagDialog = new CreateEditDeleteTagDialog(tagId, tag);
        ContentDialog dialog = new()
        {
            XamlRoot = App.MainWindow.Content.XamlRoot,
            DefaultButton = ContentDialogButton.Primary,
            Content = tagDialog
        };

        Uids.SetUid(dialog, tagId == null ? "/Tag/CreateDialog" : "/Tag/EditDialog");

        dialog.RequestedTheme = App.GetService<IThemeSelectorService>().ActualTheme;
        App.GetService<IThemeSelectorService>().ThemeChanged += (_, theme) => { dialog.RequestedTheme = theme; };
        ContentDialogResult? deleteResult;
        ContentDialogResult result;
        do
        {
            result = await dialog.ShowAndEnqueueAsync();
            deleteResult = null;

            switch (result)
            {
                case ContentDialogResult.Primary:
                {
                    await tagDialog.SaveChangesAsync();
                    break;
                }
                case ContentDialogResult.Secondary:
                {
                    if (tagId != null)
                    {
                        deleteResult = await DeleteTag((int)tagId);
                    }
                    break;
                }
            }
        } while (deleteResult == ContentDialogResult.None);

        return (result, tagDialog);
    }

    public static async Task<ContentDialogResult> DeleteTag(int tagId)
    {
        if (App.MainWindow == null) return ContentDialogResult.None;

        ContentDialog dialog = new()
        {
            XamlRoot = App.MainWindow.Content.XamlRoot,
            DefaultButton = ContentDialogButton.Primary,
        };
        Uids.SetUid(dialog, "/Tag/DeleteDialog");
        dialog.RequestedTheme = App.GetService<IThemeSelectorService>().ActualTheme;
        App.GetService<IThemeSelectorService>().ThemeChanged += (_, theme) => { dialog.RequestedTheme = theme; };
        ContentDialogResult result = await dialog.ShowAndEnqueueAsync();

        if (result == ContentDialogResult.Primary)
        {
            await using (var database = new MediaDbContext())
            {
                await database.Tags.Where(t => t.TagId == tagId).ExecuteDeleteAsync();
            }
        }

        return result;
    }

    public async Task SaveChangesAsync()
    {
        await using (MediaDbContext dataBase = new())
        {
            Tag? trackedTag;
            if (_tag == null)
            {
                trackedTag = new Tag();
            }
            else
            {
                trackedTag = await dataBase.Tags.Include(m => m.Parents).FirstOrDefaultAsync(t => t.TagId == _tag.TagId);
            }

            if (trackedTag != null)
            {
                trackedTag.Name = ViewModel.Name;
                trackedTag.Shorthand = ViewModel.Shorthand;
                trackedTag.Color = ViewModel.Color.ToSystemColor();
                trackedTag.Aliases = AliasesListView.Strings?.ToList() ?? [];

                if (_tag == null)
                {
                    await dataBase.Tags.AddAsync(trackedTag);
                }
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