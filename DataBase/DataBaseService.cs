using Windows.Storage;
using Microsoft.EntityFrameworkCore;
using System.Text.Json;
using Windows.Foundation.Collections;
using System.Text.Json.Nodes;
using static MediaMaster.DataBase.DbEntities;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Hosting;

namespace MediaMaster.DataBase;

public class DataBaseService : DbContext
{
    public DbSet<Media> Medias { get; init; }
    public DbSet<Tag> Tags { get; init; }
    //public DbSet<Category> Categories { get; init; }
    //public DbSet<Extension> Extensions { get; init; }

#if !RELEASE
    //private const string DbPath = "C:\\Users\\Antony\\AppData\\Local\\Packages\\MediaMaster_dqnfd4b7hk63t\\LocalState\\MediaMaster.db";
    private static readonly string DbPath = Path.Combine(ApplicationData.Current.LocalFolder.Path, "MediaMaster.db");
    //private string DbPath = @"Server=(localdb)\v11.0;Integrated Security = true";
#else
    private static readonly string DbPath = Path.Combine(ApplicationData.Current.LocalFolder.Path, "MediaMaster.db");
#endif


    private StorageFolder DataFolder { get; set; }

    private const string CategoriesFileName = "MediaCategories.json";

    private const string CategoriesSettingsKey = "MediaCategories";

    private readonly IPropertySet _localSetting = ApplicationData.Current.LocalSettings.Values;

    private const string AddKey = "Add";
    private const string RemoveKey = "Remove";

    private static readonly ILoggerFactory MyLoggerFactory = LoggerFactory.Create(builder => { builder.AddDebug(); });

    protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
    {
        optionsBuilder.UseSqlite($"Data Source={DbPath}");
        //optionsBuilder.UseChangeTrackingProxies();
        optionsBuilder.EnableDetailedErrors();
        optionsBuilder.UseLazyLoadingProxies();

        optionsBuilder.UseLoggerFactory(MyLoggerFactory);
    }

    public async Task InitializeAsync()
    {
        DataFolder = await ApplicationData.Current.LocalFolder.CreateFolderAsync("Data", CreationCollisionOption.OpenIfExists);

        //await Database.MigrateAsync();
        await Database.EnsureDeletedAsync();
        await Database.EnsureCreatedAsync();
        //await Extensions.LoadAsync();
        //await Categories.LoadAsync();
        await Medias.LoadAsync();

        //await SetupMediaCategories();

        ChangeTracker.StateChanged += UpdateTimestamps;
        ChangeTracker.Tracked += UpdateTimestamps;

        for (int i = 0; i < 10; i++)
        {
            Tag media = this.CreateProxy<Tag>(t =>
            {
                t.Name = "Media";
                foreach (Tag tag in Tags)
                {
                    t.Children.Add(tag);
                }
            });
            Tags.Add(media);
            await SaveChangesAsync();
        }
    }

    //public override Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
    //{
    //    return base.SaveChangesAsync(cancellationToken);
    //    //DispatcherQueue? dispatcherQueueCurrent = DispatcherQueue.GetForCurrentThread();
    //    //if (_dispatcherQueue == dispatcherQueueCurrent)
    //    //{
    //    //    return base.SaveChangesAsync(cancellationToken);
    //    //}
    //    //return _dispatcherQueue.EnqueueAsync(() => base.SaveChangesAsync(cancellationToken));
    //}

    //////////public async Task SetupMediaCategories()
    //////////{
    //////////    JsonNode defaultCategories = await GetDefaultCategories();
    //////////    JsonNode userCategories = await GetUserCategories();
    //////////    JsonNode categoriesToRemove = userCategories[RemoveKey]!;
    //////////    JsonNode categoriesToAdd = userCategories[AddKey]!;

    //////////    if (defaultCategories is not JsonObject categoriesObject) return;

    //////////    if (categoriesToRemove is JsonObject categoriesToRemoveObject)
    //////////    {
    //////////        foreach ((string key, JsonNode? value) in categoriesToRemoveObject)
    //////////        {
    //////////            JsonArray? defaultFormats = categoriesObject[key]?.AsArray();

    //////////            if (defaultFormats == null || value is not JsonArray array) continue;

    //////////            foreach (JsonNode? userFormat in array)
    //////////            {
    //////////                JsonNode? defaultFormat = defaultFormats.FirstOrDefault(node => node?.ToString() == userFormat?.ToString());
    //////////                defaultFormats.Remove(defaultFormat);
    //////////            }
    //////////        }
    //////////    }

    //////////    if (categoriesToAdd is JsonObject)
    //////////    {
    //////////        foreach ((string key, JsonNode? value) in categoriesToAdd.AsObject())
    //////////        {
    //////////            if (categoriesObject[key] is not JsonArray formats)
    //////////            {
    //////////                formats = [];
    //////////                categoriesObject[key] = formats;
    //////////            }

    //////////            if (value is not JsonArray valueArray) continue;

    //////////            foreach (JsonNode? format in valueArray)
    //////////            {
    //////////                formats.Add(format?.DeepClone());
    //////////            }
    //////////        }
    //////////    }

    //////////    foreach ((string key, JsonNode? extensions) in categoriesObject)
    //////////    {
    //////////        if (extensions is not JsonArray array)
    //////////        {
    //////////            categoriesObject.Remove(key);
    //////////            continue;
    //////////        }

    //////////        foreach (IGrouping<string?, JsonNode?> extension in array.GroupBy(node => node?.ToString()).Where(nodes => nodes.Count() > 1))
    //////////        {
    //////////            foreach (JsonNode? node in extension.Skip(1))
    //////////            {
    //////////                array.Remove(node);
    //////////            }
    //////////        }
    //////////    }

    //////////    await AddCategoriesToDatabase(categoriesObject);

    //////////    // Invert the keys and values of the categories object
    //////////    JsonObject invertedCategoriesObject = [];
    //////////    foreach ((string key, JsonNode? value) in categoriesObject)
    //////////    {
    //////////        if (value is not JsonArray array) continue;

    //////////        foreach (JsonNode? format in array)
    //////////        {
    //////////            if (format == null) continue;

    //////////            if (invertedCategoriesObject.ContainsKey(format.ToString()) && invertedCategoriesObject[format.ToString()] is JsonArray formatArray)
    //////////            {
    //////////                if (!formatArray.ToString().Contains(key))
    //////////                {
    //////////                    formatArray.Add(key);
    //////////                }
    //////////            }
    //////////            else
    //////////            {
    //////////                invertedCategoriesObject[format.ToString()] = new JsonArray { key };
    //////////            }
    //////////        }
    //////////    }

    //////////    //foreach (Category category in Categories)
    //////////    //{ 
    //////////    //    Debug.WriteLine(category.Name);
    //////////    //    foreach (Extension extension in category.Extensions)
    //////////    //    {
    //////////    //        Debug.WriteLine($"    - {extension.Name}");
    //////////    //    }
    //////////    //    Debug.WriteLine("");
    //////////    //}

    //////////    // IEnumerable<string> formatsToUpdate = FormatsToUpdate(invertedCategoriesObject);

    //////////    // await UpdateFormats(formatsToUpdate);
    //////////}

    //////////private async Task<JsonNode> GetDefaultCategories()
    //////////{
    //////////    string defaultCategoriesFilePath = Path.Combine(AppContext.BaseDirectory, DataFolder.Name, CategoriesFileName);

    //////////    await using FileStream fileStream = File.OpenRead(defaultCategoriesFilePath);
    //////////    JsonNode defaultCategories = (await JsonNode.ParseAsync(fileStream))!;

    //////////    return defaultCategories;
    //////////}

    //////////private async Task<JsonNode> GetUserCategories()
    //////////{
    //////////    string filePath = Path.Combine(DataFolder.Path, CategoriesFileName);

    //////////    bool fileExists = File.Exists(filePath);
    //////////    if (!fileExists)
    //////////    {
    //////////        File.Create(filePath).Close();
    //////////    }

    //////////    JsonNode userCategories;
    //////////    try
    //////////    {
    //////////        await using FileStream fileStream = File.OpenRead(filePath);
    //////////        userCategories = await JsonSerializer.DeserializeAsync<JsonNode>(fileStream) ?? new JsonObject();
    //////////    }
    //////////    catch (JsonException)
    //////////    {
    //////////        userCategories = new JsonObject();
    //////////    }

    //////////    if (!userCategories.AsObject().ContainsKey(AddKey))
    //////////    {
    //////////        userCategories[AddKey] = new JsonObject();
    //////////    }
    //////////    if (!userCategories.AsObject().ContainsKey(RemoveKey))
    //////////    {
    //////////        userCategories[RemoveKey] = new JsonObject();
    //////////    }

    //////////    await File.WriteAllTextAsync(filePath, userCategories.ToString());

    //////////    return userCategories;
    //////////}

    //////////private async Task AddCategoriesToDatabase(JsonObject categoriesObject)
    //////////{
    //////////    // Convert the JSON object to a dictionary of Categories
    //////////    //Dictionary<string, Category> mediaCategories = [];
    //////////    //foreach ((string key, JsonNode? value) in categoriesObject)
    //////////    //{
    //////////    //    if (value is not JsonArray) continue;

    //////////    //    Category newCategory = this.CreateProxy<Category>();

    //////////    //    newCategory.Name = key;
    //////////    //    foreach (JsonNode? format in value.AsArray())
    //////////    //    {
    //////////    //        if (format == null) continue;
    //////////    //        string extensionString = format.ToString();

    //////////    //        Extension? ext = await Extensions.FirstOrDefaultAsync(f => f.Name == extensionString);

    //////////    //        if (ext == null)
    //////////    //        {
    //////////    //            ext = this.CreateProxy<Extension>(e => e.Name = extensionString);
    //////////    //            await AddAsync(ext);
    //////////    //        }

    //////////    //        newCategory.Extensions.Add(ext);

    //////////    //        // if (!Formats.TryGetValue(formatString, out IList<Category>? categories))
    //////////    //        // {
    //////////    //        //     categories = [];
    //////////    //        //     Formats[formatString] = categories;
    //////////    //        // }

    //////////    //        // categories.Add(newCategory);
    //////////    //    }
    //////////    //    mediaCategories[key] = newCategory;
    //////////    //}

    //////////    // Get the Categories from the database
    //////////    Dictionary<string, Category> categoriesInDb = Categories.Local.ToDictionary(t => t.Name);

    //////////    // Prepare the lists for bulk operations
    //////////    //List<Category> categoriesToDelete = [];
    //////////    //List<Category> categoriesToUpdate = [];
    //////////    //List<Category> categoriesToAdd = [];

    //////////    // Prepare the data for bulk operations
    //////////    foreach (string category in categoriesInDb.Keys)
    //////////    {
    //////////        if (!categoriesObject.ContainsKey(category))
    //////////        {
    //////////            Remove(categoriesInDb[category]);
    //////////            //categoriesToDelete.Add(categoriesInDb[category]);
    //////////        }
    //////////        else
    //////////        {
    //////////            if (categoriesObject[category] is not JsonArray extensionsArray) continue;

    //////////            IEnumerable<string?> extensionsArrayString = extensionsArray.Select(e => e?.ToString()).ToArray();

    //////////            foreach (Extension extension in categoriesInDb[category].Extensions.ToArray())
    //////////            {
    //////////                if (!extensionsArrayString.Contains(extension.Name))
    //////////                {
    //////////                    categoriesInDb[category].Extensions.Remove(extension);
    //////////                }
    //////////            }

    //////////            foreach (string? extension in extensionsArrayString)
    //////////            {
    //////////                if (extension is null) continue;

    //////////                if (categoriesInDb[category].Extensions.Select(e => e.Name).Contains(extension)) continue;

    //////////                Extension? ext = Extensions.Local.FirstOrDefault(f => f.Name == extension);

    //////////                if (ext == null)
    //////////                {
    //////////                    ext = this.CreateProxy<Extension>(e => e.Name = extension);
    //////////                    await AddAsync(ext);
    //////////                }

    //////////                categoriesInDb[category].Extensions.Add(ext);
    //////////            }

    //////////            //categoriesToUpdate.Add(value);
    //////////        }
    //////////    }

    //////////    foreach ((string key, JsonNode? value) in categoriesObject)
    //////////    {
    //////////        if (value is not JsonArray array) continue;

    //////////        if (categoriesInDb.ContainsKey(key)) continue;

    //////////        Category newCategory = this.CreateProxy<Category>();

    //////////        newCategory.Name = key;
    //////////        foreach (JsonNode? format in array)
    //////////        {
    //////////            if (format == null) continue;
    //////////            string extensionString = format.ToString();

    //////////            Extension? ext = Extensions.Local.FirstOrDefault(f => f.Name == extensionString);

    //////////            if (ext == null)
    //////////            {
    //////////                ext = this.CreateProxy<Extension>(e => e.Name = extensionString);
    //////////                await AddAsync(ext);
    //////////            }

    //////////            newCategory.Extensions.Add(ext);
    //////////        }

    //////////        await AddAsync(newCategory);
    //////////    }

    //////////    //categoriesToAdd.AddRange(mediaCategories.Keys.Except(categoriesInDb.Keys).Select(category => mediaCategories[category]));

    //////////    //await PerformBulkOperations(categoriesToDelete, categoriesToUpdate, categoriesToAdd);

    //////////    await SaveChangesAsync();
    //////////}




    //private async Task PerformBulkOperations(IEnumerable<Category> categoriesToDelete, IEnumerable<Category> categoriesToUpdate, IEnumerable<Category> categoriesToAdd)
    //{
    //    await this.BulkDeleteAsync(categoriesToDelete);
    //    await this.BulkUpdateAsync(categoriesToUpdate);
    //    await this.BulkInsertAsync(categoriesToAdd);
    //}

    // private IEnumerable<string> FormatsToUpdate(JsonObject mediaCategories)
    // {
    //     IList<string> formatsToUpdate = [];
    //     
    //     JsonNode previousCategories;
    //     if (_localSetting[CategoriesSettingsKey] is not string previousCategoriesInSettings)
    //     {
    //         previousCategories = new JsonObject();
    //     }
    //     else
    //     {
    //         previousCategories = JsonNode.Parse(previousCategoriesInSettings) ?? new JsonObject();
    //     }
    //
    //     _localSetting[CategoriesSettingsKey] = previousCategories.ToString();
    //     
    //     if (previousCategories is not JsonObject) return formatsToUpdate;
    //
    //     foreach ((string key, JsonNode? value) in previousCategories.AsObject())
    //     {
    //         if (value is not JsonArray) continue;
    //         
    //         if (mediaCategories.ContainsKey(key) && mediaCategories[key] is JsonArray) continue;
    //         
    //         formatsToUpdate.Add(key);
    //     }
    //
    //     foreach ((string key, JsonNode? value) in mediaCategories)
    //     {
    //         if (value is not JsonArray) continue;
    //
    //         if (previousCategories.AsObject().ContainsKey(key) && previousCategories[key] is JsonArray && previousCategories[key]?.AsArray().ToString() == value.AsArray().ToString()) continue;
    //
    //         formatsToUpdate.Add(key);
    //     }
    //
    //     _localSetting[CategoriesSettingsKey] = mediaCategories.ToString();
    //     
    //     return formatsToUpdate;
    // }

    // private async Task UpdateFormats(IEnumerable<string> formatsToUpdate)
    // {
    //     foreach (string format in formatsToUpdate)
    //     {
    //         if (Formats.TryGetValue(format, out IList<Category>? categories))
    //         {
    //             await Medias.Where(m => m.Extension == format).ForEachAsync(m => m.Categories = categories);
    //         }
    //         else
    //         {
    //             await Medias.Where(m => m.Extension == format).ForEachAsync(m => m.Categories = null);
    //         }
    //     }
    // }
}