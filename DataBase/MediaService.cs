using Microsoft.EntityFrameworkCore;
namespace MediaMaster.DataBase;
using Microsoft.UI.Dispatching;
using System.Collections.Generic;

public class MediaService
{
    private readonly DataBaseService _dataBase;

    public MediaService(DataBaseService dataBase)
    {
        _dataBase = dataBase;
    }

    public async Task AddMediaAsync(string path)
    {
        //DateTime time = DateTime.Now;
        //DispatcherQueue? dispatcherQueueCurrent = DispatcherQueue.GetForCurrentThread();
        //if (_dispatcherQueue != dispatcherQueueCurrent)
        //{
        //    await _dispatcherQueue.EnqueueAsync(() => AddMediaAsync(path));
        //    return;
        //}

        IEnumerable<string> mediaPaths = GetFiles(path);
        IList<Media> medias = mediaPaths.Select(GetMedia).ToList();

        //await _dispatcherQueue.EnqueueAsync(() => _dataBase.Medias.AddRangeAsync(medias));
        await _dataBase.Medias.AddRangeAsync(medias);
        //await _dataBase.BulkSaveChangesAsync();
        //await _dataBase.BulkInsertAsync(medias);
        await _dataBase.SaveChangesAsync();
    }

    private static IEnumerable<string> GetFiles(string path)
    {
        if (Directory.Exists(path))
        {
            EnumerationOptions opt = new()
            {
                RecurseSubdirectories = true,
                IgnoreInaccessible = true
            };
            return Directory.EnumerateFileSystemEntries(path, "*.*", opt);
        }

        if (File.Exists(path))
        {
            return [path];
        }

        // Path does not exist
        return [];
    }

    private Media GetMedia(string path)
    {
        Media media = _dataBase.CreateProxy<Media>(async m =>
        {
            m.Name = Path.GetFileNameWithoutExtension(path);
            m.FilePath = path;
            string extension = Path.GetExtension(path);
            //m.Extension = await _dataBase.Extensions.FirstOrDefaultAsync(f => f.Name == extension) ?? _dataBase.CreateProxy<Extension>(e => e.Name = extension);
        });
        return media;
    }
}
