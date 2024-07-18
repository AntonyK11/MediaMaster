using System.Collections;
using System.Collections.ObjectModel;
using Windows.Foundation;
using MediaMaster.DataBase;
using MediaMaster.DataBase.Models;
using MediaMaster.Services;
using MediaMaster.Views.Dialog;
using Microsoft.EntityFrameworkCore;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;

namespace MediaMaster.Controls;


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

        CustomItemsView.SelectItemsInvoked += (_, _) => TagsSelected();
        CustomItemsView.RemoveItemsInvoked += (_, tagObjectId) => TagRemoved((int)tagObjectId);
    }

    private async void TagsSelected()
    {
        List<int> tagIds = Tags.Select(t => t.TagId).ToList();
        (ContentDialogResult result, SelectTagsDialog? selectTagsDialog) = await SelectTagsDialog.ShowDialogAsync(tagIds, TagId != null ? [(int)TagId] : [], MediaId == null);

        if (selectTagsDialog != null)
        {
            if (result == ContentDialogResult.Primary)
            {
                await UpdateItemSource(selectTagsDialog.SelectedTags);
            }
            else
            {
                await UpdateItemSource(selectTagsDialog.Tags.Where(t => tagIds.Contains(t.TagId)).ToList());
            }
        }

        SelectTagsInvoked?.Invoke(this, tagIds);
    }

    private async void TagRemoved(int tagId)
    {
        var tag = GetItemSource().FirstOrDefault(t => t.TagId == tagId);

        if (tag == null) return;

        if (MediaId == null || !(tag.Flags.HasFlag(TagFlags.Extension) || tag.Flags.HasFlag(TagFlags.Website)))
        {
            var tagToRemove = Tags.FirstOrDefault(t => t.TagId == tag.TagId);
            if (tagToRemove == null) return;
            Tags.Remove(tagToRemove);
            RemoveTagsInvoked?.Invoke(this, tag);
            await UpdateItemSource(Tags);
        }
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
        else
        {
            Tags = tags.ToList();
        }

        const int tagCheckLimit = 100;
        var showExtensions = App.GetService<SettingsService>().ShowExtensions;

        ICollection<Tag> filteredTags = Tags.Where(t => showExtensions || !t.Flags.HasFlag(TagFlags.Extension)).ToList();
        ICollection<Tag> itemSource = CustomItemsView.ItemsSource.OfType<Tag>().ToList();

        ICollection<Tag>  tagsToRemove = itemSource.Except(filteredTags, TagComparer.Instance).Take(tagCheckLimit).ToList();
        ICollection<Tag>  tagsToAdd = filteredTags.Except(itemSource, TagComparer.Instance).Take(tagCheckLimit).ToList();

        if (tagsToAdd.Count >= tagCheckLimit || tagsToRemove.Count >= tagCheckLimit)
        {
            CustomItemsView.ItemsSource = new ObservableCollection<object>(filteredTags);
        }
        else
        {
            foreach (var tag in tagsToRemove)
            {
                CustomItemsView.ItemsSource.Remove(tag);
            }
        
            foreach (var tag in tagsToAdd)
            {
                CustomItemsView.ItemsSource.Add(tag);
            }
        }
    }

    private void RemoveTagFlyout_OnDataContextChanged(FrameworkElement sender, DataContextChangedEventArgs args)
    {
        var tagId = (int)sender.DataContext;
        var tag = GetItemSource().FirstOrDefault(t => t.TagId == tagId);

        if (tag == null) return;

        if (MediaId == null || !(tag.Flags.HasFlag(TagFlags.Extension) || tag.Flags.HasFlag(TagFlags.Website)))
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
        if (MediaId == null || !(tag.Flags.HasFlag(TagFlags.Extension) || tag.Flags.HasFlag(TagFlags.Website)))
        {
            ((CustomItemContainer)sender).DeleteButtonVisibility = Visibility.Visible;
        }
        else
        {
            ((CustomItemContainer)sender).DeleteButtonVisibility = Visibility.Collapsed;
        }
    }
}

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

        Tag? tag1 = x as Tag;
        Tag? tag2 = y as Tag;

        if (tag1 == tag2) return 0;

        var cx = tag1?.Name as IComparable;
        var cy = tag2?.Name as IComparable;

        return cx == null ? -1 : cy == null ? +1 : cx.CompareTo(cy);
    }
}

public class TagComparer : IEqualityComparer<Tag>
{
    public static readonly IEqualityComparer<Tag> Instance = new TagComparer();

    public bool Equals(Tag? x, Tag? y)
    {
        if (x == y) return true;
        if (x == null || y == null) return false;
        return x.TagId == y.TagId && x.Name == y.Name && x.Flags == y.Flags && x.Permissions == y.Permissions && x.DisplayName == y.DisplayName;
    }

    public int GetHashCode(Tag obj)
    {
        return HashCode.Combine(obj.TagId, obj.Name, obj.Flags, obj.Permissions, obj.DisplayName);
    }
}