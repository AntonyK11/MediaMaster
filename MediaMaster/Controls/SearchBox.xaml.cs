using System.Collections.ObjectModel;
using Windows.UI.Core;
using CommunityToolkit.Common.Collections;
using CommunityToolkit.WinUI;
using Microsoft.UI.Input;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Controls.Primitives;
using Microsoft.UI.Xaml.Input;
using MediaMaster.DataBase.Models;

namespace MediaMaster.Controls;

public sealed partial class SearchBox
{
    private Popup _popup = null!;
    private TagView _tagView = null!;

    private bool _isFocused;

    public ViewModel ViewModel { get; } = new();

    public SearchBox()
    {
        InitializeComponent();

        AutoSuggestBox.GettingFocus += AutoSuggestBox_GettingFocus;
        AutoSuggestBox.LosingFocus += AutoSuggestBox_LosingFocus;

        Loaded += SearchBox_Loaded;
    }

    private void SearchBox_Loaded(object sender, RoutedEventArgs e)
    {
        _popup = AutoSuggestBox.FindDescendants().OfType<Popup>().FirstOrDefault(x => x.Name is "SuggestionsPopup")!;
        _tagView = AutoSuggestBox.FindDescendants().OfType<TagView>().FirstOrDefault(x => x.Name is "TagView")!;
        //_tagView.ItemsSource = ViewModel.Tags;

        var tagView2 = AutoSuggestBox.FindDescendants().OfType<Popup>().FirstOrDefault(x => x.Name is "SuggestionsPopup")?.FindChildren().OfType<TagView>().FirstOrDefault(x => x.Name is "TagView2");
        //tagView2.ItemsSource = ViewModel.Tags;

        //AutoSuggestBox.ItemsSource = new IncrementalLoadingCollection<ViewModel, Tag>();

        //AutoSuggestBox.SuggestionChosen += async (_, _) =>
        //{
        //    await Task.Delay(1);
        //    _popup.IsOpen = true;
        //};

    }

    private void AutoSuggestBox_GettingFocus(UIElement sender, GettingFocusEventArgs args)
    {
        _popup.IsOpen = true;
        _isFocused = true;
        _tagView.ShowScrollButtons = true;
    }

    private void AutoSuggestBox_LosingFocus(UIElement sender, LosingFocusEventArgs args)
    {
        _popup.IsOpen = false;
        _isFocused = false;
        _tagView.ShowScrollButtons = false;
    }

    private void ButtonBase2_OnClick(object sender, RoutedEventArgs e)
    {
        ViewModel.Tags.Add(new Tag
        {
            Name = AutoSuggestBox.Text
        });
    }

    private void SuggestionsList_OnItemClick(AutoSuggestBox autoSuggestBox, AutoSuggestBoxSuggestionChosenEventArgs args)
    {
        AutoSuggestBox.Text = ((Tag)args.SelectedItem).Name;
    }
}

public class ViewModel : IIncrementalSource<Tag>
{
    public ObservableCollection<Tag> Tags { get; set; } = [];

    public ViewModel()
    {
        for (var i = 0; i < 10; i++)
        {
            Tags.Add(new Tag
            {
                Name = $"Tag {i}"
            });
        }
    }

    public async Task<IEnumerable<Tag>> GetPagedItemsAsync(int pageIndex, int pageSize, CancellationToken cancellationToken = new CancellationToken())
    {
        // Gets items from the collection according to pageIndex and pageSize parameters.
        var result = (from p in Tags select p).Skip(pageIndex * pageSize).Take(pageSize);

        // Simulates a longer request...
        await Task.Delay(10);

        return result;
    }

}

public class CustomButton : Button
{
    public CustomButton()
    {
        ProtectedCursor = InputCursor.CreateFromCoreCursor(new CoreCursor(CoreCursorType.Arrow, 0));
    }
}

