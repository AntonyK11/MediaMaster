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
[DependencyProperty("FilterNotes", typeof(bool), DefaultValue = true)]
[DependencyProperty("FilterTags", typeof(bool), DefaultValue = true)]
[DependencyProperty("FilterPath", typeof(bool), DefaultValue = true)]
[DependencyProperty("IsSearching", typeof(bool), DefaultValue = false, IsReadOnly = true)]
public sealed partial class SearchBox : UserControl
{
    public readonly Expression<Func<Media, bool>> Filter;
    private bool _filterName = true;
    private bool _filterNotes = true;
    private bool _filterPath = true;
    private bool _filterTags = true;
    private HashSet<int> _medias = [];
    private string _text = "";

    public SearchBox()
    {
        InitializeComponent();

        Filter = m =>
            _text.Length == 0 ||
            (!_filterName && !_filterNotes && !_filterTags) ||
            (_filterName && EF.Functions.Like(m.Name, $"%{_text}%")) ||
            (_filterNotes && EF.Functions.Like(m.Notes, $"%{_text}%")) ||
            (_filterPath && EF.Functions.Like(m.Uri, $"%{_text}%")) ||
            (_filterTags && _medias.Contains(m.MediaId));
    }

    public event TypedEventHandler<object, Expression<Func<Media, bool>>>? FilterChanged;

    private static async Task<HashSet<int>> GetMediasFromTagName(string tagName)
    {
        await using (var database = new MediaDbContext())
        {
            HashSet<int> tagsId = await database.Tags
                .Where(t => EF.Functions.Like(t.Name, $"%{tagName}%"))
                .Select(t => t.TagId)
                .ToHashSetAsync();

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

            return await database.MediaTags.Where(m => tagsId.Contains(m.TagId)).Select(m => m.MediaId).ToHashSetAsync();
        }
    }

    private async Task Change()
    {
        _filterName = FilterName;
        _filterNotes = FilterNotes;
        _filterPath = FilterPath;
        _filterTags = FilterTags;
        _text = TextBox.Text;
        if (_filterTags)
        {
            _medias = await GetMediasFromTagName(_text);
        }

        await App.DispatcherQueue.EnqueueAsync(() => FilterChanged?.Invoke(this, Filter));

        IsSearching = (_filterName || _filterNotes || _filterPath || _filterTags) && _text.Length != 0;
    }

    private async void TextBox_OnTextChanged(object sender, TextChangedEventArgs e)
    {
        await Change();
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

    async partial void OnFilterPathChanged()
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

    private void PathButton_OnLoaded(object sender, RoutedEventArgs e)
    {
        ((ToggleButton)sender).DataContext = this;

        var binding = new Binding { Path = new PropertyPath("FilterPath"), Mode = BindingMode.TwoWay };
        ((ToggleButton)sender).SetBinding(ToggleButton.IsCheckedProperty, binding);
    }
}

public sealed partial class PointerToggleButton : ToggleButton
{
    public PointerToggleButton()
    {
        ProtectedCursor = InputSystemCursor.Create(InputSystemCursorShape.Arrow);
    }
}