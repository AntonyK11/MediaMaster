namespace MediaMaster.Extensions;

public static class ContentDialogExtensions
{
    private static readonly Stack<Dialog> DialogStack = new();

    public static async Task<ContentDialogResult> ShowAndEnqueueAsync(this ContentDialog newContentDialog)
    {
        Dialog newDialog = new(newContentDialog);

        if (DialogStack.Count > 0)
        {
            var first = DialogStack.First();
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

    private class Dialog(ContentDialog contentDialog)
    {
        private bool _wasHidden;
        private readonly TaskCompletionSource _tcs = new();

        public async Task<ContentDialogResult?> ShowAsync()
        {
            var result = await contentDialog.ShowAsync();
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