using CommunityToolkit.WinUI.Collections;
using MediaMaster.DataBase;
using MediaMaster.DataBase.Models;
using MediaMaster.Extensions;
using MediaMaster.Interfaces.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using WinUI3Localizer;

namespace MediaMaster.Views.Dialog;

public sealed partial class SelectTagsDialog : Page
{
    public ICollection<Tag> SelectedTags { get; private set; } = [];
    
    private readonly bool _showExtensions;

    private readonly ICollection<int> _tagsToExclude;

    private AdvancedCollectionView? _advancedCollectionView;
    private ICollection<Tag> _tags = [];
    private bool _watchForSelectionChange = true;

    public SelectTagsDialog(ICollection<int>? selectedTags = null, ICollection<int>? tagsToExclude = null,
        bool showExtensions = true)
    {
        InitializeComponent();

        _showExtensions = showExtensions;
        _tagsToExclude = tagsToExclude ?? [];

        UpdateItemSource(selectedTags);
    }

    private void UpdateItemSource(ICollection<int>? selectedTags = null)
    {
        _watchForSelectionChange = false;

        SetupTags(selectedTags ?? SelectedTags.Select(t => t.TagId).ToList());
        SelectTags();
        TextBox_TextChanged(null, null);

        _watchForSelectionChange = true;
    }

    private async void SetupTags(ICollection<int> selectedTags)
    {
        await using (var database = new MediaDbContext())
        {
            _tags = database.Tags.Where(tag => !_tagsToExclude.Contains(tag.TagId)).ToList();
        }

        _advancedCollectionView = new AdvancedCollectionView(_tags.Where(t => !(t.Flags.HasFlag(TagFlags.Extension) && !_showExtensions)).ToList(), true);
        _advancedCollectionView.SortDescriptions.Add(new SortDescription("Name", SortDirection.Ascending));
        ListView.ItemsSource = _advancedCollectionView;

        SelectedTags = _tags.Where(t => selectedTags.Contains(t.TagId)).ToList();
    }

    private void SelectTags()
    {
        foreach (Tag? tag in ((ICollection<object>)ListView.ItemsSource).Where(o => o is Tag).Cast<Tag>())
        {
            if (SelectedTags.Select(t => t.TagId).Contains(tag.TagId))
            {
                if (!ListView.SelectedItems.Contains(tag))
                {
                    ListView.SelectedItems.Add(tag);
                }
            }
            else
            {
                if (ListView.SelectedItems.Contains(tag))
                {
                    ListView.SelectedItems.Remove(tag);
                }
            }
        }
    }

    private void TextBox_TextChanged(object? sender, TextChangedEventArgs? args)
    {
        List<object> selection = ListView.SelectedItems.ToList();

        var splitText = TextBox.Text.Trim().Split(" ");
        if (_advancedCollectionView != null)
        {
            _advancedCollectionView.Filter = x =>
            {
                if (x is Tag tag)
                {
                    return splitText.All(key =>
                        tag.Name.Contains(key, StringComparison.CurrentCultureIgnoreCase) ||
                        tag.Shorthand.Contains(key, StringComparison.CurrentCultureIgnoreCase) ||
                        tag.Aliases.Any(a => a.Contains(key, StringComparison.CurrentCultureIgnoreCase)));
                }

                return false;
            };
        }

        foreach (var tag in selection)
        {
            ListView.SelectedItems.Add(tag);
        }
    }

    private void ListView_OnSelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (_watchForSelectionChange)
        {
            foreach (Tag tag in e.AddedItems.Where(t => !SelectedTags.Contains(t)))
            {
                SelectedTags.Add(tag);
            }

            foreach (var tagId in e.RemovedItems.Where(t => t is Tag).Cast<Tag>().Select(t => t.TagId))
            {
                SelectedTags.Remove(SelectedTags.FirstOrDefault(t => t.TagId == tagId) ?? new Tag());
            }
        }
    }

    private async void EditTagFlyout_OnClick(object sender, RoutedEventArgs e)
    {
        var tagId = (int)((FrameworkElement)sender).DataContext;
        await CreateEditDeleteTagDialog.ShowDialogAsync(tagId);

        UpdateItemSource();
    }

    private async void DuplicateTagFlyout_OnClick(object sender, RoutedEventArgs e)
    {
        var tagId = (int)((FrameworkElement)sender).DataContext;

        Tag? tag;
        await using (var database = new MediaDbContext())
        {
            tag = await database.Tags.Include(t => t.Parents).FirstOrDefaultAsync(t => t.TagId == tagId);
            if (tag == null) return;
        }

        tag.Permissions = 0;
        await CreateEditDeleteTagDialog.ShowDialogAsync(tag: tag);

        UpdateItemSource();
    }

    private async void DeleteTagFlyout_OnClick(object sender, RoutedEventArgs e)
    {
        var tagId = (int)((FrameworkElement)sender).DataContext;
        ContentDialogResult result = await CreateEditDeleteTagDialog.DeleteTag(tagId);

        if (result == ContentDialogResult.Primary)
        {
            UpdateItemSource();
        }
    }

    public static async Task<(ContentDialogResult, SelectTagsDialog?)> ShowDialogAsync(
        ICollection<int>? selectedTags = null, ICollection<int>? tagsToExclude = null, bool showExtensions = true)
    {
        if (App.MainWindow == null) return (ContentDialogResult.None, null);

        var selectTagsDialog = new SelectTagsDialog(selectedTags, tagsToExclude, showExtensions);
        ContentDialog dialog = new()
        {
            XamlRoot = App.MainWindow.Content.XamlRoot,
            DefaultButton = ContentDialogButton.Primary,
            Content = selectTagsDialog
        };
        Uids.SetUid(dialog, "/Tag/SelectDialog");
        dialog.RequestedTheme = App.GetService<IThemeSelectorService>().ActualTheme;
        App.GetService<IThemeSelectorService>().ThemeChanged += (_, theme) => { dialog.RequestedTheme = theme; };

        ContentDialogResult result;
        do
        {
            result = await dialog.ShowAndEnqueueAsync();

            if (result == ContentDialogResult.Secondary)
            {
                (ContentDialogResult createResult, _) = await CreateEditDeleteTagDialog.ShowDialogAsync();

                if (createResult == ContentDialogResult.Primary)
                {
                    selectTagsDialog.UpdateItemSource();
                }
            }
        } while (result == ContentDialogResult.Secondary);

        return (result, selectTagsDialog);
    }
}