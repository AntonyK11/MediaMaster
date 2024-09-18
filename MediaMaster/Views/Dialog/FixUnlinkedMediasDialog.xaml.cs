using System.Reflection;
using CommunityToolkit.Mvvm.ComponentModel;
using DependencyPropertyGenerator;
using EFCore.BulkExtensions;
using MediaMaster.Controls;
using MediaMaster.DataBase;
using MediaMaster.Extensions;
using MediaMaster.Interfaces.Services;
using MediaMaster.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.WindowsAPICodePack.Dialogs;
using WinUI3Localizer;
using WinUIEx;

namespace MediaMaster.Views.Dialog;

public partial class MediaProperties : ObservableObject
{
    [ObservableProperty] private Media _media = null!;
    [ObservableProperty] private string _path = null!;
    [ObservableProperty] private Visibility _showDuplicateControl = Visibility.Collapsed;
    [ObservableProperty] private Visibility _showNotValidControl = Visibility.Visible;
    [ObservableProperty] private Visibility _showValidControl = Visibility.Collapsed;
}

[DependencyProperty("GenerateBookmarkTags", typeof(bool), DefaultValue = true, IsReadOnly = true)]
public partial class FixUnlinkedMediasDialog : Page
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
                
                _unlinkedMedias = unlinkedMediaList.Where(m => !m.Uri.IsWebsite() && !Path.Exists(m.Uri)).Select(m => new MediaProperties { Media = m, Path = m.Uri }).ToList();
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
        App.GetService<IThemeSelectorService>().ThemeChanged += (_, theme) => { dialog.RequestedTheme = theme; };

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
                    await using (var database = new MediaDbContext())
                    {
                        await database
                            .BulkUpdateAsync(
                                fixUnlinkedMediasDialog
                                    ._unlinkedMedias
                                    .Where(m => m.ShowValidControl == Visibility.Visible)
                                    .Select(m => m.Media)
                                    .ToList()
                            );
                    }
                }
            }
        } while (result == ContentDialogResult.Primary && errorResult == ContentDialogResult.None);

        return (result, fixUnlinkedMediasDialog);
    }

    private async void EditableTextBlock_OnEditButtonPressed(EditableTextBlock sender, string args)
    {
        (CommonFileDialogResult result, var fileName) = FilePickerService.OpenFilePicker(sender.Text);

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
                        EditableTextBlock_OnEditButtonPressed(sender, args);
                    }
                }
                else
                {
                    sender.Text = fileName;

                    var media = (MediaProperties)sender.Tag;
                    UpdateMedia(media, fileName);
                }
            }
        }
    }

    private void UpdateMedia(MediaProperties media, string newPath)
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

    private async void ButtonBase_OnClick(object sender, RoutedEventArgs e)
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
                UpdateMedia(mediaProperties, file);
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