using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.WinUI;
using DependencyPropertyGenerator;
using EFCore.BulkExtensions;
using MediaMaster.DataBase;
using MediaMaster.Extensions;
using MediaMaster.Interfaces.Services;
using MediaMaster.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.WindowsAPICodePack.Dialogs;
using Windows.Devices.Geolocation;
using WinUI3Localizer;

namespace MediaMaster.Views.Dialog;

public sealed partial class MediaProperties : ObservableObject
{
    [ObservableProperty] private Media _media = null!;
    [ObservableProperty] private string _path = null!;
    public string OldPath = "";
    [ObservableProperty] private Visibility _showDuplicateControl = Visibility.Collapsed;
    [ObservableProperty] private Visibility _showNotValidControl = Visibility.Visible;
    [ObservableProperty] private Visibility _showValidControl = Visibility.Collapsed;
    [ObservableProperty] private bool _isDeleted;
}

[DependencyProperty("GenerateBookmarkTags", typeof(bool), DefaultValue = true, IsReadOnly = true)]
public sealed partial class FixUnlinkedMediasDialog : Page
{
    private ICollection<MediaProperties> _unlinkedMedias = [];

    public FixUnlinkedMediasDialog()
    {
        InitializeComponent();

        GetMedias();
    }

    private async void GetMedias()
    {
        await Task.Run(async () =>
        {
            await using (var database = new MediaDbContext())
            {
                List<Media> unlinkedMediaList = await database.Medias
                    .Select(m => new Media { MediaId = m.MediaId, Name = m.Name, Notes = m.Notes, Uri = m.Uri })
                    .ToListAsync();
                
                _unlinkedMedias = unlinkedMediaList.Where(m => !m.Uri.IsWebsite() && !Path.Exists(m.Uri)).Select(m => new MediaProperties { Media = m, Path = m.Uri, OldPath = m.Uri }).ToList();
            }
        });

        UnlinkedMediaListView.ItemsSource = _unlinkedMedias;
        ProgressBar.Visibility = Visibility.Collapsed;
    }

    public static async Task<(ContentDialogResult, FixUnlinkedMediasDialog?)> ShowDialogAsync()
    {
        if (App.MainWindow == null) return (ContentDialogResult.None, null);

        var fixUnlinkedMediasDialog = new FixUnlinkedMediasDialog();
        ContentDialog dialog = new()
        {
            XamlRoot = App.MainWindow.Content.XamlRoot,
            DefaultButton = ContentDialogButton.Primary,
            Content = fixUnlinkedMediasDialog,
            RequestedTheme = App.GetService<IThemeSelectorService>().ActualTheme
        };

        Uids.SetUid(dialog, "/Media/FixUnlinkedMediasDialog");
        App.GetService<IThemeSelectorService>().ThemeChanged += (_, theme) => dialog.RequestedTheme = theme;

        ContentDialogResult result;
        var errorResult = ContentDialogResult.Primary;

        do
        {
            result = await dialog.ShowAndEnqueueAsync();
            if (result == ContentDialogResult.Primary)
            {
                MediaProperties? duplicate = fixUnlinkedMediasDialog._unlinkedMedias.FirstOrDefault(m => m.ShowDuplicateControl == Visibility.Visible);
                if (duplicate != null)
                {
                    ContentDialog errorDialog = new()
                    {
                        XamlRoot = App.MainWindow.Content.XamlRoot,
                        DefaultButton = ContentDialogButton.Primary,
                        RequestedTheme = App.GetService<IThemeSelectorService>().ActualTheme
                    };

                    Uids.SetUid(errorDialog, "/Media/DuplicatesWillNotBeFixed");
                    App.GetService<IThemeSelectorService>().ThemeChanged += (_, theme) =>
                    {
                        errorDialog.RequestedTheme = theme;
                    };

                    errorResult = await errorDialog.ShowAndEnqueueAsync();
                }

                if (errorResult == ContentDialogResult.Primary)
                {
                    await fixUnlinkedMediasDialog.UpdateMedias();
                }
            }
        } while (result == ContentDialogResult.Primary && errorResult == ContentDialogResult.None);

        return (result, fixUnlinkedMediasDialog);
    }

    private async Task UpdateMedias()
    {
        await using (var database = new MediaDbContext())
        {
            await Transaction.Try(database, async () =>
            {
                var mediaPropertiesChanged = _unlinkedMedias
                    .Where(m => m is { ShowValidControl: Visibility.Visible, IsDeleted: false })
                    .ToList();

                var newTags = new Collection<Tag>();
                var oldTags = new Collection<Tag>();

                Tag? newTag = null;
                Tag? oldTag = null;

                foreach (var media in mediaPropertiesChanged)
                {
                    media.Media.Modified = DateTime.UtcNow;

                    var oldExtension = Path.GetExtension(media.OldPath);
                    var newExtension = Path.GetExtension(media.Path);
                    if (oldExtension != newExtension)
                    {
                        oldTag = await database.Medias
                            .Include(m => m.Tags)
                            .Where(m => m.MediaId == media.Media.MediaId)
                            .SelectMany(m => m.Tags)
                            .FirstOrDefaultAsync(t =>
                                t.Name == oldExtension && t.Flags.HasFlag(TagFlags.Extension));

                        if (oldTag != null)
                        {
                            MediaTag? oldMediaTag = await database.MediaTags
                                .FirstOrDefaultAsync(m => m.TagId == oldTag.TagId && m.MediaId == media.Media.MediaId);

                            if (oldMediaTag != null)
                            {
                                oldTags.Add(oldTag);
                            }
                        }

                        (var isNew, newTag) = await MediaService.GetFileTag(media.Path, database: database);
                        if (newTag != null)
                        {
                            if (isNew)
                            {
                                newTags.Add(newTag);
                            }

                            media.Media.Tags.Add(newTag);
                        }
                    }

                    if (newTag != null || oldTag != null)
                    {
                        MediaDbContext.InvokeMediaChange(
                            this,
                            MediaChangeFlags.MediaChanged | MediaChangeFlags.TagsChanged,
                            [media.Media],
                            newTag != null ? [newTag] : [],
                            oldTag != null ? [oldTag] : []);
                    }
                }

                await database.BulkDeleteAsync(oldTags);
                await database.BulkInsertAsync(newTags, new BulkConfig { SetOutputIdentity = true });
                await MediaService.AddNewTagTags(newTags, database);

                if (mediaPropertiesChanged.Count != 0)
                {
                    var mediaChanged = mediaPropertiesChanged.Select(m => m.Media).ToList();

                    await MediaService.AddNewMediaTags(mediaChanged, database);
                    await database.BulkUpdateAsync(mediaChanged);

                    MediaDbContext.InvokeMediaChange(this, MediaChangeFlags.MediaChanged | MediaChangeFlags.UriChanged,
                        mediaChanged);
                }

                var mediaDeleted = _unlinkedMedias
                    .Where(m => m.IsDeleted)
                    .Select(m => m.Media)
                    .ToList();

                if (mediaDeleted.Count != 0)
                {
                    await database.BulkDeleteAsync(mediaDeleted);

                    MediaDbContext.InvokeMediaChange(this, MediaChangeFlags.MediaRemoved, mediaDeleted);
                }
            });
        }
    }

    private async void EditPathButton_OnClick(object sender, RoutedEventArgs? routedEventArgs = null)
    {
        TextBlock? textBlock = ((FrameworkElement)((FrameworkElement)sender).Parent).FindDescendant<TextBlock>(d => d.Name == "MediaPath");
        if (textBlock == null) return;

        (CommonFileDialogResult result, var fileName) = FilePickerService.OpenFilePicker(textBlock.Text);

        if (result == CommonFileDialogResult.Ok && fileName != null && App.MainWindow != null)
        {
            await using (var database = new MediaDbContext())
            {
                if (await database.Medias.Select(m => m.Uri).ContainsAsync(fileName))
                {
                    ContentDialog errorDialog = new()
                    {
                        XamlRoot = App.MainWindow.Content.XamlRoot,
                        DefaultButton = ContentDialogButton.Primary,
                        RequestedTheme = App.GetService<IThemeSelectorService>().ActualTheme
                    };
                    Uids.SetUid(errorDialog, "/Media/FilePathAlreadyExistsDialog");
                    App.GetService<IThemeSelectorService>().ThemeChanged += (_, theme) =>
                    {
                        errorDialog.RequestedTheme = theme;
                    };

                    ContentDialogResult errorResult = await errorDialog.ShowAndEnqueueAsync();

                    if (errorResult == ContentDialogResult.Primary)
                    {
                        EditPathButton_OnClick(sender);
                    }
                }
                else
                {
                    textBlock.Text = fileName;

                    var media = (MediaProperties)textBlock.Tag;
                    UpdateMediaProperties(media, fileName);
                }
            }
        }
    }

    private void UpdateMediaProperties(MediaProperties media, string newPath)
    {
        media.ShowNotValidControl = Visibility.Collapsed;

        if (media.ShowDuplicateControl == Visibility.Visible)
        {
            List<MediaProperties> duplicates = _unlinkedMedias.Where(m => m.Path == media.Path).ToList();
            if (duplicates.Count < 3)
            {
                foreach (MediaProperties dupe in duplicates)
                {
                    dupe.ShowDuplicateControl = Visibility.Collapsed;
                    dupe.ShowValidControl = Visibility.Visible;
                }
            }
        }

        MediaProperties? duplicate = _unlinkedMedias.FirstOrDefault(m => m.Path == newPath);
        if (duplicate != null)
        {
            media.ShowDuplicateControl = Visibility.Visible;
            duplicate.ShowDuplicateControl = Visibility.Visible;

            media.ShowValidControl = Visibility.Collapsed;
            duplicate.ShowValidControl = Visibility.Collapsed;
        }
        else
        {
            media.ShowDuplicateControl = Visibility.Collapsed;
            media.ShowDuplicateControl = Visibility.Collapsed;
            media.ShowValidControl = Visibility.Visible;
        }

        media.Media.Uri = newPath;
        media.Path = newPath;
    }

    private async void DeleteMediaButton_OnClick(object sender, RoutedEventArgs? routedEventArgs = null)
    {
        TextBlock? textBlock = ((FrameworkElement)((FrameworkElement)sender).Parent).FindDescendant<TextBlock>(d => d.Name == "MediaPath");
        if (textBlock == null) return;

        if (App.MainWindow == null) return;

        ContentDialog dialog = new()
        {
            XamlRoot = App.MainWindow.Content.XamlRoot,
            DefaultButton = ContentDialogButton.Close,
            RequestedTheme = App.GetService<IThemeSelectorService>().ActualTheme
        };
        Uids.SetUid(dialog, "/Media/DeleteUnlinkedDialog");
        App.GetService<IThemeSelectorService>().ThemeChanged += (_, theme) => dialog.RequestedTheme = theme;
        ContentDialogResult result = await dialog.ShowAndEnqueueAsync();

        if (result != ContentDialogResult.Primary) return;

        var media = (MediaProperties)textBlock.Tag;
        media.IsDeleted = true;
    }

    private async void PathsFromDirectoryButton_OnClick(object sender, RoutedEventArgs e)
    {
        (CommonFileDialogResult result, var folderPath) = FilePickerService.OpenFilePicker(null, true);

        if (result == CommonFileDialogResult.Ok && folderPath != null)
        {
            ICollection<KeyValuePair<string, MediaProperties>> mediaToUpdate = [];
            await Task.Run(() =>
            {
                EnumerationOptions opt = new()
                {
                    RecurseSubdirectories = true,
                    IgnoreInaccessible = true,
                    ReturnSpecialDirectories = true
                };
                Dictionary<string, List<string>> directories = Directory.EnumerateFiles(folderPath, "*", opt)
                    .GroupBy(Path.GetFileName)
                    .ToDictionary(g => g.Key!, g => g.ToList());

                Dictionary<string, List<MediaProperties>> mediaDictionary = _unlinkedMedias
                    .Where(m => m.ShowValidControl != Visibility.Visible)
                    .GroupBy(m => Path.GetFileName(m.Media.Uri))
                    .ToDictionary(g => g.Key, g => g.ToList());


                foreach ((var name, List<MediaProperties> medias) in mediaDictionary)
                {
                    if (directories.TryGetValue(name, out List<string>? paths))
                    {
                        List<Tuple<string, MediaProperties>> matches = PairFilePaths(medias, paths);
                        foreach ((var path, MediaProperties mediaProperties) in matches)
                        {
                            mediaToUpdate.Add(new KeyValuePair<string, MediaProperties>(path, mediaProperties));
                        }
                    }
                }
            });
            foreach ((var file, MediaProperties mediaProperties) in mediaToUpdate)
            {
                UpdateMediaProperties(mediaProperties, file);
            }
        }
    }

    private static List<Tuple<string, MediaProperties>> PairFilePaths(List<MediaProperties> medias,
        List<string> existingPaths)
    {
        Dictionary<string, Tuple<Tuple<int, int>, MediaProperties?>> pairedMediaPaths = [];

        foreach (MediaProperties media in medias)
        {
            string? bestMatch = null;
            var bestMatchScoreStart = 0;
            var bestMatchScoreEnd = int.MaxValue;

            foreach (var existingPath in existingPaths)
            {
                var scoreStart = GetFirstDifferenceIndex(media.Media.Uri, existingPath);
                var scoreEnd = GetLastDifferenceIndex(media.Media.Uri, existingPath);

                if (scoreStart > bestMatchScoreStart || scoreEnd > bestMatchScoreEnd)
                {
                    bestMatchScoreStart = scoreStart;
                    bestMatchScoreEnd = scoreEnd;
                    bestMatch = existingPath;
                }
            }

            if (bestMatch != null)
            {
                MediaProperties? mediaToUse;
                if (pairedMediaPaths.TryGetValue(bestMatch, out Tuple<Tuple<int, int>, MediaProperties?>? tuple))
                {
                    if (tuple.Item1.Item1 >= bestMatchScoreStart)
                    {
                        mediaToUse = tuple.Item1.Item1 == bestMatchScoreStart && tuple.Item1.Item2 < bestMatchScoreEnd
                            ? media
                            : null;
                    }
                    else
                    {
                        mediaToUse = null;
                    }
                }
                else
                {
                    mediaToUse = media;
                }

                pairedMediaPaths[bestMatch] =
                    new Tuple<Tuple<int, int>, MediaProperties?>(
                        new Tuple<int, int>(bestMatchScoreStart, bestMatchScoreEnd), mediaToUse);
            }
        }

        return pairedMediaPaths
            .Where(t => t.Value.Item2 != null)
            .Select(d => new Tuple<string, MediaProperties>(d.Key, d.Value.Item2!))
            .ToList();
    }

    private static int GetFirstDifferenceIndex(string str1, string str2)
    {
        var minLength = Math.Min(str1.Length, str2.Length);

        for (var i = 0; i < minLength; i++)
        {
            if (str1[i] != str2[i])
            {
                return i;
            }
        }

        if (str1.Length != str2.Length)
        {
            return minLength;
        }

        return int.MaxValue;
    }

    private static int GetLastDifferenceIndex(string str1, string str2)
    {
        str1 = new string(str1.Reverse().ToArray());
        str2 = new string(str2.Reverse().ToArray());

        var minLength = Math.Min(str1.Length, str2.Length);

        for (var i = 0; i < minLength; i++)
        {
            if (str1[i] != str2[i])
            {
                return i;
            }
        }

        if (str1.Length != str2.Length)
        {
            return minLength;
        }

        return int.MaxValue;
    }
}