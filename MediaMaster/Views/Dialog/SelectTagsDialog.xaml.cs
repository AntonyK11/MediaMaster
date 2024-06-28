using Windows.System;
using CommunityToolkit.WinUI.Collections;
using MediaMaster.DataBase;
using MediaMaster.DataBase.Models;
using MediaMaster.Extensions;
using MediaMaster.Interfaces.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Input;
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
        ItemsView.ItemsSource = _advancedCollectionView;

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

    private void ListView_OnSelectionChanged(ItemsView itemsView, ItemsViewSelectionChangedEventArgs args)
    {
        if (_watchForSelectionChange)
        {
            SelectedTags = itemsView.SelectedItems.OfType<Tag>().ToList();
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

    private void ItemsView_OnProcessKeyboardAccelerators(UIElement sender, ProcessKeyboardAcceleratorEventArgs args)
    {
        if (args is { Modifiers: VirtualKeyModifiers.Control, Key: VirtualKey.A })
        {
            args.Handled = true;
            var oldSelectionCount = ((ItemsView)sender).SelectedItems.Count;
            ((ItemsView)sender).SelectAll();
            var newSelectionCount = ((ItemsView)sender).SelectedItems.Count;
            if (oldSelectionCount == newSelectionCount)
            {
                ((ItemsView)sender).DeselectAll();
            }
        }
    }

    private async void FrameworkElement_OnDataContextChanged(FrameworkElement sender, DataContextChangedEventArgs args)
    {
        _watchForSelectionChange = false;
        var itemContainer = (ItemContainer)sender;
        var tagId = (int)itemContainer.DataContext;
        itemContainer.IsSelected = SelectedTags.Select(t => t.TagId).Contains(tagId);
        await Task.Delay(1);
        _watchForSelectionChange = true;
    }
}