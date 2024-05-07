using Microsoft.EntityFrameworkCore.ChangeTracking;
using Microsoft.EntityFrameworkCore;

namespace MediaMaster.DataBase.Models;

public static class Timestamps
{
    public static void UpdateTimestamps(object? sender, EntityEntryEventArgs e)
    {
        if (e.Entry is { Entity: IHasTimestamps entityWithTimestamps, State: EntityState.Modified })
        {
            entityWithTimestamps.Modified = DateTime.UtcNow;
        }
    }
}

public interface IHasTimestamps
{
    DateTime Modified { set; }
}