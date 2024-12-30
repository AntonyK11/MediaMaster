using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.WinUI.Collections;
using MediaMaster.Helpers;
using MediaMaster.Views;
using Windows.Storage;
using MediaMaster.Extensions;
using System.Text.Json;
using CommunityToolkit.WinUI;
using WinUI3Localizer;
using WinUICommunity;

namespace MediaMaster.Services;

public sealed class SearchSavingService
{
    public AdvancedCollectionView SavedSearches { get; }
    private readonly ObservableCollection<SavedSearch> _savedSearchesCollection = [];

    public SearchSavingService()
    {
        SavedSearches = new AdvancedCollectionView(_savedSearchesCollection);
        SavedSearches.SortDescriptions.Add(new SortDescription("Name", SortDirection.Ascending));

        SetupSavedSearches();
    }

    private async void SetupSavedSearches()
    {
        List<StoredSearch> storedSearches = await GetStoredSearches();
        foreach (var storedSearch in storedSearches)
        {
            SavedSearches.Add(new SavedSearch(storedSearch.Name));
        }
    }

    public SavedSearch AddSavedSearch(string name, ICollection<FilterObject> filterObjects)
    {
        DeleteSavedSearch(name);
        var savedSearch = new SavedSearch(name);
        SavedSearches.Add(savedSearch);
        SetSavedSearch(name, new StoredSearch { Name = name, FilterObjects = filterObjects });

        return savedSearch;
    }

    public void DeleteSavedSearch(string name)
    {
        var savedSearch = _savedSearchesCollection.FirstOrDefault(s => s.Name == name);
        if (savedSearch != null)
        {
            SavedSearches.Remove(savedSearch);
        }
    }

    public async Task<ICollection<FilterObject>?> GetSavedSearch(string name)
    {
        var savedSearch = (await GetStoredSearches()).FirstOrDefault(s => s.Name == name);
        return savedSearch?.FilterObjects;
    }

    public async void SetSavedSearch(string name, StoredSearch? addStoredSearch = null)
    {
        List<StoredSearch> storedSearches = await GetStoredSearches();

        var removeStoredSearch = storedSearches.FirstOrDefault(s => s.Name == name);
        if (removeStoredSearch != null)
        {
            storedSearches.Remove(removeStoredSearch);
        }

        if (addStoredSearch != null)
        {
            storedSearches.Add(addStoredSearch);
        }

        await StoreSearches(storedSearches);
    }

    private static async Task<List<StoredSearch>> GetStoredSearches()
    {
        var storageItem = await ApplicationData.Current.LocalFolder.CreateFileAsync("Searches.json", CreationCollisionOption.OpenIfExists);
        var browsersDataString = await File.ReadAllTextAsync(storageItem.Path);
        if (browsersDataString.IsNullOrEmpty()) return [];
        try
        {
            return JsonSerializer.Deserialize(browsersDataString, SourceGenerationContext.Default.ListStoredSearch) ?? [];
        }
        catch (Exception e)
        {
            Debug.WriteLine(e);
            await App.DispatcherQueue.EnqueueAsync(() =>
            {
                Growl.Error(new GrowlInfo
                {
                    ShowDateTime = true,
                    IsClosable = true,
                    Title = string.Format("InAppNotification_Title".GetLocalizedString(), DateTimeOffset.Now),
                    Message = $"{"InAppNotification_Error".GetLocalizedString()}\n\n{e.Message}\n{e.InnerException}"
                });
            });
            return [];
        }
    }

    private static async Task StoreSearches(List<StoredSearch> storedSearches)
    {
        var text = JsonSerializer.Serialize(storedSearches, SourceGenerationContext.Default.ListStoredSearch);
        await File.WriteAllTextAsync(Path.Combine(ApplicationData.Current.LocalFolder.Path, "Searches.json"), text);
    }
}

public partial class SavedSearch : ObservableObject
{
    [ObservableProperty] public partial string Name { get; set; }

    public SavedSearch(string name)
    {
        Name = name;
    }

    public void Delete()
    {
        App.GetService<SearchSavingService>().DeleteSavedSearch(Name);
        App.GetService<SearchSavingService>().SetSavedSearch(Name);
    }

    public async Task<ICollection<FilterObject>> GetFilterObjects()
    {
        return await App.GetService<SearchSavingService>().GetSavedSearch(Name) ?? [];
    }
}

public class StoredSearch
{
    public required string Name { get; set; }
    public required ICollection<FilterObject> FilterObjects { get; set; }
}