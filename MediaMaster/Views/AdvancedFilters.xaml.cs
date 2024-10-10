using System.Linq.Expressions;
using System.Text.Json.Serialization;
using Windows.ApplicationModel.DataTransfer;
using Windows.Foundation;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.WinUI;
using MediaMaster.DataBase;
using MediaMaster.Extensions;
using MediaMaster.Interfaces.Services;
using MediaMaster.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.UI.Xaml.Shapes;
using WinUI3Localizer;

namespace MediaMaster.Views;

public sealed partial class AdvancedFilters : Page
{
    public readonly ObservableCollection<FilterObject> FilterObjects = [];
    public readonly SearchSavingService SearchSavingService;
    private bool _isSearchSaved = true;

    public AdvancedFilters()
    {
        InitializeComponent();

        SearchSavingService = App.GetService<SearchSavingService>();
        SetSearchState(_isSearchSaved);
        App.GetService<ITranslationService>().LanguageChanged += (_, _) =>
        {
            SetSearchState(_isSearchSaved);
        };
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
                    "Name_Is" => m => EF.Functions.Like(m.Name, $"{currentFilter.Text}"),
                    "Name_Contains" => m => EF.Functions.Like(m.Name, $"%{currentFilter.Text}%"),
                    "Name_Starts_With" => m => EF.Functions.Like(m.Name, $"{currentFilter.Text}%"),
                    "Name_Ends_Width" => m => EF.Functions.Like(m.Name, $"%{currentFilter.Text}"),
                    _ => null
                };
                break;

            case "Notes":
                expression = currentOperation.Name switch
                {
                    "Notes_Contain" => m => EF.Functions.Like(m.Notes, $"%{currentFilter.Text}%"),
                    _ => null
                };
                break;

            case "Date_Added":
                expression = currentOperation.Name switch
                {
                    "Date_After" => m => m.Added > currentDate,
                    "Date_Before" => m => m.Added < currentDate,
                    "Date_From_to" => m => currentDate < m.Added && m.Added < currentSecondaryDate,
                    _ => null
                };
                break;

            case "Date_Modified":
                expression = currentOperation.Name switch
                {
                    "Date_After" => m => m.Modified > currentDate,
                    "Date_Before" => m => m.Modified < currentDate,
                    "Date_From_to" => m => currentDate < m.Modified && m.Modified < currentSecondaryDate,
                    _ => null
                };
                break;

            case "Tags":
                HashSet<int> mediaIds = currentOperation.Name switch
                {
                    "Tags_Contain" => await GetMediasFromTags(currentFilter.Tags, currentFilter.WithParents),
                    "Tags_Name_Contains" => await GetMediasFromTagName(currentFilter.Text, currentFilter.WithParents),
                    _ => []
                };

                expression = m => mediaIds.Contains(m.MediaId);
                break;
        }

        if (expression != null && currentFilter.Negate)
        {
            expression = expression.Not();
        }

        return expression;
    }

    private static async Task<HashSet<int>> GetMediasFromTags(ICollection<Tag> tags, bool getTagsChildren = true)
    {
        await using (var database = new MediaDbContext())
        {
            HashSet<int> tagsId = tags.Select(t => t.TagId).ToHashSet();

            if (getTagsChildren)
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

    private static async Task<HashSet<int>> GetMediasFromTagName(string name, bool getTagsChildren = true)
    {
        await using (var database = new MediaDbContext())
        {
            HashSet<int> tagsId = database.Tags.Where(t => EF.Functions.Like(t.Name, $"%{name}%")).Select(t => t.TagId).ToHashSet();

            if (getTagsChildren)
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

        SetSearchState(false);
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

        SetSearchState(false);
    }

    private void AddFilter_OnClick(object sender, RoutedEventArgs e)
    {
        FilterObjects.Add(new Filter());
        SetSearchState(false);
    }

    private void AddGroup_OnClick(object sender, RoutedEventArgs e)
    {
        FilterObjects.Add(new FilterGroup());
        SetSearchState(false);
    }


    private void ClearAll_OnClick(object sender, RoutedEventArgs e)
    {
        FilterObjects.Clear();

        SetSearchState(true);
        SavedSearchesComboBox.SelectedIndex = -1;
        SearchName.Text = "";
    }

    private void SwitchButton_OnClick(object sender, RoutedEventArgs e)
    {
        var filterGroup = (FilterGroup)((Button)sender).Tag;
        filterGroup.OrCombination = !filterGroup.OrCombination;
        SetSearchState(false);
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
        var savedSearch = SearchSavingService.AddSavedSearch(name, FilterObjects.ToList());

        SavedSearchesComboBox.SelectedItem = savedSearch;
        SetSearchState(true);
    }

    private async void SavedSearchesComboBox_OnSelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (SavedSearchesComboBox.SelectedIndex == -1) return;

        FilterObjects.Clear();
        var savedSearch = (SavedSearch)SavedSearchesComboBox.SelectedItem;
        foreach (var filterObject in await savedSearch.GetFilterObjects())
        {
            FilterObjects.Add(filterObject);
        }

        SearchName.Text = savedSearch.Name;
        SetSearchState(true);
    }

    private void SetSearchState(bool isSaved)
    {
        _isSearchSaved = isSaved;
        if (isSaved)
        {
            SaveSearchButton.Content = "/Home/SaveSearch_AdvancedFilterFlyout".GetLocalizedString();
            SaveSearchButton.SetValue(ToolTipService.ToolTipProperty, null);
        }
        else
        {
            SaveSearchButton.Content = "/Home/SaveSearch_AdvancedFilterFlyout".GetLocalizedString() + "*";
            SaveSearchButton.SetValue(ToolTipService.ToolTipProperty, "/Home/SaveSearch_AdvancedFilterFlyout_Tooltip".GetLocalizedString());
        }
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
    internal string _name = string.Empty;

    [ObservableProperty]
    [JsonInclude]
    internal string _uid = string.Empty;
}

public abstract partial class Operations : AdvancedType
{
    [ObservableProperty]
    [JsonInclude]
    internal int _operationIndex;

    public abstract List<AdvancedType> OperationsCollection { get; set; }

    [JsonIgnore]
    public AdvancedType? CurrentOperation
    {
        get
        {
            return OperationsCollection[OperationIndex];
        }

        set
        {
            if (value != null)
            {
                OperationIndex = OperationsCollection.IndexOf(value);
            }
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

public partial class NameOperations : Operations
{
    private static List<AdvancedType> StaticOperationsCollection { get; set; } =
    [
        new() { Uid = "/Home/Name_Is_FilterOperation", Name = "Name_Is" },
        new() { Uid = "/Home/Name_Contains_FilterOperation", Name = "Name_Contains" },
        new() { Uid = "/Home/Name_StartsWith_FilterOperation", Name = "Name_Starts_With" },
        new() { Uid = "/Home/Name_EndsWith_FilterOperation", Name = "Name_Ends_Width" }
    ];

    public override List<AdvancedType> OperationsCollection
    {
        get => StaticOperationsCollection;
        set => StaticOperationsCollection = value;
    }

    public new string Name { get; } = "NameOperations";
}

public partial class NotesOperations : Operations
{
    private static List<AdvancedType> StaticOperationsCollection { get; set; } =
    [
        new() { Uid = "/Home/Notes_Contain_FilterOperation", Name = "Notes_Contain" }
    ];

    public override List<AdvancedType> OperationsCollection
    {
        get => StaticOperationsCollection;
        set => StaticOperationsCollection = value;
    }

    public new string Name { get; } = "NotesOperations";
}

public sealed partial class DateOperations : Operations
{
    private static List<AdvancedType> StaticOperationsCollection { get; set; } =
    [
        new() { Uid = "/Home/Date_After_FilterOperation", Name = "Date_After" },
        new() { Uid = "/Home/Date_Before_FilterOperation", Name = "Date_Before" },
        new() { Uid = "/Home/Date_From_FilterOperation", Name = "Date_From_to" }
    ];

    public override List<AdvancedType> OperationsCollection
    {
        get => StaticOperationsCollection;
        set => StaticOperationsCollection = value;
    }

    public new string Name { get; } = "DateOperations";
}

public sealed partial class TagsOperations : Operations
{
    private static List<AdvancedType> StaticOperationsCollection { get; set; } =
    [
        new() { Uid = "/Home/Tags_Contain_FilterOperation", Name = "Tags_Contain" },
        new() { Uid = "/Home/Tags_NameContains_FilterOperation", Name = "Tags_Name_Contains" }
    ];

    public override List<AdvancedType> OperationsCollection
    {
        get => StaticOperationsCollection;
        set => StaticOperationsCollection = value;
    }

    public new string Name { get; } = "TagsOperations";
}

public sealed partial class FilterType : AdvancedType
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
    internal string _text = string.Empty;

    [ObservableProperty]
    [JsonInclude]
    internal TimeSpan _time;

    [ObservableProperty]
    [JsonInclude]
    internal bool _withParents = true;

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

public sealed partial class Filter : FilterObject
{
    public List<FilterType> FiltersCollection { get; set; } =
    [
        new()
        { 
            Uid = "/Home/Name_Filter",
            Name = "Name",
            Category = "Text",
            Operations = new NameOperations()
        },
        new()
        { 
            Uid = "/Home/Notes_Filter",
            Name = "Notes",
            Category = "Text",
            Operations = new NotesOperations() 
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
            Name = "Date_Modified",
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

public sealed partial class FilterGroup : FilterObject
{
    public ObservableCollection<FilterObject> FilterObjects { get; set; } = [];

    [ObservableProperty]
    [JsonInclude]
    internal bool _orCombination = true;
}

internal sealed partial class FiltersTemplateSelector : DataTemplateSelector
{
    public DataTemplate FilterTemplate { get; set; } = null!;
    public DataTemplate FilterGroupTemplate { get; set; } = null!;

    protected override DataTemplate SelectTemplateCore(object item)
    {
        return item is Filter ? FilterTemplate : FilterGroupTemplate;
    }
}