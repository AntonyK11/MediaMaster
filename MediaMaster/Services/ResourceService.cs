using Windows.Storage;

namespace MediaMaster.Services;

/// <summary>
///     Service responsible for managing resources.
/// </summary>
public static class ResourceService
{
    /// <summary>
    ///     Creates a resource file in the specified folder.
    /// </summary>
    /// <param name="folder"> The folder where the resource file will be created. </param>
    /// <param name="resourceFileName"> The name of the resource file to be created. </param>
    /// <param name="resourceFilePath"> The path of the resource file to be used as the source. </param>
    /// <param name="replaceIfExists"> A boolean value indicating whether to replace the existing resource file if found. </param>
    public static async Task CreateResource(StorageFolder folder, string resourceFileName, string resourceFilePath,
        bool replaceIfExists = false)
    {
        var fileExists = await folder.TryGetItemAsync(resourceFileName) is not null;
        if (!fileExists | replaceIfExists)
        {
            if (fileExists)
            {
                File.Delete(Path.Combine(folder.Path, resourceFileName));
            }

            StorageFile resourceFile = await LoadResourcesFileFromAppResource(resourceFilePath);
            await resourceFile.CopyAsync(folder);
        }
    }

    /// <summary>
    ///     Loads a resources file from the app package resources.
    /// </summary>
    /// <param name="filePath"> The path of the resources file within the app package. </param>
    /// <returns> A StorageFile object representing the resources file. </returns>
    private static async Task<StorageFile> LoadResourcesFileFromAppResource(string filePath)
    {
        Uri resourcesFileUri = new($"ms-appx:///{filePath}");
        return await StorageFile.GetFileFromApplicationUriAsync(resourcesFileUri);
    }
}