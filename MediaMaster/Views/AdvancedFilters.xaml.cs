using System.Linq.Expressions;
using Windows.ApplicationModel.DataTransfer;
using Windows.Foundation;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.WinUI;
using MediaMaster.DataBase;
using MediaMaster.Extensions;
using Microsoft.EntityFrameworkCore;
using Microsoft.UI.Xaml.Shapes;

namespace MediaMaster.Views;

public partial class AdvancedFilters : Page
{
    public readonly ObservableCollection<FilterObject> FilterObjects = [];

    public AdvancedFilters()
    {
        InitializeComponent();
    }

    public static async Task<IList<Expression<Func<Media, bool>>>> GetFilterExpressions(
        IList<FilterObject> filterObjects)
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
                    Expression<Func<Media, bool>> orExpression = BuildOrExpression(await GetFilterExpressions(filterGroup.FilterObjects));
                    expressions.Add(orExpression);
                    break;

                case FilterGroup filterGroup:
                {
                    Expression<Func<Media, bool>> andExpression = BuildAndExpression(await GetFilterExpressions(filterGroup.FilterObjects));
                    expressions.Add(andExpression);
                    break;
                }
            }
        }

        return expressions;
    }

    private static Expression<Func<Media, bool>> BuildOrExpression(IList<Expression<Func<Media, bool>>> expressions)
    {
        Expression<Func<Media, bool>> retExpression = m => false;
        return expressions.Aggregate(retExpression, (current, expression) => current.Or(expression));
    }

    private static Expression<Func<Media, bool>> BuildAndExpression(IList<Expression<Func<Media, bool>>> expressions)
    {
        Expression<Func<Media, bool>> retExpression = m => true;
        return expressions.Aggregate(retExpression, (current, expression) => current.And(expression));
    }

    private static async Task<Expression<Func<Media, bool>>?> GetFilterExpression(Filter filter)
    {
        FilterType currentFilter = filter.FilterType;
        AdvancedType currentOperation = currentFilter.Operations.CurrentOperation;

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

    private void ButtonBase_OnClick(object sender, RoutedEventArgs e)
    {
        FilterObjects.Add(new Filter());
    }

    private void ButtonBase_OnClick2(object sender, RoutedEventArgs e)
    {
        FilterObjects.Add(new FilterGroup());
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

        ((ObservableCollection<FilterObject>)oldSender.ItemsSource).Remove(item);
        ((ObservableCollection<FilterObject>)target.ItemsSource).Insert(index, item);

        e.Handled = true;
    }

    private void UIElement_OnDrop(object sender, DragEventArgs e)
    {
        e.Handled = true;
        DragOperationDeferral def = e.GetDeferral();

        var item = (FilterObject)e.Data.Properties["item"];
        var oldSender = (ListView)e.Data.Properties["sender"];

        ((ObservableCollection<FilterObject>)oldSender.ItemsSource).Remove(item);

        def.Complete();
    }

    private void ButtonBase_OnClick3(object sender, RoutedEventArgs e)
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
}

public partial class AdvancedType : ObservableObject
{
    [ObservableProperty] private string _name = null!;
    [ObservableProperty] private string _uid = null!;
}

public abstract partial class Operations : AdvancedType
{
    [ObservableProperty] private AdvancedType _currentOperation;

    protected Operations()
    {
        CurrentOperation = OperationsCollection.First();
    }

    public abstract ICollection<AdvancedType> OperationsCollection { get; }

    partial void OnCurrentOperationChanged(AdvancedType? oldValue, AdvancedType newValue)
    {
        if (!OperationsCollection.Contains(newValue))
        {
            CurrentOperation = oldValue ?? OperationsCollection.First();
        }
    }
}

public partial class TextOperations : Operations
{
    private static readonly ICollection<AdvancedType> StaticOperationsCollection =
    [
        new AdvancedType { Uid = "/Home/Is_FilterOperation", Name = "Is" },
        new AdvancedType { Uid = "/Home/Contains_FilterOperation", Name = "Contains" },
        new AdvancedType { Uid = "/Home/StartWith_FilterOperation", Name = "Start_with" },
        new AdvancedType { Uid = "/Home/EndWith_FilterOperation", Name = "End_Width" }
    ];

    public override ICollection<AdvancedType> OperationsCollection => StaticOperationsCollection;
}

public partial class DateOperations : Operations
{
    private static readonly ICollection<AdvancedType> StaticOperationsCollection =
    [
        new AdvancedType { Uid = "/Home/After_FilterOperation", Name = "After" },
        new AdvancedType { Uid = "/Home/Before_FilterOperation", Name = "Before" },
        new AdvancedType { Uid = "/Home/From_FilterOperation", Name = "From_to" }
    ];

    public override ICollection<AdvancedType> OperationsCollection => StaticOperationsCollection;
}

public partial class TagsOperations : Operations
{
    private static readonly ICollection<AdvancedType> StaticOperationsCollection =
    [
        new AdvancedType { Uid = "/Home/Contains_FilterOperation", Name = "Contains" },
        new AdvancedType { Uid = "/Home/ContainsWithoutParents_FilterOperation", Name = "Contains_Without_Parents" }
    ];

    public override ICollection<AdvancedType> OperationsCollection => StaticOperationsCollection;
}

public partial class FilterType : AdvancedType
{
    [ObservableProperty] private string _category = null!;
    [ObservableProperty] private DateTimeOffset _date = DateTimeOffset.Now;

    [ObservableProperty] private AdvancedType _defaultType = null!;

    [ObservableProperty] private bool _negate;
    [ObservableProperty] private Operations _operations = null!;
    [ObservableProperty] private DateTimeOffset _secondDate = DateTimeOffset.Now;
    [ObservableProperty] private TimeSpan _secondTime;
    [ObservableProperty] private ICollection<Tag> _tags = [];

    [ObservableProperty] private string _text = null!;
    [ObservableProperty] private TimeSpan _time;
}

public abstract class FilterObject : ObservableObject;

public partial class Filter : FilterObject
{
    public readonly ICollection<FilterType> FiltersCollection =
    [
        new FilterType
            { 
                Uid = "/Home/Name_Filter",
                Name = "Name",
                Category = "Text",
                Operations = new TextOperations()
            },
        new FilterType
            { 
                Uid = "/Home/Notes_Filter",
                Name = "Notes",
                Category = "Text",
                Operations = new TextOperations() 
            },
        new FilterType
        {
            Uid = "/Home/DateAdded_Filter",
            Name = "Date_Added",
            Category = "Date",
            Operations = new DateOperations()
        },
        new FilterType
        {
            Uid = "/Home/DateModified_Filter",
            Name = "Date_Modifed",
            Category = "Date",
            Operations = new DateOperations()
        },
        new FilterType
        {
            Uid = "/Home/Tags_Filter",
            Name = "Tags",
            Category = "Tags",
            Operations = new TagsOperations()
        }
    ];

    [ObservableProperty] private FilterType _filterType;

    public Filter()
    {
        FilterType = FiltersCollection.First();
    }

    partial void OnFilterTypeChanged(FilterType? oldValue, FilterType newValue)
    {
        if (!FiltersCollection.Contains(newValue))
        {
            FilterType = oldValue ?? FiltersCollection.First();
        }
    }
}

public partial class FilterGroup : FilterObject
{
    public readonly ObservableCollection<FilterObject> FilterObjects = [];
    [ObservableProperty] private bool _orCombination = true;
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