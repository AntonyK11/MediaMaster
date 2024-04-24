using System.Collections.ObjectModel;
using System.Diagnostics;
using BookmarksManager;
using BookmarksManager.Chrome;
using BookmarksManager.Firefox;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.WinUI;
using Interop.UIAutomationClient;
using MediaMaster.Helpers;
using TreeScope = Interop.UIAutomationClient.TreeScope;


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
}

public partial class BrowserTab : ObservableObject
{
    [ObservableProperty] private string _browser;
    [ObservableProperty] private string _browserIcon;
    [ObservableProperty] private string _url;
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

    private readonly Stopwatch _watch = new();

    private IEnumerable<TimeSpan> elapsedspans = [];

    private static TimeSpan Average(IEnumerable<TimeSpan> spans) => TimeSpan.FromMilliseconds(spans.Select(s => s.TotalMilliseconds).Average());

    private bool _isRunning;
    public async Task FindActiveTabs()
    {
        if (BrowserData is null || _isRunning) return;

        _isRunning = true;

        _watch.Restart();

        var tasks = BrowserData.Select(browser => ProcessBrowserInstances(browser));
        await Task.WhenAll(tasks);

        _watch.Stop();
        //if (elapsedspans.Count() > 1) elapsedspans = elapsedspans.Skip(1);
        //elapsedspans = elapsedspans.Append(_watch.Elapsed);
        //Debug.WriteLine($"{_watch.ElapsedMilliseconds}ms");
        Debug.WriteLine("-----------------------------");
        Debug.WriteLine($"{_watch.ElapsedMilliseconds}ms");
        Debug.WriteLine("-----------------------------");

        //await Task.Run(async () =>
        //{
        //    _watch.Restart();
        //    foreach (var browser in BrowserData)
        //    {
        //        try
        //        {
        //            await ProcessBrowserInstances(browser);
        //        }
        //        catch (Exception)
        //        {
        //            // ignore any exceptions as we don't want to crash the app if something goes wrong, we just want to skip the browser
        //        }
        //    }
        //    _watch.Stop();
        //    if (elapsedspans.Count() > 3) elapsedspans = elapsedspans.Skip(1);
        //    elapsedspans = elapsedspans.Append(_watch.Elapsed);
        //    //Debug.WriteLine($"{_watch.ElapsedMilliseconds}ms");
        //    Debug.WriteLine($"{Average(elapsedspans).TotalMilliseconds}ms");
        //});
        _isRunning = false;
    }

    public async Task RefreshActiveTab(string browserName)
    {
        var browserData = BrowserData?.FirstOrDefault(b => b.Name == browserName);
        if (browserData != null)
        {
            await Task.Run(async () =>
            {
                try
                {
                    await ProcessBrowserInstances(browserData, false);
                }
                catch (Exception)
                {
                    // ignore any exceptions as we don't want to crash the app if something goes wrong, we just want to skip the browser
                }
            });
        }
    }

    // https://stackoverflow.com/a/70748896
    public async Task ProcessBrowserInstances(BrowserData browser, bool cache = true)
    {
        Stopwatch watch = new();
        watch.Start();

        var skip = false;
        var found = false;
        var url = "";

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
                    return mainModule is not null && !mainModule.FileName.Contains("GX");
                }),

                "Opera GX" => processes.Where(p =>
                {
                    if (p.HasExited) return false;
                    var mainModule = p.MainModule;
                    return mainModule is not null && mainModule.FileName.Contains("GX");
                }),

                _ => processes
            };

            if (browser.Name is "Opera GX")
            {
                var tempHWND = WindowsApiService.GetForegroundWindow();
                List<Process> processesList = processes.ToList();
                if (processesList.FirstOrDefault(p => p.MainWindowHandle == tempHWND) is { } process)
                {
                    processesList.Remove(process);
                    processesList.Insert(0, process);
                    operaGXHWND = tempHWND;
                }
                else if (processesList.FirstOrDefault(p => p.MainWindowHandle == operaGXHWND) is { } processCached)
                {
                    processesList.Remove(processCached);
                    processesList.Insert(0, processCached);
                }

                processes = processesList;
            }

            foreach (var process in processes)
            {
                if (!process.HasExited && process.MainWindowHandle != IntPtr.Zero)
                {
                    if (cache
                        && _browsersWindowTitle.TryGetValue(browser.Name, out var title)
                        && title == process.MainWindowTitle)
                    {
                        skip = true;
                        break;
                    }

                    var result = GetBrowserTab(process.MainWindowHandle);
                    if (result is not null)
                    {
                        url = result;
                        found = true;

                        if (!process.HasExited)
                        {
                            _browsersWindowTitle[browser.Name] = process.MainWindowTitle;
                        }

                        break;
                    }
                }

                _browsersWindowTitle.Remove(browser.Name);
            }
        });

        var tab = ActiveBrowserTabs.FirstOrDefault(t => t.Browser == browser.Name);
        if (tab is not null)
        {
            watch.Stop();
            Debug.WriteLine($"{browser.Name} - {watch.ElapsedMilliseconds}ms");
            if (!found)
            {
                if (skip) return;
                ActiveBrowserTabs.Remove(tab);
            }
            else
            {
                tab.Url = url;
            }
            return;
        }
        if (found)
        {
            ActiveBrowserTabs.Add(new BrowserTab { Browser = browser.Name, BrowserIcon = browser.Icon, Url = url });
        }

        watch.Stop();
        Debug.WriteLine($"{browser.Name} - {watch.ElapsedMilliseconds}ms");
    }

    public string? GetBrowserTab(nint hwnd)
    {
        IUIAutomationElement? element = _automation.ElementFromHandle(hwnd);
        //IUIAutomationCondition condToolBarControlType = _automation.CreatePropertyCondition(UIA_PropertyIds.UIA_ControlTypePropertyId, UIA_ControlTypeIds.UIA_ToolBarControlTypeId);
        //IUIAutomationCondition condName = _automation.CreateNotCondition(_automation.CreatePropertyCondition(UIA_PropertyIds.UIA_NamePropertyId, ""));
        //IUIAutomationCondition condition = _automation.CreateAndCondition(condToolBarControlType, condName);
        IUIAutomationCondition condEditControlType = _automation.CreatePropertyCondition(UIA_PropertyIds.UIA_ControlTypePropertyId, UIA_ControlTypeIds.UIA_EditControlTypeId);
        IUIAutomationCondition condKeyboard = _automation.CreatePropertyCondition(UIA_PropertyIds.UIA_IsKeyboardFocusablePropertyId, true);
        IUIAutomationCondition condition1 = _automation.CreateAndCondition(condEditControlType, condKeyboard);

        IUIAutomationCondition condName = _automation.CreateNotCondition(_automation.CreatePropertyCondition(UIA_PropertyIds.UIA_NamePropertyId, ""));
        while (true)
        {
            //IUIAutomationElement? elem = element.FindFirst(TreeScope.TreeScope_Descendants, condition);
            //if (elem is null) break;
            //condName = _automation.CreateAndCondition(condName, _automation.CreateNotCondition(_automation.CreatePropertyCondition(UIA_PropertyIds.UIA_NamePropertyId, elem.CurrentName)));
            //condition = _automation.CreateAndCondition(condToolBarControlType, condName);


            IUIAutomationCondition condition2 = _automation.CreateAndCondition(condition1, condName);
            //IUIAutomationCondition cond3 = _automation.CreatePropertyCondition(UIA_PropertyIds.UIA_ValueIsReadOnlyPropertyId, false);
            //condition = _automation.CreateAndCondition(condition, cond3);

            //IUIAutomationCondition cond4 = _automation.CreatePropertyCondition(UIA_PropertyIds.UIA_NamePropertyId, true); // Added to fix weird issue with Firefox where another element was found that was not the URL

            IUIAutomationElement? elem = element.FindFirst(TreeScope.TreeScope_Descendants, condition2);
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