namespace MediaMaster.Extensions;

public static class ContentDialogExtensions
{
    private static readonly Stack<Dialog> DialogStack = new();
    private static readonly Stack<Dialog> SecondaryDialogStack = new();

    public static async Task<ContentDialogResult> ShowAndEnqueueAsync(this ContentDialog newContentDialog)
    {
        Dialog newDialog = new(newContentDialog);

        if (DialogStack.Count > 0)
        {
            Dialog first = DialogStack.First();
            DialogStack.Push(newDialog);
            first.Hide();
            await Task.Delay(210);
        }
        else
        {
            DialogStack.Push(newDialog);
        }

        ContentDialogResult? result;
        do
        {
            if (DialogStack.First() != newDialog)
            {
                await DialogStack.First().GetCompletionTask();
            }

            result = await newDialog.ShowAsync();
        } while (result == null);

        DialogStack.Pop();

        return (ContentDialogResult)result;
    }

    public static async Task<ContentDialogResult> ShowAndEnqueueSecondaryAsync(this ContentDialog newContentDialog)
    {
        Dialog newDialog = new(newContentDialog);

        if (SecondaryDialogStack.Count > 0)
        {
            Dialog first = SecondaryDialogStack.First();
            SecondaryDialogStack.Push(newDialog);
            first.Hide();
            await Task.Delay(210);
        }
        else
        {
            SecondaryDialogStack.Push(newDialog);
        }

        ContentDialogResult? result;
        do
        {
            if (SecondaryDialogStack.First() != newDialog)
            {
                await SecondaryDialogStack.First().GetCompletionTask();
            }

            result = await newDialog.ShowAsync();
        } while (result == null);

        SecondaryDialogStack.Pop();

        return (ContentDialogResult)result;
    }

    private class Dialog(ContentDialog contentDialog)
    {
        private readonly TaskCompletionSource _tcs = new();
        private bool _wasHidden;

        public async Task<ContentDialogResult?> ShowAsync()
        {
            ContentDialogResult result = await contentDialog.ShowAsync();
            if (_wasHidden)
            {
                _wasHidden = false;
                return null;
            }

            _tcs.SetResult();
            return result;
        }

        public void Hide()
        {
            _wasHidden = true;
            contentDialog.Hide();
        }

        public Task GetCompletionTask()
        {
            return _tcs.Task;
        }
    }
}