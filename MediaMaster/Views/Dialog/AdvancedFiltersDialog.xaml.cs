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
            Content = mediaDialog
        };

        Uids.SetUid(dialog, "/Media/CreateMediaDialog");

        dialog.RequestedTheme = App.GetService<IThemeSelectorService>().ActualTheme;
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

    private void ListViewBase_OnDragItemsCompleted(ListViewBase sender, DragItemsCompletedEventArgs args)
    {
        Debug.WriteLine(args);
    }

    private void Target_DragOver(object sender, DragEventArgs e)
    {
        e.AcceptedOperation = DataPackageOperation.Move;
    }

    private void Target_DragEnter(object sender, DragEventArgs e)
    {
        // We don't want to show the Move icon
        e.DragUIOverride.IsGlyphVisible = false;
    }

    private void Source_DragOver(object sender, DragEventArgs e)
    {
        e.AcceptedOperation = DataPackageOperation.Move;
    }

    private void Source_DragItemsStarting(object sender, DragItemsStartingEventArgs e)
    {
        //// Prepare a string with one dragged item per line
        //StringBuilder items = new StringBuilder();
        //foreach (Contact item in e.Items)
        //{
        //    if (items.Length > 0) { items.AppendLine(); }
        //    if (item.ToString() != null)
        //    {
        //        // Append name from contact object onto data string
        //        items.Append(item.ToString() + " " + item.Company);
        //    }
        //}
        //// Set the content of the DataPackage
        //e.Data.SetText(items.ToString());
        e.Data.Properties.Add("1", e.Items.First());
        e.Data.Properties.Add("sender", sender);
        e.Data.RequestedOperation = DataPackageOperation.Move;

    }

    private async void Target_DragItemsStarting(object sender, DragItemsStartingEventArgs e)
    {
        //if (e.Items.Count == 1)
        //{
            //// Prepare ListViewItem to be moved
            //Contact tmp = (Contact)e.Items[0];

        e.Data.Properties.Add("1", e.Items.First());
        e.Data.Properties.Add("sender", sender);
        e.Data.RequestedOperation = DataPackageOperation.Move;
        //}
    }

    private async void ListView_Drop(object sender, DragEventArgs e)
    {
        e.Handled = true;
        DragOperationDeferral def = e.GetDeferral();
        ListView target = (ListView)sender;

        var item = (FilterObject)e.Data.Properties["1"];
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

        //if (e.DataView.Contains(StandardDataFormats.Text))
        //{
        //    DragOperationDeferral def = e.GetDeferral();
        //    string s = await e.DataView.GetTextAsync();
        //    string[] items = s.Split('\n');
        //    foreach (string item in items)
        //    {

        //        //// Create Contact object from string, add to existing target ListView
        //        //string[] info = item.Split(" ", 3);
        //        //Contact temp = new Contact(info[0], info[1], info[2]);

        //        //// Find the insertion index:
        //        //Windows.Foundation.Point pos = e.GetPosition(target.ItemsPanelRoot);

        //        //// If the target ListView has items in it, use the height of the first item
        //        ////      to find the insertion index.
        //        //int index = 0;
        //        //if (target.Items.Count != 0)
        //        //{
        //        //    // Get a reference to the first item in the ListView
        //        //    ListViewItem sampleItem = (ListViewItem)target.ContainerFromIndex(0);

        //        //    // Adjust itemHeight for margins
        //        //    double itemHeight = sampleItem.ActualHeight + sampleItem.Margin.Top + sampleItem.Margin.Bottom;

        //        //    // Find index based on dividing number of items by height of each item
        //        //    index = Math.Min(target.Items.Count - 1, (int)(pos.Y / itemHeight));

        //        //    // Find the item being dropped on top of.
        //        //    ListViewItem targetItem = (ListViewItem)target.ContainerFromIndex(index);

        //        //    // If the drop position is more than half-way down the item being dropped on
        //        //    //      top of, increment the insertion index so the dropped item is inserted
        //        //    //      below instead of above the item being dropped on top of.
        //        //    Windows.Foundation.Point positionInItem = e.GetPosition(targetItem);
        //        //    if (positionInItem.Y > itemHeight / 2)
        //        //    {
        //        //        index++;
        //        //    }

        //        //    // Don't go out of bounds
        //        //    index = Math.Min(target.Items.Count, index);
        //        //}
        //        //// Only other case is if the target ListView has no items (the dropped item will be
        //        ////      the first). In that case, the insertion index will remain zero.

        //        //// Find correct source list
        //        //if (target.Name == "DragDropListView")
        //        //{
        //        //    // Find the ItemsSource for the target ListView and insert
        //        //    contacts1.Insert(index, temp);
        //        //    //Go through source list and remove the items that are being moved
        //        //    foreach (Contact contact in DragDropListView2.Items)
        //        //    {
        //        //        if (contact.FirstName == temp.FirstName && contact.LastName == temp.LastName && contact.Company == temp.Company)
        //        //        {
        //        //            contacts2.Remove(contact);
        //        //            break;
        //        //        }
        //        //    }
        //        //}
        //        //else if (target.Name == "DragDropListView2")
        //        //{
        //        //    contacts2.Insert(index, temp);
        //        //    foreach (Contact contact in DragDropListView.Items)
        //        //    {
        //        //        if (contact.FirstName == temp.FirstName && contact.LastName == temp.LastName && contact.Company == temp.Company)
        //        //        {
        //        //            contacts1.Remove(contact);
        //        //            break;
        //        //        }
        //        //    }
        //        //}
        //    }

        //e.AcceptedOperation = DataPackageOperation.Move;
        //    def.Complete();
        //}
        def.Complete();
        e.AcceptedOperation = DataPackageOperation.None;
    }
}

public partial class FilterObject : ObservableObject
{

}

public partial class Filter : FilterObject
{
    [ObservableProperty] public string _title = "hi";
}

public partial class FilterGroup : FilterObject
{
    public ObservableCollection<FilterObject> FilterObjects = [new Filter()];
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