using CommunityToolkit.WinUI.Collections;
using MediaMaster.DataBase;
using MediaMaster.DataBase.Models;
using MediaMaster.Extensions;
using MediaMaster.Interfaces.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Data;
using WinUI3Localizer;

namespace MediaMaster.Views.Dialog;

public sealed partial class SelectTagsDialog : Page
{
    public ICollection<Tag> Tags { get; private set; } = [];
    public ICollection<Tag> SelectedTags { get; private set; } = [];
    
    private readonly bool _showExtensions;

    private readonly ICollection<int> _tagsToExclude;

    private AdvancedCollectionView? _advancedCollectionView;
    private bool _watchForSelectionChange = true;

    public SelectTagsDialog(ICollection<int>? selectedTags = null, ICollection<int>? tagsToExclude = null,
        bool showExtensions = true)
    {
        InitializeComponent();

        _showExtensions = showExtensions;
        _tagsToExclude = tagsToExclude ?? [];

        UpdateItemSource(selectedTags);
    }

    private async void UpdateItemSource(ICollection<int>? selectedTags = null)
    {
        await SetupTags(selectedTags ?? SelectedTags.Select(t => t.TagId).ToList());

        if (_advancedCollectionView != null)
        {
            SelectTags(_advancedCollectionView.OfType<Tag>().ToList(), SelectedTags);
        }

        TextBox_TextChanged(null, null);
    }

    private async Task SetupTags(ICollection<int> selectedTags)
    {
        _watchForSelectionChange = false;
        await using (var database = new MediaDbContext())
        {
            Tags = database.Tags.Where(tag => !_tagsToExclude.Contains(tag.TagId)).ToList();
        }

        _advancedCollectionView = new AdvancedCollectionView(Tags.Where(t => _showExtensions || !t.Flags.HasFlag(TagFlags.Extension)).ToList());
        _advancedCollectionView.SortDescriptions.Add(new SortDescription("Name", SortDirection.Ascending));
        ListView.ItemsSource = _advancedCollectionView;

        SelectedTags = Tags.Where(t => selectedTags.Contains(t.TagId)).ToList();

        _watchForSelectionChange = true;
    }

    private void SelectTags(IList<Tag> tags, ICollection<Tag> selectedTags)
    {
        _watchForSelectionChange = false;

        var selectedTagIds = selectedTags.Select(t => t.TagId).ToHashSet();
        var selectedIndexes = new List<int>();

        for (var i = 0; i < tags.Count; i++)
        {
            if (selectedTagIds.Contains(tags[i].TagId))
            {
                selectedIndexes.Add(i);
            }
        }

        var rangesToSelect = FindContiguousRanges(selectedIndexes);

        foreach (var range in rangesToSelect)
        {
            ListView.SelectRange(range);
        }

        _watchForSelectionChange = true;
    }

    private static List<ItemIndexRange> FindContiguousRanges(List<int> indexes)
    {
        var ranges = new List<ItemIndexRange>();
        if (indexes.Count == 0) return ranges;

        indexes.Sort();
        var start = indexes[0];

        for (var i = 0; i < indexes.Count - 1; i++)
        {
            var current = indexes[i];
            var next = indexes[i + 1];

            if (current == next - 1)
            {
                continue;
            }
            
            ranges.Add(new ItemIndexRange(start, (uint)(current - start + 1)));
            start = next;
        }
        ranges.Add(new ItemIndexRange(start, (uint)(indexes.Last() - start + 1)));
        return ranges;
    }
    
    private void TextBox_TextChanged(object? sender, TextChangedEventArgs? args)
    {
        _watchForSelectionChange = false;

        var splitText = TextBox.Text.Trim().Split(" ");
        if (_advancedCollectionView != null)
        {
            using (_advancedCollectionView.DeferRefresh())
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
        }

        _watchForSelectionChange = true;
    }

    private void ListView_OnSelectionChanged(object sender, SelectionChangedEventArgs args)
    {
        if (_watchForSelectionChange)
        {
            foreach (var tag in args.RemovedItems.OfType<Tag>())
            {
                SelectedTags.Remove(tag);
            }

            foreach (var tag in args.AddedItems.OfType<Tag>())
            {
                SelectedTags.Add(tag);
            }
        }
    }

    private async void EditTagFlyout_OnClick(object sender, RoutedEventArgs e)
    {
        var tag = (Tag)((FrameworkElement)sender).DataContext;
        await CreateEditDeleteTagDialog.ShowDialogAsync(tag.TagId);

        UpdateItemSource();
    }

    private async void DuplicateTagFlyout_OnClick(object sender, RoutedEventArgs e)
    {
        var tag = (Tag)((FrameworkElement)sender).DataContext;

        await using (var database = new MediaDbContext())
        {
            tag = await database.Tags.Include(t => t.Parents).FirstOrDefaultAsync(t => t.TagId == tag.TagId);
            if (tag == null) return;
        }

        tag.Permissions = 0;
        await CreateEditDeleteTagDialog.ShowDialogAsync(tag: tag);

        UpdateItemSource();
    }

    private async void DeleteTagFlyout_OnClick(object sender, RoutedEventArgs e)
    {
        var tag = (Tag)((FrameworkElement)sender).DataContext;
        ContentDialogResult result = await CreateEditDeleteTagDialog.DeleteTag(tag.TagId);

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