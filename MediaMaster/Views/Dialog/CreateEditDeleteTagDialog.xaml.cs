using System.Drawing;
using EFCore.BulkExtensions;
using MediaMaster.DataBase;
using MediaMaster.Extensions;
using MediaMaster.Interfaces.Services;
using MediaMaster.Services;
using MediaMaster.ViewModels.Dialog;
using Microsoft.EntityFrameworkCore;
using WinUI3Localizer;

namespace MediaMaster.Views.Dialog;

public sealed partial class CreateEditDeleteTagDialog : Page
{
    private readonly Tag? _currentTag;
    public EditTagDialogViewModel ViewModel = new();

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
            _currentTag = tagParam;
        }
        else if (tagParam != null)
        {
            _ = TagView.UpdateItemSource(tagParam.Parents);
        }

        if (tagParam == null)
        {
            Windows.UI.Color emptyColor = Windows.UI.Color.FromArgb(255, 255, 255, 255);
            ViewModel.Color = emptyColor;
            return;
        }

        AliasesListView.Strings = tagParam.Aliases;
        ViewModel.Name = tagParam.Name;
        ViewModel.Shorthand = tagParam.Shorthand;
        ViewModel.Color = tagParam.Color.ToWindowsColor();
        ViewModel.SetPermissions(tagParam.Permissions);

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
        var badContrast = color
                              .GetBackgroundColor(ElementTheme.Dark)
                              .CalculateContrastRatio(color.CalculateColorText(ElementTheme.Dark)) < 4.5
                          && color
                              .GetBackgroundColor(ElementTheme.Light)
                              .CalculateContrastRatio(color.CalculateColorText(ElementTheme.Light)) < 4.5;

        ContrastIcon.Visibility = badContrast ? Visibility.Visible : Visibility.Collapsed;
    }

    public static async Task<(ContentDialogResult, CreateEditDeleteTagDialog?)> ShowDialogAsync(XamlRoot xamlRoot, int? tagId = null,
        Tag? tag = null)
    {
        if (App.MainWindow == null) return (ContentDialogResult.None, null);

        var tagDialog = new CreateEditDeleteTagDialog(tagId, tag);
        ContentDialog dialog = new()
        {
            XamlRoot = xamlRoot,
            DefaultButton = ContentDialogButton.Primary,
            Content = tagDialog,
            RequestedTheme = App.GetService<IThemeSelectorService>().ActualTheme
        };

        Uids.SetUid(dialog, tagId == null ? "/Tag/CreateDialog" : "/Tag/EditDialog");
        if (tagDialog._currentTag?.Permissions.HasFlag(TagPermissions.CannotDelete) is true)
        {
            dialog.SecondaryButtonText = "";
        }

        App.GetService<IThemeSelectorService>().ThemeChanged += (_, theme) => dialog.RequestedTheme = theme;
        ContentDialogResult? secondaryResult;
        ContentDialogResult result;
        do
        {
            if (xamlRoot != App.MainWindow.Content.XamlRoot)
            {
                result = await dialog.ShowAndEnqueueAsync(true);
            }
            else
            {
                result = await dialog.ShowAndEnqueueAsync();
            }
            secondaryResult = null;

            switch (result)
            {
                case ContentDialogResult.Primary:
                {
                    await using (var database = new MediaDbContext())
                    {
                        var foundTag = await database.Tags.Select(t => t.Name).FirstOrDefaultAsync(id => id == tagDialog.ViewModel.Name);
                        if (foundTag != null)
                        {

                            ContentDialog tagAlreadyExistsDialog = new()
                            {
                                XamlRoot = xamlRoot,
                                DefaultButton = ContentDialogButton.Close,
                                RequestedTheme = App.GetService<IThemeSelectorService>().ActualTheme
                            };
                            Uids.SetUid(tagAlreadyExistsDialog, "/Tag/TagAlreadyExistsDialog");
                            App.GetService<IThemeSelectorService>().ThemeChanged += (_, theme) => tagAlreadyExistsDialog.RequestedTheme = theme;
                            if (xamlRoot != App.MainWindow.Content.XamlRoot)
                            {
                                secondaryResult = await tagAlreadyExistsDialog.ShowAndEnqueueAsync(true);
                            }
                            else
                            {
                                secondaryResult = await tagAlreadyExistsDialog.ShowAndEnqueueAsync();
                            }

                            if (secondaryResult == ContentDialogResult.Primary)
                            {
                                await tagDialog.SaveChangesAsync();
                            }
                        }
                        else
                        {
                            await tagDialog.SaveChangesAsync();
                        }
                    }
                    break;
                }
                case ContentDialogResult.Secondary:
                {
                    if (tagId != null)
                    {
                        secondaryResult = await DeleteTag((int)tagId, xamlRoot);
                    }

                    break;
                }
            }
        } while (secondaryResult == ContentDialogResult.None);

        return (result, tagDialog);
    }

    public static async Task<ContentDialogResult> DeleteTag(int tagId, XamlRoot xamlRoot)
    {
        if (App.MainWindow == null) return ContentDialogResult.None;

        ContentDialog dialog = new()
        {
            XamlRoot = xamlRoot,
            DefaultButton = ContentDialogButton.Close,
            RequestedTheme = App.GetService<IThemeSelectorService>().ActualTheme
        };
        Uids.SetUid(dialog, "/Tag/DeleteDialog");
        App.GetService<IThemeSelectorService>().ThemeChanged += (_, theme) => dialog.RequestedTheme = theme;

        ContentDialogResult result;
        if (xamlRoot != App.MainWindow.Content.XamlRoot)
        {
            result = await dialog.ShowAndEnqueueAsync(true);
        }
        else
        {
            result = await dialog.ShowAndEnqueueAsync();
        }

        if (result == ContentDialogResult.Primary)
        {
            await using (var database = new MediaDbContext())
            {
                await database.Tags.Where(t => t.TagId == tagId).ExecuteDeleteAsync();
            }
        }

        return result;
    }

    private async Task SaveChangesAsync()
    {
        await using (var database = new MediaDbContext())
        {
            await Transaction.Try(database, async () =>
            {
                Tag? trackedTag;
                if (_currentTag == null)
                {
                    trackedTag = new Tag();
                }
                else
                {
                    trackedTag = await database.Tags
                        .AsTracking()
                        .Include(m => m.Parents)
                        .FirstOrDefaultAsync(t => t.TagId == _currentTag.TagId);
                }

                if (trackedTag != null)
                {
                    trackedTag.Name = ViewModel.Name;
                    trackedTag.Shorthand = ViewModel.Shorthand;

                    trackedTag.FirstParentReferenceName =
                        TagView.GetItemSource().MinBy(t => t.Name)?.ReferenceName ?? "";

                    trackedTag.Color = ViewModel.Color.ToSystemColor();
                    trackedTag.Aliases = [.. AliasesListView.Strings];

                    if (_currentTag == null)
                    {
                        await database.Tags.AddAsync(trackedTag);
                    }

                    await database.SaveChangesAsync();


                    HashSet<int> currentTagIds = trackedTag.Parents.Select(t => t.TagId).ToHashSet();
                    HashSet<int> selectedTagIds = TagView.GetItemSource().Select(t => t.TagId).ToHashSet();

                    HashSet<int> tagsToAdd = selectedTagIds.Except(currentTagIds).ToHashSet();
                    HashSet<int> tagsToRemove = currentTagIds.Except(selectedTagIds).ToHashSet();

                    // Bulk add new tags
                    if (tagsToAdd.Count != 0)
                    {
                        List<TagTag> newTagTags = tagsToAdd.Select(tagId => new TagTag
                            { ParentsTagId = tagId, ChildrenTagId = trackedTag.TagId }).ToList();
                        await database.BulkInsertAsync(newTagTags);
                    }

                    // Bulk remove old tags
                    if (tagsToRemove.Count != 0)
                    {
                        List<TagTag> tagTagsToRemove = await database.TagTags
                            .Where(t => t.ChildrenTagId == trackedTag.TagId && tagsToRemove.Contains(t.ParentsTagId))
                            .ToListAsync();
                        await database.BulkDeleteAsync(tagTagsToRemove);
                    }
                }
            });
        }
    }
}