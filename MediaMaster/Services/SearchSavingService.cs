using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.WinUI.Collections;
using MediaMaster.Helpers;
using MediaMaster.Views;
using Windows.Storage;
using MediaMaster.Extensions;
using System.Text.Json;
using System.Text.Json.Serialization;
using DocumentFormat.OpenXml.Wordprocessing;
using System.Xml.Linq;
using System.Collections.Generic;

namespace MediaMaster.Services;

public class SearchSavingService
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

    public void AddSavedSearch(string name, ICollection<FilterObject> filterObjects)
    {
        DeleteSavedSearch(name);
        SavedSearches.Add(new SavedSearch(name));
        SetSavedSearch(name, new StoredSearch { Name = name, FilterObjects = filterObjects });
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

    private readonly JsonSerializerOptions _options = new()
    {
        WriteIndented = true,
        Converters = { new FilterObjectConverter(), new OperationsConverter() },
        TypeInfoResolver = SourceGenerationContext.Default
    };

    private async Task<List<StoredSearch>> GetStoredSearches()
    {
        var storageItem = await ApplicationData.Current.LocalFolder.CreateFileAsync("Searches.json", CreationCollisionOption.OpenIfExists);
        var browsersDataString = await File.ReadAllTextAsync(storageItem.Path);
        if (browsersDataString.IsNullOrEmpty()) return [];
        return JsonSerializer.Deserialize(browsersDataString, typeof(List<StoredSearch>), _options) as List<StoredSearch> ?? [];
    }

    private async Task StoreSearches(List<StoredSearch> storedSearches)
    {
        var text = JsonSerializer.Serialize(storedSearches, typeof(List<StoredSearch>), _options);
        await File.WriteAllTextAsync(Path.Combine(ApplicationData.Current.LocalFolder.Path, "Searches.json"), text);
    }
}

public partial class SavedSearch : ObservableObject
{
    [ObservableProperty] private string _name;

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

public class FilterObjectConverter : JsonConverter<FilterObject>
{
    public override FilterObject Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        using (JsonDocument doc = JsonDocument.ParseValue(ref reader))
        {
            if (doc.RootElement.TryGetProperty("_orCombination", out _))
            {
                return JsonSerializer.Deserialize(doc.RootElement.GetRawText(), typeof(FilterGroup), options) as FilterGroup;
            }
            else
            {
                return JsonSerializer.Deserialize(doc.RootElement.GetRawText(), typeof(Filter), options) as Filter;
            }
        }
    }

    public override void Write(Utf8JsonWriter writer, FilterObject value, JsonSerializerOptions options)
    {
        if (value is FilterGroup filterGroup)
        {
            JsonSerializer.Serialize(writer, filterGroup, typeof(FilterGroup), options);
        }
        else if (value is Filter filter)
        {
            JsonSerializer.Serialize(writer, filter, typeof(Filter), options);
        }
    }
}

public class OperationsConverter : JsonConverter<Operations>
{
    public override Operations Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        using (JsonDocument doc = JsonDocument.ParseValue(ref reader))
        {
            doc.RootElement.TryGetProperty("Name", out var name);

            if (name.GetString() == "TextOperations")
            {
                return JsonSerializer.Deserialize(doc.RootElement.GetRawText(), typeof(TextOperations), options) as TextOperations;
            }
            else if (name.GetString() == "DateOperations")
            {
                return JsonSerializer.Deserialize(doc.RootElement.GetRawText(), typeof(DateOperations), options) as DateOperations;
            }
            else
            {
                return JsonSerializer.Deserialize(doc.RootElement.GetRawText(), typeof(TagsOperations), options) as TagsOperations;
            }
        }
    }

    public override void Write(Utf8JsonWriter writer, Operations value, JsonSerializerOptions options)
    {
        if (value is TextOperations textOperations)
        {
            JsonSerializer.Serialize(writer, textOperations, typeof(TextOperations), options);
        }
        else if (value is DateOperations dateOperations)
        {
            JsonSerializer.Serialize(writer, dateOperations, typeof(DateOperations), options);
        }
        else if (value is TagsOperations tagsOperations)
        {
            JsonSerializer.Serialize(writer, tagsOperations, typeof(TagsOperations), options);
        }
    }
}