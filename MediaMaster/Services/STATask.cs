using static MediaMaster.WIn32.WindowsApiService;

namespace MediaMaster.Services;

/// https://github.com/files-community/Files/blob/main/src/Files.App/Helpers/Win32/Win32Helper.Storage.cs
public static class STATask
{
    public static Task StartSTATask(Func<Task> func)
    {
        var tcs = new TaskCompletionSource();
        Thread thread = new(async () =>
        {
            OleInitialize();

            try
            {
                await func();
                tcs.SetResult();
            }
            catch
            {
                tcs.SetResult();
            }
            finally
            {
                OleUninitialize();
            }
        })
        {
            IsBackground = true,
            Priority = ThreadPriority.Normal
        };

        thread.SetApartmentState(ApartmentState.STA);
        thread.Start();
        return tcs.Task;
    }

    public static Task StartSTATask(Action action)
    {
        var tcs = new TaskCompletionSource();
        Thread thread = new(() =>
        {
            OleInitialize();

            try
            {
                action();
                tcs.SetResult();
            }
            catch
            {
                tcs.SetResult();
            }
            finally
            {
                OleUninitialize();
            }
        })
        {
            IsBackground = true,
            Priority = ThreadPriority.Normal
        };

        thread.SetApartmentState(ApartmentState.STA);
        thread.Start();
        return tcs.Task;
    }

    public static Task<T?> StartSTATask<T>(Func<T> func)
    {
        var tcs = new TaskCompletionSource<T?>();

        Thread thread = new(() =>
        {
            OleInitialize();

            try
            {
                tcs.SetResult(func());
            }
            catch
            {
                tcs.SetResult(default);
            }
            finally
            {
                OleUninitialize();
            }
        })
        {
            IsBackground = true,
            Priority = ThreadPriority.Normal
        };

        thread.SetApartmentState(ApartmentState.STA);
        thread.Start();
        return tcs.Task;
    }

    public static Task<T?> StartSTATask<T>(Func<Task<T>> func)
    {
        var tcs = new TaskCompletionSource<T?>();

        Thread thread = new(async () =>
        {
            OleInitialize();

            try
            {
                tcs.SetResult(await func());
            }
            catch
            {
                tcs.SetResult(default);
            }
            finally
            {
                OleUninitialize();
            }
        })
        {
            IsBackground = true,
            Priority = ThreadPriority.Normal
        };

        thread.SetApartmentState(ApartmentState.STA);
        thread.Start();
        return tcs.Task;
    }
}

