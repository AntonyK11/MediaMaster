using System.Collections;
using Windows.Foundation;
using DependencyPropertyGenerator;
using MediaMaster.DataBase;
using MediaMaster.Interfaces.Services;
using MediaMaster.Services;
using MediaMaster.Views.Dialog;
using Microsoft.EntityFrameworkCore;

namespace MediaMaster.Controls;

[DependencyProperty("SelectionMode", typeof(ItemsViewSelectionMode), DefaultValue = ItemsViewSelectionMode.None)]
[DependencyProperty("AddTagButton", typeof(bool), DefaultValue = true)]
[DependencyProperty("ShowScrollButtons", typeof(bool), DefaultValue = true)]
[DependencyProperty("Layout", typeof(Layout), DefaultValueExpression = "new StackLayout { Orientation = Orientation.Horizontal, Spacing = 8 }")]
[DependencyProperty("MediaIds", typeof(HashSet<int>), DefaultValueExpression = "new HashSet<int>()")]
[DependencyProperty("TagId", typeof(int?))]
[DependencyProperty("Tags", typeof(ICollection<Tag>), DefaultValueExpression = "new List<Tag>()")]
public partial class TagView : UserControl
{
    private bool _skipTagsChange;

    public TagView()
    {
        InitializeComponent();
        CustomItemsView.Comparer = TagsComparer.Instance;
        _ = UpdateItemSource();

        CustomItemsView.SelectItemsInvoked += (_, _) => TagsSelected();
        CustomItemsView.RemoveItemsInvoked += (_, tagObjectId) => TagRemoved((int)tagObjectId);

        App.GetService<IThemeSelectorService>().ThemeChanged += (_, _) =>
        {
            CustomItemsView.ItemsSource.Clear();
            CustomItemsView.ItemsSource = new ObservableCollection<object>(Tags);
        };
    }

    async partial void OnMediaIdsChanged()
    {
        await UpdateItemSource();
    }

    async partial void OnTagIdChanged()
    {
        await UpdateItemSource(refreshAll: true);
    }

    public event TypedEventHandler<object, Tag>? RemoveTagsInvoked;
    public event TypedEventHandler<object, ICollection<int>>? SelectTagsInvoked;

    async partial void OnTagsChanged()
    {
        if (!_skipTagsChange)
        {
            await UpdateItemSource();
        }
    }

    private async void TagsSelected()
    {
        HashSet<int> tagIds = Tags.Select(t => t.TagId).ToHashSet();
        (ContentDialogResult result, TagsListDialog? tagsListDialog) =
            await TagsListDialog.ShowDialogAsync(this.XamlRoot,
                tagIds,
                TagId != null ? [(int)TagId] : [],
                MediaIds.Count == 0);

        if (tagsListDialog != null)
        {
            if (result == ContentDialogResult.Primary)
            {
                await UpdateItemSource(tagsListDialog.SelectedTags);
            }
            else
            {
                await UpdateItemSource(tagsListDialog.Tags.Where(t => tagIds.Contains(t.TagId)).ToList());
            }
        }

        SelectTagsInvoked?.Invoke(this, tagIds);
    }

    private async void TagRemoved(int tagId)
    {
        Tag? tag = GetItemSource().FirstOrDefault(t => t.TagId == tagId);

        if (tag == null) return;

        if (MediaIds.Count == 0 || !(tag.Flags.HasFlag(TagFlags.Extension) || tag.Flags.HasFlag(TagFlags.Website)))
        {
            Tag? tagToRemove = Tags.FirstOrDefault(t => t.TagId == tag.TagId);
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
        await CreateEditDeleteTagDialog.ShowDialogAsync(this.XamlRoot, tagId);

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
        await CreateEditDeleteTagDialog.ShowDialogAsync(this.XamlRoot, tag: tag);

        await UpdateItemSource();
    }

    private async void DeleteTagFlyout_OnClick(object sender, RoutedEventArgs e)
    {
        var tagId = (int)((FrameworkElement)sender).DataContext;
        ContentDialogResult result = await CreateEditDeleteTagDialog.DeleteTag(tagId, this.XamlRoot);

        if (result == ContentDialogResult.Primary)
        {
            await UpdateItemSource();
        }
    }

    public async Task UpdateItemSource(ICollection<Tag>? tags = null, bool refreshAll = false)
    {
        _skipTagsChange = true;
        if (tags == null)
        {
            await using (MediaDbContext database = new())
            {
                if (MediaIds.Count != 0)
                {
                    if (!(MediaIds.Count == 1 && MediaIds.First() == -1))
                    {
                        Tags = await database.Medias
                            .Where(m => MediaIds.Contains(m.MediaId))
                            .SelectMany(m => m.Tags)
                            .GroupBy(t => t)
                            .Where(g => g.Count() == MediaIds.Count)
                            .Select(g => g.Key)
                            .ToListAsync();
                    }
                }
                else if (TagId != null)
                {
                    if (refreshAll)
                    {
                        Tags = database.Tags
                            .Select(t => new { t.TagId, t.Parents })
                            .FirstOrDefault(t => t.TagId == TagId)?.Parents.ToList() ?? [];
                    }
                    else if (GetItemSource().Count != 0)
                    {
                        HashSet<int> tagIds = GetItemSource().Select(t => t.TagId).ToHashSet();
                        Tags = database.Tags
                            .Where(tag => tagIds.Contains(tag.TagId))
                            .ToList();
                    }
                }
            }
        }
        else
        {
            Tags = tags.ToList();
        }

        const int tagCheckLimit = 100;
        var showExtensions = App.GetService<SettingsService>().ShowExtensions || MediaIds.Count == 0;

        ICollection<Tag> filteredTags = Tags.Where(t => showExtensions || !t.Flags.HasFlag(TagFlags.Extension)).ToList();
        ICollection<Tag> itemSource = CustomItemsView.ItemsSource.OfType<Tag>().ToList();

        ICollection<Tag> tagsToRemove = itemSource.Except(filteredTags, TagComparer.Instance).Take(tagCheckLimit).ToList();
        ICollection<Tag> tagsToAdd = filteredTags.Except(itemSource, TagComparer.Instance).Take(tagCheckLimit).ToList();

        if (tagsToAdd.Count >= tagCheckLimit || tagsToRemove.Count >= tagCheckLimit)
        {
            CustomItemsView.ItemsSource = new ObservableCollection<object>(filteredTags);
        }
        else
        {
            foreach (Tag tag in tagsToRemove)
            {
                CustomItemsView.ItemsSource.Remove(tag);
            }

            foreach (Tag tag in tagsToAdd)
            {
                CustomItemsView.ItemsSource.Add(tag);
            }
        }

        _skipTagsChange = false;

        UpdateLayout();
        CustomItemsView.UpdateScrollButtonsVisibility();
    }

    private void RemoveTagFlyout_OnDataContextChanged(FrameworkElement sender, DataContextChangedEventArgs args)
    {
        var tagId = (int)sender.DataContext;
        Tag? tag = GetItemSource().FirstOrDefault(t => t.TagId == tagId);

        if (tag == null) return;

        if (MediaIds.Count == 0 || !(tag.Flags.HasFlag(TagFlags.Extension) || tag.Flags.HasFlag(TagFlags.Website)))
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
        Tag? tag = GetItemSource().FirstOrDefault(t => t.TagId == tagId);

        if (tag == null) return;
        if (MediaIds.Count == 0 || !(tag.Flags.HasFlag(TagFlags.Extension) || tag.Flags.HasFlag(TagFlags.Website)))
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

        var tag1 = x as Tag;
        var tag2 = y as Tag;

        if (tag1 == tag2) return 0;

        IComparable? cx = tag1?.Name;
        IComparable? cy = tag2?.Name;

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
        return x.TagId == y.TagId && x.Name == y.Name && x.Argb == y.Argb && x.Flags == y.Flags &&
               x.Permissions == y.Permissions && x.DisplayName == y.DisplayName;
    }

    public int GetHashCode(Tag obj)
    {
        return HashCode.Combine(obj.TagId, obj.Name, obj.Argb, obj.Flags, obj.Permissions, obj.DisplayName);
    }
}