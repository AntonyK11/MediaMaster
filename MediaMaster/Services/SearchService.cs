using MediaMaster.Controls;
using MediaMaster.DataBase;
using Microsoft.EntityFrameworkCore;
using System.Linq.Expressions;

namespace MediaMaster.Services;

public static class SearchService
{
    public static async Task<(List<CompactMedia>, int)> GetMedias(KeyValuePair<bool, Expression<Func<Media, object>>>? sortFunction, bool sortAscending, Expression<Func<Media, bool>>? simpleFilterFunction, ICollection<Expression<Func<Media, bool>>> advancedFilterFunctions, int? skip = null, int? take = null)
    {
        await using (var database = new MediaDbContext())
        {
            var mediaQuery = GetQuery(database, sortFunction, sortAscending, simpleFilterFunction, advancedFilterFunctions);

            var mediasFound = await mediaQuery.CountAsync().ConfigureAwait(false);

            if (skip != null)
            {
                mediaQuery = mediaQuery.Skip((int)skip);
            }
            if (take != null)
            {
                mediaQuery = mediaQuery.Take((int)take);
            }

            List<CompactMedia> medias = await mediaQuery
                .Select(m => new CompactMedia { MediaId = m.MediaId, Name = m.Name, Uri = m.Uri, IsFavorite = m.IsFavorite, IsArchived = m.IsArchived})
                .ToListAsync()
                .ConfigureAwait(false);

            return (medias, mediasFound);
        }
    }

    public static IQueryable<Media> GetQuery(MediaDbContext database, KeyValuePair<bool, Expression<Func<Media, object>>>? sortFunction, bool sortAscending, Expression<Func<Media, bool>>? simpleFilterFunction, ICollection<Expression<Func<Media, bool>>> advancedFilterFunctions)
    {
        IQueryable<Media> mediaQuery = database.Medias;
        if (sortFunction != null)
        {
            mediaQuery = sortAscending
                ? SortMedias(mediaQuery, (KeyValuePair<bool, Expression<Func<Media, object>>>)sortFunction, sortAscending).ThenBy(m => m.Name)
                : SortMedias(mediaQuery, (KeyValuePair<bool, Expression<Func<Media, object>>>)sortFunction, sortAscending).ThenByDescending(m => m.Name);
        }
        else
        {
            mediaQuery = sortAscending
                ? mediaQuery.OrderBy(m => m.Name)
                : mediaQuery.OrderByDescending(m => m.Name);
        }

        if (simpleFilterFunction != null)
        {
            mediaQuery = mediaQuery.Where(simpleFilterFunction);
        }
        mediaQuery = AdvancedFilterMedias(mediaQuery, advancedFilterFunctions);

        return mediaQuery;
        
    }

    private static IOrderedQueryable<Media> SortMedias(IQueryable<Media> medias, KeyValuePair<bool, Expression<Func<Media, object>>> sortFunction, bool sortAscending)
    {
        return sortAscending ^ sortFunction.Key
            ? medias.OrderByDescending(sortFunction.Value)
            : medias.OrderBy(sortFunction.Value);
    }

    private static IQueryable<Media> AdvancedFilterMedias(IQueryable<Media> medias, ICollection<Expression<Func<Media, bool>>>  advancedFilterFunctions)
    {
        return advancedFilterFunctions.Aggregate(medias, (current, filter) => current.Where(filter));
    }
}

