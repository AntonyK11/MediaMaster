using System.Text.Json.Nodes;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Microsoft.EntityFrameworkCore;
using Windows.Storage;
using CommunityToolkit.WinUI;
using CommunityToolkit.WinUI.Behaviors;
using MediaMaster.Services;
using Microsoft.Extensions.Logging;
using EFCore.BulkExtensions;

namespace MediaMaster.DataBase;

[Flags]
public enum MediaChangeFlags
{
    MediaAdded = 0x00001,
    MediaRemoved = 0x00002,
    MediaChanged = 0x00004,
    
    NameChanged = 0x00010,
    UriChanged = 0x00100,
    NotesChanged = 0x01000,
    TagsChanged = 0x10000,
}

public struct MediaChangeArgs(MediaChangeFlags flags, ICollection<Media> media, ICollection<Tag>? tagsAdded = null, ICollection<Tag>? tagsRemoved = null)
{
    public MediaChangeFlags Flags = flags;
    public HashSet<int> MediaIds = media.Select(m => m.MediaId).ToHashSet();
    public ICollection<Media> Medias = media;
    public ICollection<Tag>? TagsAdded = tagsAdded;
    public ICollection<Tag>? TagsRemoved = tagsRemoved;
}

public partial class MediaDbContext : DbContext
{
    public static Tag? FileTag;
    public static Tag? WebsiteTag;
    public static Tag? FavoriteTag;
    public static Tag? ArchivedTag;

    public static event TypedEventHandler<object?, MediaChangeArgs>? MediasChanged;

    //public static event TypedEventHandler<object, >? TagAdded;
    //public static event TypedEventHandler<object, >? TagRemoved;
    //public static event TypedEventHandler<object, >? TagChanged;

    public DbSet<Media> Medias { get; init; }
    public DbSet<Tag> Tags { get; init; }

    public DbSet<MediaTag> MediaTags { get; init; }
    public DbSet<TagTag> TagTags { get; init; }


    private static readonly string DbPath = Path.Combine(ApplicationData.Current.LocalFolder.Path, "MediaMaster.db");
    //private const string DbPath = "C:\\Users\\Antony\\AppData\\Local\\Packages\\MediaMaster_dqnfd4b7hk63t\\LocalState\\MediaMaster.db";

    private StorageFolder DataFolder { get; set; }

    private const string CategoriesFileName = "MediaCategories.json";

    private const string CategoriesSettingsKey = "MediaCategories";

    private readonly IPropertySet _localSetting = ApplicationData.Current.LocalSettings.Values;

    private const string AddKey = "Add";
    private const string RemoveKey = "Remove";

    public readonly Dictionary<string, ICollection<string>> Categories = [];

    protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
    {
        optionsBuilder.UseSqlite($"Data Source={DbPath}");
        optionsBuilder.EnableDetailedErrors();
        optionsBuilder.EnableSensitiveDataLogging();

        optionsBuilder.LogTo(message => Debug.WriteLine(message), LogLevel.Information);

        optionsBuilder.UseQueryTrackingBehavior(QueryTrackingBehavior.NoTracking);
    }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Media>()
            .HasMany(e => e.Tags)
            .WithMany(e => e.Medias)
            .UsingEntity<MediaTag>();

        modelBuilder.Entity<Tag>()
            .HasMany(e => e.Children)
            .WithMany(e=> e.Parents)
            .UsingEntity<TagTag>(
                t => t
                    .HasOne<Tag>()
                    .WithMany()
                    .HasForeignKey(e => e.ChildrenTagId),
                t => t
                    .HasOne<Tag>()
                    .WithMany()
                    .HasForeignKey(e => e.ParentsTagId),
                j =>
                {
                    j.HasKey(t => new { t.ChildrenTagId, t.ParentsTagId });
                });
    }

    public async Task InitializeAsync()
    {
        //await Database.MigrateAsync();
        await Database.EnsureDeletedAsync();
        await Database.EnsureCreatedAsync();
        //await Medias.LoadAsync();
        //await Tags.LoadAsync();

        //ChangeTracker.StateChanged += Timestamps.UpdateTimestamps;
        //ChangeTracker.Tracked += Timestamps.UpdateTimestamps;

        ICollection<Tag> tags = Tags.ToList();

        if (tags.FirstOrDefault(t => t.Name == "File") is not { } fileTag)
        {
            fileTag = new Tag
            {
                Name = "File",
                Shorthand = "file",
                Permissions = TagPermissions.CannotChangeParents | TagPermissions.CannotDelete
            };
            await Tags.AddAsync(fileTag);
        }
        FileTag = fileTag;

        if (tags.FirstOrDefault(t => t.Name == "Website") is not { } websiteTag)
        {
            websiteTag = new Tag
            {
                Name = "Website",
                Shorthand = "web",
                Permissions = TagPermissions.CannotChangeParents | TagPermissions.CannotDelete
            };
            await Tags.AddAsync(websiteTag);
        }
        WebsiteTag = websiteTag;

        if (tags.FirstOrDefault(t => t.Name == "Favorite") is not { } favoriteTag)
        {
            favoriteTag = new Tag
            {
                Name = "Favorite",
                Permissions = TagPermissions.CannotChangeParents | TagPermissions.CannotDelete | TagPermissions.CannotChangeColor,
                Aliases = { "Favorited", "Favorites" },
                Argb = -204544
            };
            await Tags.AddAsync(favoriteTag);
        }
        FavoriteTag = favoriteTag;

        if (tags.FirstOrDefault(t => t.Name == "Archived") is not { } archivedTag)
        {
            archivedTag = new Tag
            {
                Name = "Archived",
                Permissions = TagPermissions.CannotChangeParents | TagPermissions.CannotDelete | TagPermissions.CannotChangeColor,
                Aliases = { "Archive" },
                Argb = -3921124
            };
            await Tags.AddAsync(archivedTag);
        }
        ArchivedTag = archivedTag;

        await SaveChangesAsync();

        var defaultCategoriesFilePath = Path.Combine(AppContext.BaseDirectory, "Data", CategoriesFileName);

        await using FileStream fileStream = File.OpenRead(defaultCategoriesFilePath);
        JsonNode defaultCategories = (await JsonNode.ParseAsync(fileStream))!;

        ICollection<Tag> newTags = [];

        if (defaultCategories is JsonObject defaultCategoriesObject)
        {
            foreach ((var key, JsonNode? value) in defaultCategoriesObject)
            {
                if (value is not JsonArray array) continue;

                var category = new Tag()
                {
                    Name = key,
                    Flags = TagFlags.Extension,
                    Permissions = TagPermissions.CannotChangeParents,
                    FirstParentReferenceName = FileTag.GetReferenceName()
                };
                category.Parents.Add(FileTag);
                newTags.Add(category);

                foreach (JsonNode? extension in array)
                {
                    if (extension != null)
                    {
                        var tag = new Tag()
                        {
                            Name = extension.ToString(),
                            Flags = TagFlags.Extension,
                            Permissions = TagPermissions.CannotChangeName | TagPermissions.CannotDelete,
                            FirstParentReferenceName = category.GetReferenceName()
                        };
                        tag.Parents.Add(category);
                        newTags.Add(tag);
                    }
                }
            }
        }

        await this.BulkInsertAsync(newTags, new BulkConfig { SetOutputIdentity = true });

        var tagTags = newTags.SelectMany(tag => tag.Parents.Select(parent => new TagTag
        {
            ParentsTagId = parent.TagId,
            ChildrenTagId = tag.TagId
        })).ToList();

        await this.BulkInsertAsync(tagTags);
    }

    public static void InvokeMediaChange(object?  sender, MediaChangeFlags flags, ICollection<Media> media, ICollection<Tag>? tagsAdded = null, ICollection<Tag>? tagsRemoved = null)
    {
        var args = new MediaChangeArgs(flags, media, tagsAdded, tagsRemoved);
        App.DispatcherQueue.EnqueueAsync(() => MediasChanged?.Invoke(sender, args));

        if (flags.HasFlag(MediaChangeFlags.MediaAdded) || flags.HasFlag(MediaChangeFlags.MediaRemoved))
        {
            var notification = new Notification()
            {
                Title = $"Notification {DateTimeOffset.Now}",
                Duration = TimeSpan.FromSeconds(5)
            };

            if (flags.HasFlag(MediaChangeFlags.MediaAdded))
            {
                notification.Message = $"{media.Count} Media(s) Added";
            }
            else if (flags.HasFlag(MediaChangeFlags.MediaRemoved))
            {
                notification.Message = $"{media.Count} Media(s) Removed";
            }

            App.GetService<InAppNotificationService>().SendNotification(notification);
        }
    }

    //public async Task SetupMediaCategories()
    //{
    //    JsonNode defaultCategories = await GetDefaultCategories();
    //    JsonNode userCategories = await GetUserCategories();
    //    JsonNode categoriesToRemove = userCategories[RemoveKey]!;
    //    JsonNode categoriesToAdd = userCategories[AddKey]!;

    //    if (defaultCategories is not JsonObject categoriesObject) return;

    //    if (categoriesToRemove is JsonObject categoriesToRemoveObject)
    //    {
    //        foreach ((string key, JsonNode? value) in categoriesToRemoveObject)
    //        {
    //            JsonArray? defaultFormats = categoriesObject[key]?.AsArray();

    //            if (defaultFormats == null || value is not JsonArray array) continue;

    //            foreach (JsonNode? userFormat in array)
    //            {
    //                JsonNode? defaultFormat = defaultFormats.FirstOrDefault(node => node?.ToString() == userFormat?.ToString());
    //                defaultFormats.Remove(defaultFormat);
    //            }
    //        }
    //    }

    //    if (categoriesToAdd is JsonObject)
    //    {
    //        foreach ((string key, JsonNode? value) in categoriesToAdd.AsObject())
    //        {
    //            if (categoriesObject[key] is not JsonArray formats)
    //            {
    //                formats = [];
    //                categoriesObject[key] = formats;
    //            }

    //            if (value is not JsonArray valueArray) continue;

    //            foreach (JsonNode? format in valueArray)
    //            {
    //                formats.Add(format?.DeepClone());
    //            }
    //        }
    //    }

    //    foreach ((string key, JsonNode? extensions) in categoriesObject)
    //    {
    //        if (extensions is not JsonArray array)
    //        {
    //            categoriesObject.Remove(key);
    //            continue;
    //        }

    //        foreach (IGrouping<string?, JsonNode?> extension in array.GroupBy(node => node?.ToString()).Where(nodes => nodes.Count() > 1))
    //        {
    //            foreach (JsonNode? node in extension.Skip(1))
    //            {
    //                array.Remove(node);
    //            }
    //        }
    //    }

    //    await AddCategoriesToDatabase(categoriesObject);

    //    // Invert the keys and values of the categories object
    //    JsonObject invertedCategoriesObject = [];
    //    foreach ((string key, JsonNode? value) in categoriesObject)
    //    {
    //        if (value is not JsonArray array) continue;

    //        foreach (JsonNode? format in array)
    //        {
    //            if (format == null) continue;

    //            if (invertedCategoriesObject.ContainsKey(format.ToString()) && invertedCategoriesObject[format.ToString()] is JsonArray formatArray)
    //            {
    //                if (!formatArray.ToString().Contains(key))
    //                {
    //                    formatArray.Add(key);
    //                }
    //            }
    //            else
    //            {
    //                invertedCategoriesObject[format.ToString()] = new JsonArray { key };
    //            }
    //        }
    //    }

    //    //foreach (Category category in Categories)
    //    //{ 
    //    //    Debug.WriteLine(category.Name);
    //    //    foreach (Extension extension in category.Extensions)
    //    //    {
    //    //        Debug.WriteLine($"    - {extension.Name}");
    //    //    }
    //    //    Debug.WriteLine("");
    //    //}

    //    // IEnumerable<string> formatsToUpdate = FormatsToUpdate(invertedCategoriesObject);

    //    // await UpdateFormats(formatsToUpdate);
    //}

    //private async Task<JsonNode> GetDefaultCategories()
    //{
    //    string defaultCategoriesFilePath = Path.Combine(AppContext.BaseDirectory, DataFolder.Name, CategoriesFileName);

    //    await using FileStream fileStream = File.OpenRead(defaultCategoriesFilePath);
    //    JsonNode defaultCategories = (await JsonNode.ParseAsync(fileStream))!;

    //    return defaultCategories;
    //}

    //private async Task<JsonNode> GetUserCategories()
    //{
    //    string filePath = Path.Combine(DataFolder.Path, CategoriesFileName);

    //    bool fileExists = File.Exists(filePath);
    //    if (!fileExists)
    //    {
    //        File.Create(filePath).Close();
    //    }

    //    JsonNode userCategories;
    //    try
    //    {
    //        await using FileStream fileStream = File.OpenRead(filePath);
    //        userCategories = await JsonSerializer.DeserializeAsync<JsonNode>(fileStream) ?? new JsonObject();
    //    }
    //    catch (JsonException)
    //    {
    //        userCategories = new JsonObject();
    //    }

    //    if (!userCategories.AsObject().ContainsKey(AddKey))
    //    {
    //        userCategories[AddKey] = new JsonObject();
    //    }
    //    if (!userCategories.AsObject().ContainsKey(RemoveKey))
    //    {
    //        userCategories[RemoveKey] = new JsonObject();
    //    }

    //    await File.WriteAllTextAsync(filePath, userCategories.ToString());

    //    return userCategories;
    //}

    //private async Task AddCategoriesToDatabase(JsonObject categoriesObject)
    //{
    //    // Convert the JSON object to a dictionary of Categories
    //    //Dictionary<string, Category> mediaCategories = [];
    //    //foreach ((string key, JsonNode? value) in categoriesObject)
    //    //{
    //    //    if (value is not JsonArray) continue;

    //    //    Category newCategory = this.CreateProxy<Category>();

    //    //    newCategory.Name = key;
    //    //    foreach (JsonNode? format in value.AsArray())
    //    //    {
    //    //        if (format == null) continue;
    //    //        string extensionString = format.ToString();

    //    //        Extension? ext = await Extensions.FirstOrDefaultAsync(f => f.Name == extensionString);

    //    //        if (ext == null)
    //    //        {
    //    //            ext = this.CreateProxy<Extension>(e => e.Name = extensionString);
    //    //            await AddAsync(ext);
    //    //        }

    //    //        newCategory.Extensions.Add(ext);

    //    //        // if (!Formats.TryGetValue(formatString, out IList<Category>? categories))
    //    //        // {
    //    //        //     categories = [];
    //    //        //     Formats[formatString] = categories;
    //    //        // }

    //    //        // categories.Add(newCategory);
    //    //    }
    //    //    mediaCategories[key] = newCategory;
    //    //}

    //    // Get the Categories from the database
    //    Dictionary<string, Category> categoriesInDb = Categories.Local.ToDictionary(t => t.Name);

    //    // Prepare the lists for bulk operations
    //    //List<Category> categoriesToDelete = [];
    //    //List<Category> categoriesToUpdate = [];
    //    //List<Category> categoriesToAdd = [];

    //    // Prepare the data for bulk operations
    //    foreach (string category in categoriesInDb.Keys)
    //    {
    //        if (!categoriesObject.ContainsKey(category))
    //        {
    //            Remove(categoriesInDb[category]);
    //            //categoriesToDelete.Add(categoriesInDb[category]);
    //        }
    //        else
    //        {
    //            if (categoriesObject[category] is not JsonArray extensionsArray) continue;

    //            IEnumerable<string?> extensionsArrayString = extensionsArray.Select(e => e?.ToString()).ToArray();

    //            foreach (Extension extension in categoriesInDb[category].Extensions.ToArray())
    //            {
    //                if (!extensionsArrayString.Contains(extension.Name))
    //                {
    //                    categoriesInDb[category].Extensions.Remove(extension);
    //                }
    //            }

    //            foreach (string? extension in extensionsArrayString)
    //            {
    //                if (extension is null) continue;

    //                if (categoriesInDb[category].Extensions.Select(e => e.Name).Contains(extension)) continue;

    //                Extension? ext = Extensions.Local.FirstOrDefault(f => f.Name == extension);

    //                if (ext == null)
    //                {
    //                    ext = this.CreateProxy<Extension>(e => e.Name = extension);
    //                    await AddAsync(ext);
    //                }

    //                categoriesInDb[category].Extensions.Add(ext);
    //            }

    //            //categoriesToUpdate.Add(value);
    //        }
    //    }

    //    foreach ((string key, JsonNode? value) in categoriesObject)
    //    {
    //        if (value is not JsonArray array) continue;

    //        if (categoriesInDb.ContainsKey(key)) continue;

    //        Category newCategory = this.CreateProxy<Category>();

    //        newCategory.Name = key;
    //        foreach (JsonNode? format in array)
    //        {
    //            if (format == null) continue;
    //            string extensionString = format.ToString();

    //            Extension? ext = Extensions.Local.FirstOrDefault(f => f.Name == extensionString);

    //            if (ext == null)
    //            {
    //                ext = this.CreateProxy<Extension>(e => e.Name = extensionString);
    //                await AddAsync(ext);
    //            }

    //            newCategory.Extensions.Add(ext);
    //        }

    //        await AddAsync(newCategory);
    //    }

    //    //categoriesToAdd.AddRange(mediaCategories.Keys.Except(categoriesInDb.Keys).Select(category => mediaCategories[category]));

    //    //await PerformBulkOperations(categoriesToDelete, categoriesToUpdate, categoriesToAdd);

    //    await SaveChangesAsync();
    //}
}