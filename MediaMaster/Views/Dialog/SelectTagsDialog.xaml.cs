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
    public ICollection<Tag> SelectedTags { get; set; } = [];
    
    private readonly ICollection<int> _tagsToExclude = [];

    private AdvancedCollectionView? _advancedCollectionView;
    private ICollection<Tag> _tags = [];
    private bool _watchForSelectionChange = true;

    public SelectTagsDialog(ICollection<int>? selectedTags = null, ICollection<int>? tagsToExclude = null)
    {
        InitializeComponent();

        //using (var database = new MediaDbContext())
        //{
        //    _tags = database.Tags.ToList();
        //}

        if (tagsToExclude != null)
        {
            _tagsToExclude = tagsToExclude;
        }

        SetupTags(selectedTags);

        SelectTags();

        ListView.SelectionChanged += ListViewOnSelectionChanged;
    }

    private async void SetupTags(ICollection<int>? selectedTags)
    {
        _watchForSelectionChange = false;
        await using (var database = new MediaDbContext())
        {
            _tags = database.Tags.Where(tag => !_tagsToExclude.Contains(tag.TagId)).ToList();
        }

        _advancedCollectionView = new AdvancedCollectionView(_tags.ToList(), true);
        _advancedCollectionView.SortDescriptions.Add(new SortDescription("Name", SortDirection.Ascending));
        ListView.ItemsSource = _advancedCollectionView;

        if (selectedTags != null)
        {
            SelectedTags = _tags.Where(t => selectedTags.Contains(t.TagId)).ToList();
        }

        _watchForSelectionChange = true;
    }

    private void SelectTags()
    {
        _watchForSelectionChange = false;
        foreach (Tag? tag in ((ICollection<object>)ListView.ItemsSource).Cast<Tag>())
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
        _watchForSelectionChange = true;
    }

    private void ListViewOnSelectionChanged(object sender, SelectionChangedEventArgs e)
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

    private void TextBox_TextChanged(object? sender, TextChangedEventArgs? args)
    {
        _watchForSelectionChange = false;
        List<object> selection = ListView.SelectedItems.ToList();

        var splitText = TextBox.Text.ToLower().Trim().Split(" ");
        if (_advancedCollectionView != null)
        {
            _advancedCollectionView.Filter = x =>
            {
                if (x is Tag tag)
                {
                    return splitText.All(key =>
                        tag.Name.Contains(key, StringComparison.CurrentCultureIgnoreCase) ||
                        (tag.Shorthand != null && tag.Shorthand.Contains(key, StringComparison.CurrentCultureIgnoreCase)) ||
                        tag.Aliases.Select(a => a.ToLower()).Contains(key.ToLower()));
                }

                return false;
            };
        }

        foreach (var tag in selection)
        {
            ListView.SelectedItems.Add(tag);
        }
        _watchForSelectionChange = true;
    }

    private async void EditTagFlyout_OnClick(object sender, RoutedEventArgs e)
    {
        var tagId = (int)((FrameworkElement)sender).Tag;
        await CreateEditDeleteTagDialog.ShowDialogAsync(tagId);

        SetupTags(SelectedTags.Select(t => t.TagId).ToList());
        SelectTags();
        TextBox_TextChanged(null, null);
    }

    public static async Task<(ContentDialogResult, SelectTagsDialog?)> ShowDialogAsync(
        ICollection<int>? selectedTags = null, ICollection<int>? tagsToExclude = null)
    {
        if (App.MainWindow == null) return (ContentDialogResult.None, null);

        var selectTagsDialog = new SelectTagsDialog(selectedTags, tagsToExclude);
        ContentDialog dialog = new()
        {
            XamlRoot = App.MainWindow.Content.XamlRoot,
            DefaultButton = ContentDialogButton.Primary,
            Content = selectTagsDialog
        };
        Uids.SetUid(dialog, "Select_Tags_Dialog");
        dialog.RequestedTheme = App.GetService<IThemeSelectorService>().Theme;
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
                    selectTagsDialog.SetupTags(selectTagsDialog.SelectedTags.Select(t => t.TagId).ToList());
                    selectTagsDialog.SelectTags();
                    selectTagsDialog.TextBox_TextChanged(null, null);
                }
            }
        } while (result == ContentDialogResult.Secondary);

        return (result, selectTagsDialog);
    }

    private async void DeleteTagFlyout_OnClick(object sender, RoutedEventArgs e)
    {
        var tagId = (int)((FrameworkElement)sender).Tag;
        var result = await CreateEditDeleteTagDialog.DeleteTag(tagId);

        if (result == ContentDialogResult.Primary)
        {
            SetupTags(SelectedTags.Select(t => t.TagId).ToList());
            SelectTags();
            TextBox_TextChanged(null, null);
        }
    }
}