using System.Linq.Expressions;
using Windows.Foundation;
using CommunityToolkit.WinUI;
using DependencyPropertyGenerator;
using MediaMaster.DataBase;
using Microsoft.EntityFrameworkCore;
using Microsoft.UI.Input;
using Microsoft.UI.Xaml.Controls.Primitives;
using Microsoft.UI.Xaml.Data;

namespace MediaMaster.Controls;

[DependencyProperty("FilterName", typeof(bool), DefaultValue = true)]
[DependencyProperty("FilterNotes", typeof(bool), DefaultValue = false)]
[DependencyProperty("FilterTags", typeof(bool), DefaultValue = false)]
public partial class SearchBox : UserControl
{
    public readonly Expression<Func<Media, bool>> Filter;
    private bool _filterName = true;
    private bool _filterNotes;
    private bool _filterTags;
    private HashSet<int> _medias = [];
    private string _text = "";

    public SearchBox()
    {
        InitializeComponent();

        Filter = m =>
            _text.Length == 0 ||
            (_filterName && EF.Functions.Like(m.Name, $"%{_text}%")) ||
            (_filterNotes && EF.Functions.Like(m.Notes, $"%{_text}%")) ||
            (_filterTags && _medias.Contains(m.MediaId));

        TextBox.TextChanged += async (_, args) => { await Change(); };
    }

    public event TypedEventHandler<object, Expression<Func<Media, bool>>>? FilterChanged;

    private static async Task<HashSet<int>> GetMediasFromTagName(string tagName)
    {
        await using (var database = new MediaDbContext())
        {
            HashSet<int> tagsId = database.Tags
                .Where(t => EF.Functions.Like(t.Name, $"%{tagName}%"))
                .Select(t => t.TagId)
                .ToHashSet();

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

            return database.MediaTags.Where(m => tagsId.Contains(m.TagId)).Select(m => m.MediaId).ToHashSet();
        }
    }

    private async Task Change()
    {
        _filterName = FilterName;
        _filterNotes = FilterNotes;
        _filterTags = FilterTags;
        _text = TextBox.Text;
        if (_filterTags)
        {
            _medias = await GetMediasFromTagName(_text);
        }

        await App.DispatcherQueue.EnqueueAsync(() => FilterChanged?.Invoke(this, Filter));
    }

    async partial void OnFilterNameChanged()
    {
        await Change();
    }

    async partial void OnFilterNotesChanged()
    {
        await Change();
    }

    async partial void OnFilterTagsChanged()
    {
        await Change();
    }

    private void NameButton_OnLoaded(object sender, RoutedEventArgs e)
    {
        ((ToggleButton)sender).DataContext = this;

        var binding = new Binding { Path = new PropertyPath("FilterName"), Mode = BindingMode.TwoWay };
        ((ToggleButton)sender).SetBinding(ToggleButton.IsCheckedProperty, binding);
    }

    private void NotesButton_OnLoaded(object sender, RoutedEventArgs e)
    {
        ((ToggleButton)sender).DataContext = this;

        var binding = new Binding { Path = new PropertyPath("FilterNotes"), Mode = BindingMode.TwoWay };
        ((ToggleButton)sender).SetBinding(ToggleButton.IsCheckedProperty, binding);
    }

    private void TagsButton_OnLoaded(object sender, RoutedEventArgs e)
    {
        ((ToggleButton)sender).DataContext = this;

        var binding = new Binding { Path = new PropertyPath("FilterTags"), Mode = BindingMode.TwoWay };
        ((ToggleButton)sender).SetBinding(ToggleButton.IsCheckedProperty, binding);
    }
}

public partial class PointerToggleButton : ToggleButton
{
    public PointerToggleButton()
    {
        ProtectedCursor = InputSystemCursor.Create(InputSystemCursorShape.Arrow);
    }
}