using System.Drawing;
using EFCore.BulkExtensions;
using MediaMaster.DataBase;
using MediaMaster.Extensions;
using MediaMaster.Interfaces.Services;
using MediaMaster.ViewModels.Dialog;
using Microsoft.EntityFrameworkCore;
using WinUI3Localizer;

namespace MediaMaster.Views.Dialog;

public sealed partial class CreateEditDeleteTagDialog : Page
{
    public EditTagDialogViewModel ViewModel = new();
    public readonly Tag? CurrentTag;

    public CreateEditDeleteTagDialog(int? tagId = null, Tag? tagParam = null)
    {
        InitializeComponent();

        if (tagId != null)
        {
            using (var database = new MediaDbContext())
            {
                tagParam = database.Tags.FirstOrDefault(t => t.TagId == tagId);
            }
            TagView.TagId = tagId;
            CurrentTag = tagParam;
        }
        else if (tagParam != null)
        {
            _ = TagView.UpdateItemSource(tagParam.Parents);
        }

        if (tagParam == null)
        {
            var emptyColor = Windows.UI.Color.FromArgb(255, 255, 255, 255);
            ViewModel.Color = emptyColor;
            return;
        }
        
        AliasesListView.Strings = tagParam.Aliases;
        ViewModel.Name = tagParam.Name;
        ViewModel.Shorthand = tagParam.Shorthand;
        ViewModel.Color = tagParam.Color.ToWindowsColor();
        ViewModel.Permissions = tagParam.Permissions;

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
            Content = tagDialog,
            RequestedTheme = App.GetService<IThemeSelectorService>().ActualTheme
        };

        Uids.SetUid(dialog, tagId == null ? "/Tag/CreateDialog" : "/Tag/EditDialog");
        if (tagDialog.CurrentTag?.Permissions.HasFlag(TagPermissions.CannotDelete) is true)
        {
            dialog.SecondaryButtonText = "";
        }
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
            DefaultButton = ContentDialogButton.Close,
            RequestedTheme = App.GetService<IThemeSelectorService>().ActualTheme
        };
        Uids.SetUid(dialog, "/Tag/DeleteDialog");
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
            if (CurrentTag == null)
            {
                trackedTag = new Tag();
            }
            else
            {
                trackedTag = await dataBase.Tags.AsTracking().Include(m => m.Parents).FirstOrDefaultAsync(t => t.TagId == CurrentTag.TagId);
            }

            if (trackedTag != null)
            {
                trackedTag.Name = ViewModel.Name;
                trackedTag.Shorthand = ViewModel.Shorthand;

                trackedTag.FirstParentReferenceName = TagView.GetItemSource().MinBy(t => t.Name)?.GetReferenceName() ?? "";

                trackedTag.Color = ViewModel.Color.ToSystemColor();
                trackedTag.Aliases = AliasesListView.Strings.ToList();

                if (CurrentTag == null)
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
                    List<TagTag> newTagTags = tagsToAdd.Select(tagId => new TagTag
                        { ParentsTagId = tagId, ChildrenTagId = trackedTag.TagId }).ToList();
                    await dataBase.BulkInsertAsync(newTagTags);
                }

                // Bulk remove old tags
                if (tagsToRemove.Count != 0)
                {
                    List<TagTag> tagTagsToRemove = await dataBase.TagTags
                        .Where(t => t.ChildrenTagId == trackedTag.TagId && tagsToRemove.Contains(t.ParentsTagId))
                        .ToListAsync();
                    await dataBase.BulkDeleteAsync(tagTagsToRemove);
                }
            }
        }
    }
}