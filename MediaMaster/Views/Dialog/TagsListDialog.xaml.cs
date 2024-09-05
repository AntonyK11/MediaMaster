using System.Collections;
using CommunityToolkit.WinUI.Collections;
using MediaMaster.DataBase;
using MediaMaster.Extensions;
using MediaMaster.Interfaces.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.UI.Xaml.Data;
using WinUI3Localizer;

namespace MediaMaster.Views.Dialog;

public partial class TagsListDialog : Page
{
    private readonly bool _showExtensionsAndWebsites;

    private readonly ICollection<int> _tagsToExclude;

    private AdvancedCollectionView? _advancedCollectionView;
    private bool _watchForSelectionChange = true;

    public TagsListDialog(HashSet<int>? selectedTags = null, ICollection<int>? tagsToExclude = null,
        bool showExtensionsAndWebsites = true)
    {
        InitializeComponent();

        ListView.SelectionMode = selectedTags == null ? ListViewSelectionMode.None : ListViewSelectionMode.Multiple;

        _showExtensionsAndWebsites = showExtensionsAndWebsites;
        _tagsToExclude = tagsToExclude ?? [];

        UpdateItemSource(selectedTags);

        App.GetService<IThemeSelectorService>().ThemeChanged += (_, _) => UpdateItemSource();
    }

    public ICollection<Tag> Tags { get; private set; } = [];
    public ICollection<Tag> SelectedTags { get; private set; } = [];

    private async void UpdateItemSource(HashSet<int>? selectedTags = null)
    {
        await SetupTags(selectedTags ?? SelectedTags.Select(t => t.TagId).ToHashSet());
        TextBox_TextChanged(null, null);
    }

    private async Task SetupTags(HashSet<int> selectedTags)
    {
        _watchForSelectionChange = false;
        await using (var database = new MediaDbContext())
        {
            Tags = database.Tags.Where(tag => !_tagsToExclude.Contains(tag.TagId)).ToList();
        }

        _advancedCollectionView = new AdvancedCollectionView(
            Tags
                .Where(t => _showExtensionsAndWebsites ||
                            (!t.Flags.HasFlag(TagFlags.Extension) &&
                             !t.Flags.HasFlag(TagFlags.Website)))
                .ToList());

        _advancedCollectionView.SortDescriptions.Add(new SortDescription("DisplayName", SortDirection.Descending, TagsExtensionComparer.Instance));
        _advancedCollectionView.SortDescriptions.Add(new SortDescription("DisplayName", SortDirection.Ascending));
        ListView.ItemsSource = _advancedCollectionView;

        SelectedTags = Tags.Where(t => selectedTags.Contains(t.TagId)).ToList();

        _watchForSelectionChange = true;
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
                            tag.DisplayName.Contains(key, StringComparison.CurrentCultureIgnoreCase) ||
                            tag.Shorthand.Contains(key, StringComparison.CurrentCultureIgnoreCase) ||
                            tag.Aliases.Any(a => a.Contains(key, StringComparison.CurrentCultureIgnoreCase)));
                    }

                    return false;
                };
            }

            SelectTags(_advancedCollectionView.OfType<Tag>().ToList(), SelectedTags.Select(t => t.TagId).ToHashSet());
        }

        _watchForSelectionChange = true;
    }

    private void SelectTags(IList<Tag> tags, HashSet<int> selectedTags)
    {
        _watchForSelectionChange = false;

        var selectedIndexes = new List<int>();

        for (var i = 0; i < tags.Count; i++)
        {
            if (selectedTags.Contains(tags[i].TagId))
            {
                selectedIndexes.Add(i);
            }
        }

        List<ItemIndexRange> rangesToSelect = FindContiguousRanges(selectedIndexes);

        foreach (ItemIndexRange range in rangesToSelect)
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

    private void ListView_OnSelectionChanged(object sender, SelectionChangedEventArgs args)
    {
        if (_watchForSelectionChange)
        {
            foreach (Tag tag in args.RemovedItems.OfType<Tag>())
            {
                SelectedTags.Remove(tag);
            }

            foreach (Tag tag in args.AddedItems.OfType<Tag>())
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

    public static async Task<(ContentDialogResult, TagsListDialog?)> ShowDialogAsync(
        HashSet<int>? selectedTags = null, ICollection<int>? tagsToExclude = null,
        bool showExtensionsAndWebsites = true)
    {
        if (App.MainWindow == null) return (ContentDialogResult.None, null);

        var selectTagsDialog = new TagsListDialog(selectedTags, tagsToExclude, showExtensionsAndWebsites);
        ContentDialog dialog = new()
        {
            XamlRoot = App.MainWindow.Content.XamlRoot,
            DefaultButton = ContentDialogButton.Primary,
            Content = selectTagsDialog,
            RequestedTheme = App.GetService<IThemeSelectorService>().ActualTheme
        };

        Uids.SetUid(dialog, selectedTags == null ? "/Tag/ManageDialog" : "/Tag/SelectDialog");
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

public class TagsExtensionComparer : IComparer
{
    public static readonly IComparer Instance = new TagsExtensionComparer();

    public int Compare(object? x, object? y)
    {
        var name1 = x as string;
        var name2 = y as string;

        if (name1 == name2) return 0;

        if (name1 == null)
        {
            return -1;
        }

        if (name2 == null)
        {
            return 1;
        }

        if (name1.StartsWith('.'))
        {
            return name2.StartsWith('.') ? 0 : -1;
        }

        return name2.StartsWith('.') ? 1 : 0;
    }
}