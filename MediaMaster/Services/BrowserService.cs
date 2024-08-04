using System.Collections.ObjectModel;
using BookmarksManager;
using BookmarksManager.Chrome;
using BookmarksManager.Firefox;
using CommunityToolkit.Mvvm.ComponentModel;
using Interop.UIAutomationClient;
using MediaMaster.Extensions;
using MediaMaster.Helpers;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Media.Imaging;


namespace MediaMaster.Services;

public class BrowserData
{
    public required string Name { get; set; }
    public required string Icon { get; set; }
    public required string ProfilesDirectory { get; set; }
    public required string ProcessName { get; set; }
    public string? PackageId { get; set; }
    public required bool IsOnlyPackaged { get; set; }
    public required string BookmarkFormat { get; set; }
    public required bool IsInLocalAppData { get; set; }
    public required bool HasProfiles { get; set; }

    public required string TabEndingString { get; set; }
}

public partial class BrowserTab : ObservableObject
{
    [ObservableProperty] private BrowserData _browser;
    [ObservableProperty] private ImageSource _icon;
    [ObservableProperty] private string _domain;
    [ObservableProperty] private string _title;
    [ObservableProperty] private Uri _url;
}

public class BrowserService
{

    public static BrowserData[]? BrowserData { get; set; }

    private readonly CUIAutomation _automation = new();

    public ObservableCollection<BrowserTab> ActiveBrowserTabs = [];

    private Dictionary<string, string> _browsersWindowTitle = [];

    private nint operaGXHWND = IntPtr.Zero;

    private static string LocalAppDataPath => Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
    private static string AppDataPath => Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
    
    public async Task InitializeAsync()
    {
        var browsersDataFilePath = Path.Combine(AppContext.BaseDirectory, "Data", "Browsers.json");
        var browsersDataString = await File.ReadAllTextAsync(browsersDataFilePath);
        BrowserData = await Json.ToObjectAsync<BrowserData[]>(browsersDataString);
    }

    private bool _isRunning;
    public async Task FindActiveTabs()
    {
        if (BrowserData is null || _isRunning) return;

        _isRunning = true;

        var tasks = BrowserData.Select(browser => ProcessBrowserInstances(browser));
        await Task.WhenAll(tasks);

        _isRunning = false;
    }

    public async Task<BrowserTab?> RefreshActiveTab(string browserName)
    {
        BrowserTab? tab = null;

        var browserData = BrowserData?.FirstOrDefault(b => b.Name == browserName);
        if (browserData != null)
        {
            await Task.Run(async () =>
            {
                try
                {
                    tab = await ProcessBrowserInstances(browserData, false);
                }
                catch (Exception)
                {
                    // ignore any exceptions as we don't want to crash the app if something goes wrong, we just want to skip the browser
                }
            });
        }
        return tab;
    }

    // https://stackoverflow.com/a/70748896
    public async Task<BrowserTab?> ProcessBrowserInstances(BrowserData browser, bool cache = true)
    {
        var found = false;
        var title = "";
        Uri? url = null;
        BrowserTab? tab = ActiveBrowserTabs.FirstOrDefault(t => t.Browser.Name == browser.Name);

        await Task.Run(() =>
        {
            IEnumerable<Process> processes = Process.GetProcessesByName(browser.ProcessName);
            if (!processes.Any()) return;

            processes = browser.Name switch
            {
                "Opera" => processes.Where(p =>
                {
                    if (p.HasExited) return false;
                    var mainModule = p.MainModule;
                    return mainModule != null && !mainModule.FileName.Contains("GX");
                }),

                "Opera GX" => processes.Where(p =>
                {
                    if (p.HasExited) return false;
                    var mainModule = p.MainModule;
                    return mainModule != null && mainModule.FileName.Contains("GX");
                }),

                _ => processes
            };

            if (browser.Name is "Opera GX")
            {
                var tempHWND = WindowsApiService.GetForegroundWindow();
                List<Process> processesList = processes.ToList();
                if (processesList.FirstOrDefault(p => p.MainWindowHandle == tempHWND) is { } foregroundProcess)
                {
                    processesList.Remove(foregroundProcess);
                    processesList.Insert(0, foregroundProcess);
                    operaGXHWND = tempHWND;
                }
                else if (processesList.FirstOrDefault(p => p.MainWindowHandle == operaGXHWND) is { } cachedForegroundProcess)
                {
                    processesList.Remove(cachedForegroundProcess);
                    processesList.Insert(0, cachedForegroundProcess);
                }

                processes = processesList;
            }

            string? windowTitle = null;
            if (cache)
            {
                _browsersWindowTitle.TryGetValue(browser.Name, out windowTitle);
            }

            foreach (var process in processes)
            {
                if (!process.HasExited && process.MainWindowHandle != IntPtr.Zero && WindowsApiService.IsWindow(process.MainWindowHandle))
                {
                    if (cache && process.MainWindowTitle == windowTitle && tab != null)
                    {
                        found = true;
                        break;
                    }

                    try
                    {
                        title = process.MainWindowTitle;

                        var result = GetBrowserTab(process.MainWindowHandle);
                        if (!result.IsNullOrEmpty())
                        {
                            if (result!.IsWebsite())
                            {
                                result = result!.Replace("://www.", "://");
                                if (Uri.IsWellFormedUriString(result, UriKind.Absolute))
                                {
                                    url = new Uri(result);
                                }
                            }
                            else
                            {
                                if (result!.StartsWith("www."))
                                {
                                    result = result.Replace("www.", "");
                                }
                                if (Uri.IsWellFormedUriString("https://" + result, UriKind.Absolute))
                                {
                                    url = new Uri("https://" + result);
                                }
                            }

                            found = true;
                            break;
                        }
                    }
                    catch
                    {
                        // Catch any exceptions
                    }
                }
            }
        });

        if (found)
        {
            _browsersWindowTitle[browser.Name] = title;

            if (url != null)
            {
                if (tab == null)
                {
                    tab = new BrowserTab { Browser = browser };
                    ActiveBrowserTabs.Add(tab);
                }

                if (tab.Title != title)
                {
                    tab.Title = title;
                }

                if (tab.Url != url)
                {
                    tab.Url = url;

                    if (tab.Domain != tab.Url.Host)
                    {
                        var uri = new Uri("ms-appx://MediaMaster/" + browser.Icon);
                        if (browser.Icon.EndsWith(".svg"))
                        {
                            tab.Icon = new SvgImageSource(uri);
                        }
                        else
                        {
                            tab.Icon = new BitmapImage(uri);
                        }

                        tab.Domain = tab.Url.Host;
                    }
                }
            }
        }
        else if (tab != null)
        {
            ActiveBrowserTabs.Remove(tab);
            _browsersWindowTitle.Remove(browser.Name);
        }

        return tab;
    }

    public string? GetBrowserTab(nint hwnd)
    {
        IUIAutomationElement? element = _automation.ElementFromHandle(hwnd);

        IUIAutomationCondition condEditControlType = _automation.CreatePropertyCondition(UIA_PropertyIds.UIA_ControlTypePropertyId, UIA_ControlTypeIds.UIA_EditControlTypeId);
        IUIAutomationCondition condKeyboard = _automation.CreatePropertyCondition(UIA_PropertyIds.UIA_IsKeyboardFocusablePropertyId, true);
        IUIAutomationCondition condition = _automation.CreateAndCondition(condEditControlType, condKeyboard);

        IUIAutomationCondition condName = _automation.CreateNotCondition(_automation.CreatePropertyCondition(UIA_PropertyIds.UIA_NamePropertyId, ""));
        while (true)
        {
            IUIAutomationCondition variableCondition = _automation.CreateAndCondition(condition, condName);

            IUIAutomationElement? elem = element.FindFirst(TreeScope.TreeScope_Descendants, variableCondition);
            if (elem is null)
            {
                break;
            }

            if (!elem.CurrentName.Contains("address", StringComparison.InvariantCultureIgnoreCase))
            {
                condName = _automation.CreateAndCondition(condName, _automation.CreateNotCondition(_automation.CreatePropertyCondition(UIA_PropertyIds.UIA_NamePropertyId, elem.CurrentName)));
                continue;
            }

            return ((IUIAutomationValuePattern)elem.GetCurrentPattern(UIA_PatternIds.UIA_ValuePatternId)).CurrentValue;
        }

        return null;
    }

    public static async Task<BookmarkFolder> GetBookmarks()
    {
        return await Task.Run(async () =>
        {
            var folder = new BookmarkFolder("Bookmarks");

            if (BrowserData is null) return folder;

            foreach (BrowserData browser in BrowserData)
            {
                folder.Add(await GetBrowserBookmarks(browser));
            }

            return folder;
        });
    }

    private static async Task<BookmarkFolder> GetBrowserBookmarks(BrowserData browser)
    {
        List<BookmarkFolder> bookmarkFolders = [];

        if (browser.PackageId is not null)
        {
            bookmarkFolders.AddRange(await GetPackagedBrowserBookmarks(browser));
        }

        if (!browser.IsOnlyPackaged)
        {
            bookmarkFolders.AddRange(await GetNonPackagedBrowserBookmarks(browser));
        }

        return BuildBookmarkFolder(browser, bookmarkFolders);

    }
    
    private static BookmarkFolder BuildBookmarkFolder(BrowserData browser, IEnumerable<BookmarkFolder> bookmarkFolders)
    {
        BookmarkFolder bookmarksFolder = new(browser.Name);
        
        foreach (var bookmarkFolder in bookmarkFolders)
        {
            // Used to remove the empty folder without title in Opera and Opera GX
            foreach (var bookmark in bookmarkFolder.ToList())
            {
                if (bookmark.Title is null && bookmark is BookmarkFolder folder && !folder.Any())
                {
                    bookmarkFolder.Remove(bookmark);
                }
            }

            if (browser.HasProfiles || (browser.PackageId is not null && !browser.IsOnlyPackaged))
            {
                bookmarksFolder.Add(bookmarkFolder);
            }
            else
            {
                foreach (var bookmark in bookmarkFolder)
                {
                    bookmarksFolder.Add(bookmark);
                }
            }
        }

        return bookmarksFolder;
    }
    
    private static async Task<IEnumerable<BookmarkFolder>> GetPackagedBrowserBookmarks(BrowserData browser)
    {
        var packageFolderPath = Path.Combine(LocalAppDataPath, "Packages");

        if (Directory.Exists(packageFolderPath) && browser.PackageId is not null)
        {
            var packagePath = Directory.GetDirectories(packageFolderPath).FirstOrDefault(s => s.Contains(browser.PackageId));

            if (packagePath is not null)
            {
                var storagePath = browser.IsInLocalAppData ? "Local" : "Roaming";
                var browserPackagePath = Path.Combine(packagePath, $"LocalCache\\{storagePath}", browser.ProfilesDirectory);

                return await GetBrowserBookmarksPerFormat(browser, browserPackagePath);
            }
        }

        return [];
    }
    
    private static async Task<IEnumerable<BookmarkFolder>> GetNonPackagedBrowserBookmarks(BrowserData browser)
    {
        var storagePath = browser.IsInLocalAppData ? LocalAppDataPath : AppDataPath;
        var browserStoragePath = Path.Combine(storagePath, browser.ProfilesDirectory);
        return await GetBrowserBookmarksPerFormat(browser, browserStoragePath);
    }
    
    private static async Task<IEnumerable<BookmarkFolder>> GetBrowserBookmarksPerFormat(BrowserData browser, string browserStoragePath)
    {
        return browser.BookmarkFormat switch
        {
            "chromium" => await GetChromiumBookmarks(browserStoragePath, browser),
            "opera-gx" => await GetChromiumBookmarks(browserStoragePath, browser),
            "firefox" => GetFirefoxBookmarks(browserStoragePath),
            _ => []
        };
    }

    private static async Task<ICollection<BookmarkFolder>> GetChromiumBookmarks(string browserPath, BrowserData browser)
    {
        ICollection<BookmarkFolder> bookmarksFolder = [];

        if (!Directory.Exists(browserPath)) return bookmarksFolder;

        BookmarkFolder? bookmarks;

        if (browser.HasProfiles)
        {
            IEnumerable<string> profiles = Directory.GetDirectories(browserPath).Where(d => d.Contains("Profile") || d.Contains("Default"));

            // Opera GX has a slightly different bookmark format
            if (browser.BookmarkFormat == "opera-gx")
            {
                bookmarks = await GetChromiumBookmarks(browserPath, "Default");
                if (bookmarks is not null) bookmarksFolder.Add(bookmarks);

                var profilesPath = Path.Combine(browserPath, "_side_profiles");
                profiles = Directory.GetDirectories(profilesPath);
            }

            foreach (var profile in profiles)
            {
                bookmarks = await GetChromiumBookmarks(profile, Path.GetFileName(profile));
                if (bookmarks is not null) bookmarksFolder.Add(bookmarks);
            }
        }
        else
        {
            bookmarks = await GetChromiumBookmarks(browserPath, Path.GetFileName(browser.Name));
            if (bookmarks is not null) bookmarksFolder.Add(bookmarks);
        }

        return bookmarksFolder;
    }

    public static async Task<BookmarkFolder?> GetChromiumBookmarks(string browserPath, string bookmarksFolderTitle)
    {
        var bookmarkPath = Path.Combine(browserPath, "Bookmarks");
        if (!File.Exists(bookmarkPath)) return null;
        
        await using var file = File.OpenRead(bookmarkPath);
        BookmarkFolder bookmarks = new ChromeBookmarksReader().Read(file);
        bookmarks.Title = bookmarksFolderTitle;
        return bookmarks;

    }

    private static IEnumerable<BookmarkFolder> GetFirefoxBookmarks(string browserPath)
    {
        ICollection<BookmarkFolder> bookmarksFolder = [];
        
        if (!Directory.Exists(browserPath)) return bookmarksFolder;
        
        IEnumerable<string> profiles = Directory.GetDirectories(browserPath);

        foreach (var profile in profiles)
        {
            var dbPath = Path.Combine(profile, "places.sqlite");

            if (!File.Exists(dbPath)) continue;

            BookmarkFolder bookmarks = new FirefoxBookmarksReader(dbPath).Read();

            bookmarks.Title = Path.GetFileName(profile);
            bookmarksFolder.Add(bookmarks);
        }

        return bookmarksFolder;
    }
}