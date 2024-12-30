using CommunityToolkit.WinUI;
using MediaMaster.DataBase;
using WinUI3Localizer;
using WinUICommunity;

namespace MediaMaster.Services;

public static class Transaction
{
    public static async Task<bool> Try(MediaDbContext database, Func<Task> func)
    {
        await App.GetService<TasksService>().AddGlobalTak();

        await using var transaction = await database.Database.BeginTransactionAsync();

        try
        {
            await func();

            await transaction.CommitAsync();
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
            return false;
        }
        finally
        {
            await App.GetService<TasksService>().RemoveGlobalTak();
        }

        return true;
    }
}
