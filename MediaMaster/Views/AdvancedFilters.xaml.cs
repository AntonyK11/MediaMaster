using System.Linq.Expressions;
using System.Text.Json.Serialization;
using Windows.ApplicationModel.DataTransfer;
using Windows.Foundation;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.WinUI;
using MediaMaster.DataBase;
using MediaMaster.Extensions;
using MediaMaster.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.UI.Xaml.Shapes;
using WinUI3Localizer;
using static CommunityToolkit.WinUI.Animations.Expressions.ExpressionValues;

namespace MediaMaster.Views;

public partial class AdvancedFilters : Page
{
    public readonly ObservableCollection<FilterObject> FilterObjects = [];
    public SearchSavingService SearchSavingService;

    public AdvancedFilters()
    {
        InitializeComponent();

        SearchSavingService = App.GetService<SearchSavingService>();
    }

    public static async Task<IList<Expression<Func<Media, bool>>>> GetFilterExpressions(IList<FilterObject> filterObjects)
    {
        IList<Expression<Func<Media, bool>>> expressions = [];
        foreach (FilterObject filterObject in filterObjects)
        {
            switch (filterObject)
            {
                case Filter filter:
                {
                    Expression<Func<Media, bool>>? expression = await GetFilterExpression(filter);
                    if (expression != null)
                    {
                        expressions.Add(expression);
                    }

                    break;
                }

                case FilterGroup { OrCombination: true } filterGroup:
                    Expression<Func<Media, bool>>? orExpression = BuildOrExpression(await GetFilterExpressions(filterGroup.FilterObjects));
                    if (orExpression != null)
                    {
                        expressions.Add(orExpression);
                    }
                    break;

                case FilterGroup filterGroup:
                {
                    Expression<Func<Media, bool>>? andExpression = BuildAndExpression(await GetFilterExpressions(filterGroup.FilterObjects));
                    if (andExpression != null)
                    {
                        expressions.Add(andExpression);
                    }
                    break;
                }
            }
        }

        return expressions;
    }

    private static Expression<Func<Media, bool>>? BuildOrExpression(IList<Expression<Func<Media, bool>>> expressions)
    {
        if (expressions.Count == 0) return null;

        Expression<Func<Media, bool>> retExpression = m => false;
        return expressions.Aggregate(retExpression, (current, expression) => current.Or(expression));
    }

    private static Expression<Func<Media, bool>>? BuildAndExpression(IList<Expression<Func<Media, bool>>> expressions)
    {
        if (expressions.Count == 0) return null;

        Expression<Func<Media, bool>> retExpression = m => true;
        return expressions.Aggregate(retExpression, (current, expression) => current.And(expression));
    }

    private static async Task<Expression<Func<Media, bool>>?> GetFilterExpression(Filter filter)
    {
        FilterType currentFilter = filter.FilterType;
        AdvancedType currentOperation = currentFilter.Operations.OperationsCollection[currentFilter.Operations.OperationIndex];

        DateTime currentDate = (currentFilter.Date.GetDateTimeOffsetUpToDay() + currentFilter.Time)
            .DateTime
            .ToUniversalTime();
        DateTime currentSecondaryDate = (currentFilter.SecondDate.GetDateTimeOffsetUpToDay() + currentFilter.SecondTime)
            .DateTime
            .ToUniversalTime();

        Expression<Func<Media, bool>>? expression = null;

        switch (currentFilter.Name)
        {
            case "Name":
                expression = currentOperation.Name switch
                {
                    "Is" => m => EF.Functions.Like(m.Name, $"{currentFilter.Text}"),
                    "Contains" => m => EF.Functions.Like(m.Name, $"%{currentFilter.Text}%"),
                    "Start_With" => m => EF.Functions.Like(m.Name, $"{currentFilter.Text}%"),
                    "End_Width" => m => EF.Functions.Like(m.Name, $"%{currentFilter.Text}"),
                    _ => null
                };
                break;

            case "Notes":
                expression = currentOperation.Name switch
                {
                    "Is" => m => EF.Functions.Like(m.Notes, $"{currentFilter.Text}"),
                    "Contains" => m => EF.Functions.Like(m.Notes, $"%{currentFilter.Text}%"),
                    "Start_With" => m => EF.Functions.Like(m.Notes, $"{currentFilter.Text}%"),
                    "End_Width" => m => EF.Functions.Like(m.Notes, $"%{currentFilter.Text}"),
                    _ => null
                };
                break;

            case "Date_Added":
                expression = currentOperation.Name switch
                {
                    "After" => m => m.Added > currentDate,
                    "Before" => m => m.Added < currentDate,
                    "From_to" => m => currentDate < m.Added && m.Added < currentSecondaryDate,
                    _ => null
                };
                break;

            case "Date_Modified":
                expression = currentOperation.Name switch
                {
                    "After" => m => m.Modified > currentDate,
                    "Before" => m => m.Modified < currentDate,
                    "From_to" => m => currentDate < m.Modified && m.Modified < currentSecondaryDate,
                    _ => null
                };
                break;

            case "Tags":
                HashSet<int> mediaIds = await GetMediasFromTags(currentFilter.Tags, currentOperation.Name == "Contains_Without_Parents");
                expression = m => mediaIds.Contains(m.MediaId);
                break;
        }

        if (expression != null && currentFilter.Negate)
        {
            expression = expression.Not();
        }

        return expression;
    }

    private static async Task<HashSet<int>> GetMediasFromTags(ICollection<Tag> tags, bool getTagsChildren = false)
    {
        await using (var database = new MediaDbContext())
        {
            HashSet<int> tagsId = tags.Select(t => t.TagId).ToHashSet();

            if (!getTagsChildren)
            {
                ICollection<TagTag> tagTags = await database.TagTags.ToListAsync();

                int oldCount;
                do
                {
                    oldCount = tagsId.Count;
                    foreach (TagTag tagTag in tagTags.Where(t => tagsId.Contains(t.ParentsTagId)))
                    {
                        tagsId.Add(tagTag.ChildrenTagId);
                    }
                } while (oldCount != tagsId.Count);
            }

            return database.MediaTags.Where(m => tagsId.Contains(m.TagId)).Select(m => m.MediaId).ToHashSet();
        }
    }

    private void ListView_DragOver(object sender, DragEventArgs e)
    {
        e.AcceptedOperation = DataPackageOperation.Move;
    }

    private void ListView_DragItemsStarting(object sender, DragItemsStartingEventArgs e)
    {
        e.Data.Properties.Add("item", e.Items.First());
        e.Data.Properties.Add("sender", sender);
        e.Data.RequestedOperation = DataPackageOperation.Move;
    }

    private static bool IsInItemSource(object itemSource, ObservableCollection<FilterObject> filterObjects)
    {
        if (ReferenceEquals(itemSource, filterObjects))
        {
            return true;
        }

        foreach (FilterObject filterObject in filterObjects)
        {
            if (filterObject is FilterGroup filterGroup)
            {
                var result = IsInItemSource(itemSource, filterGroup.FilterObjects);
                if (result)
                {
                    return true;
                }
            }
        }

        return false;
    }

    private void ListView_Drop(object sender, DragEventArgs e)
    {
        var target = (ListView)sender;

        var item = (FilterObject)e.Data.Properties["item"];
        var oldSender = (ListView)e.Data.Properties["sender"];

        if (oldSender == target) return;
        if (item is FilterGroup filterGroup)
        {
            if (IsInItemSource(target.ItemsSource, filterGroup.FilterObjects))
            {
                return;
            }
        }

        Point position = e.GetPosition(target.ItemsPanelRoot);
        var index = target.Items.Count;

        for (var i = 0; i < target.Items.Count; i++)
        {
            var container = target.ContainerFromIndex(i) as ListViewItem;
            if (container != null)
            {
                Rect bounds = container
                    .TransformToVisual(target)
                    .TransformBounds(new Rect(0, 0, container.ActualWidth, container.ActualHeight));
                if ((bounds.Top + bounds.Bottom) / 2 > position.Y)
                {
                    index = i;
                    break;
                }
            }
        }
        e.Handled = true;

        ((ObservableCollection<FilterObject>)oldSender.ItemsSource).Remove(item);
        ((ObservableCollection<FilterObject>)target.ItemsSource).Insert(index, item);

        ((Grid)oldSender.Parent).MaxHeight = 0;
        ((Grid)target.Parent).MaxHeight = 0;
        ((Grid)oldSender.Parent).UpdateLayout();
        ((Grid)target.Parent).UpdateLayout();
        ((Grid)oldSender.Parent).MaxHeight = double.MaxValue;
        ((Grid)target.Parent).MaxHeight = double.MaxValue;
    }

    private void UIElement_OnDrop(object sender, DragEventArgs e)
    {
        e.Handled = true;
        var item = (FilterObject)e.Data.Properties["item"];
        var oldSender = (ListView)e.Data.Properties["sender"];

        ((ObservableCollection<FilterObject>)oldSender.ItemsSource).Remove(item);

        ((Grid)oldSender.Parent).MaxHeight = 0;
        ((Grid)oldSender.Parent).UpdateLayout();
        ((Grid)oldSender.Parent).MaxHeight = double.MaxValue;
    }

    private void AddFilter_OnClick(object sender, RoutedEventArgs e)
    {
        FilterObjects.Add(new Filter());
    }

    private void AddGroup_OnClick(object sender, RoutedEventArgs e)
    {
        FilterObjects.Add(new FilterGroup());
    }


    private void ClearAll_OnClick(object sender, RoutedEventArgs e)
    {
        FilterObjects.Clear();
    }

    private void SwitchButton_OnClick(object sender, RoutedEventArgs e)
    {
        var filterGroup = (FilterGroup)((Button)sender).Tag;
        filterGroup.OrCombination = !filterGroup.OrCombination;
    }

    private void Canvas_OnSizeChanged(object sender, SizeChangedEventArgs e)
    {
        var canvas = (Canvas)sender;
        FrameworkElement? tagView = canvas.FindChild("TagView");
        if (tagView != null)
        {
            tagView.Width = e.NewSize.Width;
        }
    }

    private void FrameworkElement_OnSizeChanged(object sender, SizeChangedEventArgs e)
    {
        var grid = (Grid)sender;

        var upperLine = (Line?)grid.FindChild("UpperLine");
        if (upperLine != null)
        {
            upperLine.Y2 = grid.RowDefinitions[1].ActualHeight - 1;
        }

        var lowerLine = (Line?)grid.FindChild("LowerLine");
        if (lowerLine != null)
        {
            lowerLine.Y2 = grid.RowDefinitions[3].ActualHeight - 1;
        }
    }

    private void SaveSearch_OnClick(object sender, RoutedEventArgs e)
    {
        var name = SearchName.Text.IsNullOrEmpty() ? "/Home/DefaultName_AdvancedFilterFlyout".GetLocalizedString() : SearchName.Text;
        SearchSavingService.AddSavedSearch(name, FilterObjects.ToList());
    }

    private async void SavedSearchesComboBox_OnSelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (SavedSearchesComboBox.SelectedIndex == -1) return;

        FilterObjects.Clear();
        foreach (var filterObject in await ((SavedSearch)SavedSearchesComboBox.SelectedItem).GetFilterObjects())
        {
            FilterObjects.Add(filterObject);
        }

        SavedSearchesComboBox.SelectedIndex = -1;
    }

    private async void FrameworkElement_OnDataContextChanged(FrameworkElement sender, DataContextChangedEventArgs args)
    {
        sender.MaxHeight = sender.MinHeight;
        await Task.Yield();
        sender.MaxHeight = double.MaxValue;
    }
}

public partial class AdvancedType : ObservableObject
{
    [ObservableProperty]
    [JsonInclude]
    internal string _name = null!;

    [ObservableProperty]
    [JsonInclude]
    internal string _uid = null!;
}

public abstract partial class Operations : AdvancedType
{
    [ObservableProperty]
    [JsonInclude]
    internal int _operationIndex;

    public abstract List<AdvancedType> OperationsCollection { get; set; }

    [JsonIgnore]
    public AdvancedType CurrentOperation
    {
        get
        {
            return OperationsCollection[OperationIndex];
        }

        set
        {
            OperationIndex = OperationsCollection.IndexOf(value);
        }
    }

    partial void OnOperationIndexChanged(int value)
    {
        if (value < 0 || value >= OperationsCollection.Count)
        {
            OperationIndex = 0;
        }
        OnPropertyChanged("CurrentOperation");
    }
}

public partial class TextOperations : Operations
{
    private static List<AdvancedType> StaticOperationsCollection { get; set; } =
    [
        new() { Uid = "/Home/Is_FilterOperation", Name = "Is" },
        new() { Uid = "/Home/Contains_FilterOperation", Name = "Contains" },
        new() { Uid = "/Home/StartWith_FilterOperation", Name = "Start_With" },
        new() { Uid = "/Home/EndWith_FilterOperation", Name = "End_Width" }
    ];

    public override List<AdvancedType> OperationsCollection
    {
        get => StaticOperationsCollection;
        set => StaticOperationsCollection = value;
    }

    public new string Name { get; } = "TextOperations";
}

public partial class DateOperations : Operations
{
    private static List<AdvancedType> StaticOperationsCollection { get; set; } =
    [
        new() { Uid = "/Home/After_FilterOperation", Name = "After" },
        new() { Uid = "/Home/Before_FilterOperation", Name = "Before" },
        new() { Uid = "/Home/From_FilterOperation", Name = "From_to" }
    ];

    public override List<AdvancedType> OperationsCollection
    {
        get => StaticOperationsCollection;
        set => StaticOperationsCollection = value;
    }

    public new string Name { get; } = "DateOperations";
}

public partial class TagsOperations : Operations
{
    private static List<AdvancedType> StaticOperationsCollection { get; set; } =
    [
        new() { Uid = "/Home/Contains_FilterOperation", Name = "Contains" },
        new() { Uid = "/Home/ContainsWithoutParents_FilterOperation", Name = "Contains_Without_Parents" }
    ];

    public override List<AdvancedType> OperationsCollection
    {
        get => StaticOperationsCollection;
        set => StaticOperationsCollection = value;
    }

    public new string Name { get; } = "TagsOperations";
}

public partial class FilterType : AdvancedType
{
    [ObservableProperty]
    [JsonInclude]
    internal string _category = null!;

    [ObservableProperty]
    [JsonInclude]
    internal DateTimeOffset _date = DateTimeOffset.Now;

    [ObservableProperty]
    [JsonInclude]
    internal bool _negate;

    [ObservableProperty]
    [JsonInclude]
    internal Operations _operations = null!;

    [ObservableProperty]
    [JsonInclude]
    internal DateTimeOffset _secondDate = DateTimeOffset.Now;

    [ObservableProperty]
    [JsonInclude]
    internal TimeSpan _secondTime;

    [ObservableProperty]
    [JsonInclude]
    internal string _text = null!;

    [ObservableProperty]
    [JsonInclude]
    internal TimeSpan _time;

    [ObservableProperty]
    [JsonInclude]
    internal HashSet<int> _tagsHashSet = [];

    private ICollection<Tag> _tags = [];
    [JsonIgnore]
    public ICollection<Tag> Tags
    {
        get
        {
            if (_tags.Count == 0 && TagsHashSet.Count != 0)
            {
                using (var database = new MediaDbContext())
                {
                    Tags = database.Tags.Where(t => TagsHashSet.Contains(t.TagId)).ToList();
                }
            }

            return _tags;
        }
        set
        {
            if (!EqualityComparer<ICollection<Tag>>.Default.Equals(_tags, value))
            {
                OnPropertyChanging();
                _tags = value;
                OnTagsChanged(value);
                OnPropertyChanged();
            }
        }
    }

    private void OnTagsChanged(ICollection<Tag> value)
    {
        TagsHashSet = value.Select(t => t.TagId).ToHashSet();
    }
}

public abstract class FilterObject : ObservableObject;

public partial class Filter : FilterObject
{
    public List<FilterType> FiltersCollection { get; set; } =
    [
        new()
        { 
            Uid = "/Home/Name_Filter",
            Name = "Name",
            Category = "Text",
            Operations = new TextOperations()
        },
        new()
        { 
            Uid = "/Home/Notes_Filter",
            Name = "Notes",
            Category = "Text",
            Operations = new TextOperations() 
        },
        new()
        {
            Uid = "/Home/DateAdded_Filter",
            Name = "Date_Added",
            Category = "Date",
            Operations = new DateOperations()
        },
        new()
        {
            Uid = "/Home/DateModified_Filter",
            Name = "Date_Modifed",
            Category = "Date",
            Operations = new DateOperations()
        },
        new()
        {
            Uid = "/Home/Tags_Filter",
            Name = "Tags",
            Category = "Tags",
            Operations = new TagsOperations()
        }
    ];

    [ObservableProperty]
    [JsonInclude]
    internal int _filterTypeIndex;

    [JsonIgnore]
    public FilterType FilterType
    {
        get
        {
            return FiltersCollection[FilterTypeIndex];
        }

        set
        {
            FilterTypeIndex = FiltersCollection.IndexOf(value);
        }
    }

    public Filter()
    {
        FilterTypeIndex = 0;
    }

    partial void OnFilterTypeIndexChanged(int value)
    {
        if (value < 0 || value >= FiltersCollection.Count)
        {
            FilterTypeIndex = 0;
        }
        OnPropertyChanged("FilterType");
    }
}

public partial class FilterGroup : FilterObject
{
    public ObservableCollection<FilterObject> FilterObjects { get; set; } = [];

    [ObservableProperty]
    [JsonInclude]
    internal bool _orCombination = true;
}

internal partial class FiltersTemplateSelector : DataTemplateSelector
{
    public DataTemplate FilterTemplate { get; set; } = null!;
    public DataTemplate FilterGroupTemplate { get; set; } = null!;

    protected override DataTemplate SelectTemplateCore(object item)
    {
        return item is Filter ? FilterTemplate : FilterGroupTemplate;
    }
}