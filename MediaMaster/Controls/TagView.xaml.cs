using System.Collections.ObjectModel;
using Windows.Foundation;
using Windows.System;
using MediaMaster.DataBase;
using MediaMaster.DataBase.Models;
using MediaMaster.Views.Dialog;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Input;

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
    
    public static readonly DependencyProperty DeleteModeProperty
        = DependencyProperty.Register(
            nameof(DeleteMode),
            typeof(ItemsViewDeleteMode),
            typeof(TagView),
            new PropertyMetadata(ItemsViewDeleteMode.Disabled));

    public ItemsViewDeleteMode DeleteMode
    {
        get => (ItemsViewDeleteMode)GetValue(DeleteModeProperty);
        set => SetValue(DeleteModeProperty, value);
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
    
    public event TypedEventHandler<object, ICollection<Tag>>? RemoveTagsInvoked;
    public event TypedEventHandler<object, ICollection<int>>? SelectTagsInvoked;
    
    public TagView()
    {
        InitializeComponent();
        _ = UpdateItemSource();

        ItemView.SelectItemsInvoked += async (_, tags) =>
        {
            List<int> tagIds = tags.Where(t => t is Tag).Cast<Tag>().Select(t => t.TagId).ToList();
            (ContentDialogResult result, SelectTagsDialog? selectTagsDialog) = await SelectTagsDialog.ShowDialogAsync(tagIds, TagId != null ? [(int)TagId] : []);

            if (selectTagsDialog != null)
            {
                await UpdateItemSource(selectTagsDialog.SelectedTags);
            }

            SelectTagsInvoked?.Invoke(this, tagIds);
        };

        ItemView.RemoveItemsInvoked += async (_, tags) =>
        {
            List<Tag> collection = tags.Where(t => t is Tag).Cast<Tag>().ToList();
            await UpdateItemSource(collection);
            RemoveTagsInvoked?.Invoke(this, collection);
        };
    }

    public ICollection<Tag> GetItemSource()
    {
        return ItemView.GetItemSource<Tag>();
    }

    private void OnKeyDown(object sender, KeyRoutedEventArgs e)
    {
        if (DeleteMode == ItemsViewDeleteMode.Enabled && e.Key is VirtualKey.Delete)
        {
            var tag = ((ItemContainer)sender).Tag;
            ItemView.RemoveItem(tag);
        }
    }

    private async void EditTagFlyout_OnClick(object sender, RoutedEventArgs e)
    {
        var tagId = (int)((FrameworkElement)sender).Tag;
        await CreateEditDeleteTagDialog.ShowDialogAsync(tagId);

        await UpdateItemSource();
    }

    private async void DeleteTagFlyout_OnClick(object sender, RoutedEventArgs e)
    {
        var tagId = (int)((FrameworkElement)sender).Tag;
        var result = await CreateEditDeleteTagDialog.DeleteTag(tagId);

        if (result == ContentDialogResult.Primary)
        {
            await UpdateItemSource();
        }
    }

    private async Task UpdateItemSource(ICollection<Tag>? tags = null, bool refreshAll = false)
    {
        if (tags == null)
        {
            await using (MediaDbContext dataBase = new())
            {
                if (MediaId != null)
                {
                    tags = dataBase.Medias.Select(m => new { m.MediaId, m.Tags }).FirstOrDefault(m => m.MediaId == MediaId)?.Tags;
                }
                else if (TagId != null)
                {
                    if (refreshAll)
                    {
                        tags = dataBase.Tags.Select(t => new { t.TagId, t.Parents }).FirstOrDefault(t => t.TagId == TagId)?.Parents.ToList();
                    }
                    else if (GetItemSource().Count != 0)
                    {
                        tags = dataBase.Tags.Where(dbTag => GetItemSource().Select(t => t.TagId).Contains(dbTag.TagId)).ToList();
                    }
                }
            }
        }

        if (tags != null)
        {
            ItemView.ItemsSource = new ObservableCollection<object>(tags.OrderBy(e => e.Name));
        }
    }
}