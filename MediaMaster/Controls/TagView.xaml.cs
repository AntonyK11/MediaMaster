using System.Collections;
using Windows.Foundation;
using MediaMaster.DataBase;
using MediaMaster.DataBase.Models;
using MediaMaster.Services;
using MediaMaster.Views.Dialog;
using Microsoft.EntityFrameworkCore;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;

namespace MediaMaster.Controls;


public class TagsComparer : IComparer
{
    public static readonly IComparer Instance = new TagsComparer();

    public int Compare(object? x, object? y)
    {
        var result = ItemsComparer.Instance.Compare(x, y);

        if (result != 0)
        {
            return result;
        }

        Tag? tag1 = x is not Tag tag_1 ? null : tag_1;
        Tag? tag2 = y is not Tag tag_2 ? null : tag_2;

        var cx = tag1?.Name as IComparable;
        var cy = tag2?.Name as IComparable;

        // ReSharper disable once PossibleUnintendedReferenceComparison
        return cx == cy ? 0 : cx == null ? -1 : cy == null ? +1 : cx.CompareTo(cy);
    }
}

public sealed partial class TagView : UserControl
{
    public static readonly DependencyProperty SelectionModeProperty
        = DependencyProperty.Register(
            nameof(SelectionMode),
            typeof(ItemsViewSelectionMode),
            typeof(TagView),
            new PropertyMetadata(ItemsViewSelectionMode.None));

    public ItemsViewSelectionMode SelectionMode
    {
        get => (ItemsViewSelectionMode)GetValue(SelectionModeProperty);
        set => SetValue(SelectionModeProperty, value);
    }
    
    public static readonly DependencyProperty AddTagButtonProperty
        = DependencyProperty.Register(
            nameof(AddTagButton),
            typeof(bool),
            typeof(TagView),
            new PropertyMetadata(true));

    public bool AddTagButton
    {
        get => (bool)GetValue(AddTagButtonProperty);
        set => SetValue(AddTagButtonProperty, value);
    }
    
    public static readonly DependencyProperty ShowScrollButtonsProperty
        = DependencyProperty.Register(
            nameof(ShowScrollButtons),
            typeof(bool),
            typeof(TagView),
            new PropertyMetadata(true));

    public bool ShowScrollButtons
    {
        get => (bool)GetValue(ShowScrollButtonsProperty);
        set => SetValue(ShowScrollButtonsProperty, value);
    }
    
    public static readonly DependencyProperty LayoutProperty
        = DependencyProperty.Register(
            nameof(Layout),
            typeof(Layout),
            typeof(TagView),
            new PropertyMetadata(new StackLayout { Orientation = Orientation.Horizontal, Spacing = 8 }));

    public Layout Layout
    {
        get => (Layout)GetValue(LayoutProperty);
        set => SetValue(LayoutProperty, value);
    }
    
    public static readonly DependencyProperty MediaIdProperty
        = DependencyProperty.Register(
            nameof(MediaId),
            typeof(int?),
            typeof(TagView),
            new PropertyMetadata(null));

    public int? MediaId
    {
        get => (int?)GetValue(MediaIdProperty);
        set
        {
            SetValue(MediaIdProperty, value);
            _ = UpdateItemSource();
        }
    }
    
    public static readonly DependencyProperty TagIdProperty
        = DependencyProperty.Register(
            nameof(TagId),
            typeof(int?),
            typeof(TagView),
            new PropertyMetadata(null));

    public int? TagId
    {
        get => (int?)GetValue(TagIdProperty);
        set
        {
            SetValue(TagIdProperty, value);
            _ = UpdateItemSource(refreshAll: true);
        }
    }
    
    public event TypedEventHandler<object, Tag>? RemoveTagsInvoked;
    public event TypedEventHandler<object, ICollection<int>>? SelectTagsInvoked;

    public ICollection<Tag> Tags = [];
    
    public TagView()
    {
        InitializeComponent();
        CustomItemsView.Comparer = TagsComparer.Instance;
        _ = UpdateItemSource();

        CustomItemsView.SelectItemsInvoked += async (_, _) =>
        {
            List<int> tagIds = Tags.Select(t => t.TagId).ToList();
            (ContentDialogResult result, SelectTagsDialog? selectTagsDialog) = await SelectTagsDialog.ShowDialogAsync(tagIds, TagId != null ? [(int)TagId] : [], MediaId == null);

            if (selectTagsDialog != null)
            {
                await UpdateItemSource(selectTagsDialog.SelectedTags);
            }

            SelectTagsInvoked?.Invoke(this, tagIds);
        };

        CustomItemsView.RemoveItemsInvoked += async (_, tagObject) =>
        {
            var tagId = (int)tagObject;
            var tag = GetItemSource().FirstOrDefault(t => t.TagId == tagId);
            
            if (tag == null) return;
            
            if (!(tag.Flags.HasFlag(TagFlags.Extension) && MediaId != null))
            {
                var tagToRemove = Tags.FirstOrDefault(t => t.TagId == tag.TagId);
                if (tagToRemove == null) return;
                Tags.Remove(tagToRemove);
                RemoveTagsInvoked?.Invoke(this, tag);
                await UpdateItemSource(Tags);
            }
        };
    }

    public ICollection<Tag> GetItemSource()
    {
        return CustomItemsView.GetItemSource<Tag>();
    }

    private async void EditTagFlyout_OnClick(object sender, RoutedEventArgs e)
    {
        var tagId = (int)((FrameworkElement)sender).DataContext;
        await CreateEditDeleteTagDialog.ShowDialogAsync(tagId);

        await UpdateItemSource();
    }

    private void RemoveTagFlyout_OnClick(object sender, RoutedEventArgs e)
    {
        CustomItemsView.RemoveItem(((FrameworkElement)sender).DataContext);
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

        await UpdateItemSource();
    }

    private async void DeleteTagFlyout_OnClick(object sender, RoutedEventArgs e)
    {
        var tagId = (int)((FrameworkElement)sender).DataContext;
        var result = await CreateEditDeleteTagDialog.DeleteTag(tagId);

        if (result == ContentDialogResult.Primary)
        {
            await UpdateItemSource();
        }
    }

    public async Task UpdateItemSource(ICollection<Tag>? tags = null, bool refreshAll = false)
    {
        Tags = tags ?? [];
        if (tags == null)
        {
            await using (MediaDbContext dataBase = new())
            {
                if (MediaId != null)
                {
                    Tags = dataBase.Medias.Select(m => new { m.MediaId, m.Tags }).FirstOrDefault(m => m.MediaId == MediaId)?.Tags.ToList() ?? [];
                }
                else if (TagId != null)
                {
                    if (refreshAll)
                    {
                        Tags = dataBase.Tags.Select(t => new { t.TagId, t.Parents }).FirstOrDefault(t => t.TagId == TagId)?.Parents.ToList() ?? [];
                    }
                    else if (GetItemSource().Count != 0)
                    {
                        Tags = dataBase.Tags.Where(tag => GetItemSource().Select(t => t.TagId).Contains(tag.TagId)).ToList();
                    }
                }
            }
        }

        var showExtensions = App.GetService<SettingsService>().ShowExtensions;
        tags = Tags.Where(t => !(t.Flags.HasFlag(TagFlags.Extension) && !showExtensions)).OrderBy(e => e.Name).ToList();

        foreach (var obj in CustomItemsView.ItemsSource.ToList())
        {
            if (obj is not Tag tag) continue;

            var dbTag = tags.FirstOrDefault(t => t.TagId == tag.TagId);
            if (dbTag == null || tag.Name != dbTag.Name || tag.Flags != dbTag.Flags || tag.Permissions != dbTag.Permissions || tag.DisplayName != dbTag.DisplayName)
            {
                CustomItemsView.ItemsSource.Remove(tag);
            }
            else
            {
                tags.Remove(dbTag);
            }
        }
        
        foreach (var obj in tags)
        {
            CustomItemsView.ItemsSource.Add(obj);
        }
    }

    private void RemoveTagFlyout_OnDataContextChanged(FrameworkElement sender, DataContextChangedEventArgs args)
    {

        var tagId = (int)sender.DataContext;
        var tag = GetItemSource().FirstOrDefault(t => t.TagId == tagId);

        if (tag == null) return;

        if (!(tag.Flags.HasFlag(TagFlags.Extension) && MediaId != null))
        {
            sender.Visibility = Visibility.Visible;
        }
        else
        {
            sender.Visibility = Visibility.Collapsed;
        }
    }

    private void CustomItemContainer_Loaded(FrameworkElement sender, DataContextChangedEventArgs args)
    {
        var tagId = (int)sender.DataContext;
        var tag = GetItemSource().FirstOrDefault(t => t.TagId == tagId);

        if (tag == null) return;
        if (!(tag.Flags.HasFlag(TagFlags.Extension) && MediaId != null))
        {
            ((CustomItemContainer)sender).DeleteButtonVisibility = Visibility.Visible;
        }
        else
        {
            ((CustomItemContainer)sender).DeleteButtonVisibility = Visibility.Collapsed;
        }
    }
}