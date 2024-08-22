using System.Reflection;
using System.Text;
using BookmarksManager;
using CommunityToolkit.Mvvm.ComponentModel;
using EFCore.BulkExtensions;
using MediaMaster.Controls;
using MediaMaster.DataBase;
using MediaMaster.Extensions;
using MediaMaster.Helpers;
using MediaMaster.Interfaces.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.WindowsAPICodePack.Dialogs;
using Windows.ApplicationModel.DataTransfer;
using Windows.Foundation;
using WinUI3Localizer;
using WinUIEx;

namespace MediaMaster.Views.Dialog;

public sealed partial class AdvancedFiltersDialog : Page
{
    public ObservableCollection<FilterObject> FilterObjects = [];

    public AdvancedFiltersDialog()
    {
        InitializeComponent();
        //TagView.MediaIds = [-1];
    }

    //private void FileUriTextBox_OnEditButtonPressed(EditableTextBlock sender, string args)
    //{
    //    // Cannot use System.Windows.Forms.OpenFileDialog because it makes the app crash if the window is closed after the dialog in certain situations
    //    using (CommonOpenFileDialog dialog = new())
    //    {
    //        dialog.InitialDirectory = Path.GetDirectoryName(FileUriTextBox.Text);
    //        dialog.DefaultFileName = Path.GetFileNameWithoutExtension(FileUriTextBox.Text);
    //        dialog.EnsureFileExists = true;
    //        dialog.EnsurePathExists = true;
    //        dialog.ShowHiddenItems = true;

    //        // Use reflection to set the _parentWindow handle without needing to include PresentationFrameWork
    //        FieldInfo? fi = typeof(CommonFileDialog).GetField("_parentWindow", BindingFlags.NonPublic | BindingFlags.Instance);
    //        if (fi != null && App.MainWindow != null)
    //        {
    //            var hwnd = App.MainWindow.GetWindowHandle();
    //            fi.SetValue(dialog, hwnd);
    //        }

    //        if (dialog.ShowDialog() == CommonFileDialogResult.Ok && dialog.FileName != null)
    //        {
    //            FileUriTextBox.Text = dialog.FileName;
    //        }
    //    }
    //}

    public static async Task<(ContentDialogResult, AdvancedFiltersDialog?)> ShowDialogAsync()
    {
        if (App.MainWindow == null) return (ContentDialogResult.None, null);

        var mediaDialog = new AdvancedFiltersDialog();
        ContentDialog dialog = new()
        {
            XamlRoot = App.MainWindow.Content.XamlRoot,
            DefaultButton = ContentDialogButton.Primary,
            Content = mediaDialog,
            RequestedTheme = App.GetService<IThemeSelectorService>().ActualTheme,
            Resources =
            {
                ["ContentDialogMaxWidth"] = double.PositiveInfinity
            }
        };
        Uids.SetUid(dialog, "/Media/CreateMediaDialog");
        App.GetService<IThemeSelectorService>().ThemeChanged += (_, theme) => { dialog.RequestedTheme = theme; };

        ContentDialogResult result = await dialog.ShowAndEnqueueAsync();
        //while (true)
        //{
        //    result = await dialog.ShowAndEnqueueAsync();

        //    if (result == ContentDialogResult.Primary)
        //    {
        //        var validation = await mediaDialog.ValidateMedia();

        //        if (validation != null)
        //        {
        //            ContentDialog errorDialog = new()
        //            {
        //                XamlRoot = App.MainWindow.Content.XamlRoot,
        //                DefaultButton = ContentDialogButton.Primary,
        //                RequestedTheme = App.GetService<IThemeSelectorService>().ActualTheme,
        //            };
        //            App.GetService<IThemeSelectorService>().ThemeChanged += (_, theme) => { errorDialog.RequestedTheme = theme; };

        //            switch (validation)
        //            {
        //                case "MissingFilePath":
        //                    Uids.SetUid(errorDialog, "/Media/MissingFilePathDialog");
        //                    break;
        //                case "FilePathAlreadyExists":
        //                    Uids.SetUid(errorDialog, "/Media/FilePathAlreadyExistsDialog");
        //                    break;
        //                case "MissingWebsiteUrl":
        //                    Uids.SetUid(errorDialog, "/Media/MissingWebsiteUrlDialog");
        //                    break;
        //                case "WebsiteUrlAlreadyExists":
        //                    Uids.SetUid(errorDialog, "/Media/WebsiteUrlAlreadyExistsDialog");
        //                    break;
        //            }

        //            ContentDialogResult errorResult = await errorDialog.ShowAndEnqueueAsync();
        //            if (errorResult == ContentDialogResult.None)
        //            {
        //                break;
        //            }
        //        }
        //        else
        //        {
        //            await mediaDialog.SaveChangesAsync();
        //            break;
        //        }
        //    }
        //    else
        //    {
        //        break;
        //    }
        //}

        return (result, mediaDialog);
    }

    //public async Task<string?> ValidateMedia()
    //{
    //    switch (SelectorBar.SelectedItem.Tag)
    //    {
    //        case "File":
    //            var filePath = FileUriTextBox.Text;
    //            if (filePath.IsNullOrEmpty())
    //            {
    //                return "MissingFilePath";
    //            }

    //            await using (var database = new MediaDbContext())
    //            {
    //                if (await database.Medias.Select(m => m.Uri).ContainsAsync(filePath))
    //                {
    //                    return "FilePathAlreadyExists";
    //                }
    //            }
    //            break;

    //        case "Website":
    //            var websiteUrl = WebsiteUriTextBox.Text;

    //            if (websiteUrl.IsNullOrEmpty())
    //            {
    //                return "MissingWebsiteUrl";
    //            }

    //            websiteUrl = websiteUrl.FormatAsWebsite();

    //            await using (var database = new MediaDbContext())
    //            {
    //                if (await database.Medias.Select(m => m.Uri).ContainsAsync(websiteUrl))
    //                {
    //                    return "WebsiteUrlAlreadyExists";
    //                }
    //            }
    //            break;
    //    }

    //    return null;
    //}

    //public async Task SaveChangesAsync()
    //{
    //    await using (MediaDbContext database = new())
    //    {
            
    //        Media media = new()
    //        {
    //            Name = NameTextBox.Text,
    //            Notes = NotesTextBox.Text
    //        };

    //        switch (SelectorBar.SelectedItem.Tag)
    //        {
    //            case "File":
    //                media.Uri = FileUriTextBox.Text;
    //                break;

    //            case "Website":
    //                media.Uri = WebsiteUriTextBox.Text.FormatAsWebsite();
    //                break;
    //        }

    //        await database.Medias.AddAsync(media);
    //        await database.SaveChangesAsync();

    //        HashSet<int> currentTagIds = media.Tags.Select(t => t.TagId).ToHashSet();
    //        HashSet<int> selectedTagIds = TagView.GetItemSource().Select(t => t.TagId).ToHashSet();

    //        List<int> tagIdsToAdd = selectedTagIds.Except(currentTagIds).ToList();
    //        List<int> tagIdsToRemove = currentTagIds.Except(selectedTagIds).ToList();

    //        if (tagIdsToAdd.Count != 0 || tagIdsToRemove.Count != 0)
    //        {
    //            // Bulk add new tags
    //            if (tagIdsToAdd.Count != 0)
    //            {
    //                List<MediaTag> newMediaTags = tagIdsToAdd.Select(tagId => new MediaTag { MediaId = media.MediaId, TagId = tagId }).ToList();
    //                await database.BulkInsertOrUpdateAsync(newMediaTags);
    //            }

    //            // Bulk remove old tags
    //            if (tagIdsToRemove.Count != 0)
    //            {
    //                List<MediaTag> mediaTagsToRemove = await database.MediaTags
    //                    .Where(mt => mt.MediaId == media.MediaId && tagIdsToRemove.Contains(mt.TagId))
    //                    .ToListAsync();
    //                await database.BulkDeleteAsync(mediaTagsToRemove);
    //            }

    //            if (MediaDbContext.ArchivedTag != null)
    //            {
    //                if (tagIdsToAdd.Contains(MediaDbContext.ArchivedTag.TagId))
    //                {
    //                    media.IsArchived = true;
    //                }
    //                else if (tagIdsToRemove.Contains(MediaDbContext.ArchivedTag.TagId))
    //                {
    //                    media.IsArchived = false;
    //                }
    //            }

    //            if (MediaDbContext.FavoriteTag != null)
    //            {
    //                if (tagIdsToAdd.Contains(MediaDbContext.FavoriteTag.TagId))
    //                {
    //                    media.IsFavorite = true;
    //                }
    //                else if (tagIdsToRemove.Contains(MediaDbContext.FavoriteTag.TagId))
    //                {
    //                    media.IsFavorite = false;
    //                }
    //            }
    //        }

    //        MediaDbContext.InvokeMediaChange(this, MediaChangeFlags.MediaAdded, [media]);
    //    }
    //}
    private void ButtonBase_OnClick(object sender, RoutedEventArgs e)
    {
        FilterObjects.Add(new Filter());
    }

    private void ButtonBase_OnClick2(object sender, RoutedEventArgs e)
    {
        FilterObjects.Add(new FilterGroup());
    }

    private void DragOver(object sender, DragEventArgs e)
    {
        e.AcceptedOperation = DataPackageOperation.Move;
    }

    private void DragItemsStarting(object sender, DragItemsStartingEventArgs e)
    {
        e.Data.Properties.Add("item", e.Items.First());
        e.Data.Properties.Add("sender", sender);
        e.Data.RequestedOperation = DataPackageOperation.Move;

    }

    private async void ListView_Drop(object sender, DragEventArgs e)
    {
        e.Handled = true;
        DragOperationDeferral def = e.GetDeferral();
        ListView target = (ListView)sender;

        var item = (FilterObject)e.Data.Properties["item"];
        var oldSender = (ListView)e.Data.Properties["sender"];

        var position = e.GetPosition(target.ItemsPanelRoot);
        var index = target.Items.Count;

        for (var i = 0; i < target.Items.Count; i++)
        {
            var container = target.ContainerFromIndex(i) as ListViewItem;
            if (container != null)
            {
                var bounds = container.TransformToVisual(target).TransformBounds(new Rect(0, 0, container.ActualWidth, container.ActualHeight));
                if ((bounds.Top + bounds.Bottom)/2 > position.Y)
                {
                    index = i;
                    break;
                }
            }
        }

        ((ObservableCollection<FilterObject>)oldSender.ItemsSource).Remove(item);
        ((ObservableCollection<FilterObject>)target.ItemsSource).Insert(index, item);

        def.Complete();
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
}

public partial class Type : ObservableObject
{
    [ObservableProperty] private string _name;
    [ObservableProperty] private string _uid;
}

public abstract partial class Operations : Type
{
    [ObservableProperty] private Type _currentOperation;
    public virtual ICollection<Type> OperationsCollection { get; }

    protected Operations()
    {
        CurrentOperation = OperationsCollection.First();
    }

    partial void OnCurrentOperationChanged(Type? oldValue, Type newValue)
    {
        if (!OperationsCollection.Contains(newValue))
        {
            CurrentOperation = oldValue ?? OperationsCollection.First();
        }
    }
}

public partial class TextOperations : Operations
{
    private static readonly ICollection<Type> StaticOperationsCollection =
    [
        new Type { Uid = "/Home/Is_FilterOperation", Name = "Is"},
        new Type { Uid = "/Home/Contains_FilterOperation", Name = "Contains" },
        new Type { Uid = "/Home/StartWith_FilterOperation", Name = "Start_with" },
        new Type { Uid = "/Home/EndWith_FilterOperation", Name = "End_Width" }
    ];

    public override ICollection<Type> OperationsCollection => StaticOperationsCollection;
}

public partial class DateOperations : Operations
{
    private static readonly ICollection<Type> StaticOperationsCollection =
    [
        new Type { Uid = "/Home/After_FilterOperation", Name = "After" },
        new Type { Uid = "/Home/Before_FilterOperation", Name = "Before" },
        new Type { Uid = "/Home/From_FilterOperation", Name = "From_to" }
    ];

    public override ICollection<Type> OperationsCollection => StaticOperationsCollection;
}

public partial class TagsOperations : Operations
{
    private static readonly ICollection<Type> StaticOperationsCollection =
    [
        new Type { Uid = "/Home/Contains_FilterOperation", Name = "Contains" },
        new Type { Uid = "/Home/ContainsWithoutParents_FilterOperation", Name = "Contains_Without_Parents" }
    ];

    public override ICollection<Type> OperationsCollection => StaticOperationsCollection;
}


public partial class FilterType : Type
{
    [ObservableProperty] private string _category;
    [ObservableProperty] private Operations _operations;

    [ObservableProperty] private Type _defaultType;

    [ObservableProperty] private string _text;
    [ObservableProperty] private DateTimeOffset _date = DateTimeOffset.Now;
    [ObservableProperty] private TimeSpan _time;
    [ObservableProperty] private DateTimeOffset _secondDate = DateTimeOffset.Now;
    [ObservableProperty] private TimeSpan _secondTime;
    [ObservableProperty] private ICollection<Tag> _tags = [];

    [ObservableProperty] private bool _negate;
}

public partial class FilterObject : ObservableObject
{

}

public partial class Filter : FilterObject
{
    public readonly ICollection<FilterType> FiltersCollection =
    [
        new FilterType { Uid = "/Home/Name_Filter", Name = "Name", Category = "Text", Operations = new TextOperations() },
        new FilterType { Uid = "/Home/Notes_Filter", Name = "Notes", Category = "Text", Operations = new TextOperations() },
        new FilterType { Uid = "/Home/DateAdded_Filter", Name = "Date_Added", Category = "Date", Operations = new DateOperations() },
        new FilterType { Uid = "/Home/DateModified_Filter", Name = "Date_Modifed", Category = "Date", Operations = new DateOperations() },
        new FilterType { Uid = "/Home/Tags_Filter", Name = "Tags", Category = "Tags", Operations = new TagsOperations() }
    ];

    [ObservableProperty] private string _title = "hi";

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
}

internal partial class FiltersTemplateSelector : DataTemplateSelector
{
    public DataTemplate FilterTemplate { get; set; }
    public DataTemplate FilterGroupTemplate { get; set; }

    protected override DataTemplate SelectTemplateCore(object item)
    {
        return item is Filter ? FilterTemplate : FilterGroupTemplate;
    }
}