using MediaMaster.DataBase;
using MediaMaster.Interfaces.ViewModels;
using MediaMaster.Views.Dialog;

namespace MediaMaster.ViewModels.Flyout;

public partial class AddMediasViewModel : INavigationAware
{
    private ICollection<string>? _mediaPaths;
    private ICollection<KeyValuePair<string?, string>>? _browserTitleUrl;

    public void OnNavigatedTo(object parameter)
    {
        if (parameter is ICollection<string> mediaPaths)
        {
            _mediaPaths = mediaPaths;
            Debug.WriteLine("hi");
        }
        else if (parameter is ICollection<KeyValuePair<string?, string>> browserTitleUrl)
        {
            _browserTitleUrl = browserTitleUrl;
            Debug.WriteLine("hi2");
        }
    }

    public void OnNavigatedFrom() { }

    public async void AddMedias(AddMediasDialog dialog)
    {
        var tagIds = dialog.Tags.Select(t => t.TagId).ToHashSet();
        var notes = dialog.Notes;

        await Task.Run(async () =>
        {
            if (_mediaPaths != null)
            {
                await App.GetService<MediaService>().AddMediaAsync(_mediaPaths, tagIds, notes);
            }
            else if (_browserTitleUrl != null)
            {
                await App.GetService<MediaService>().AddMediaAsync(_browserTitleUrl, null, tagIds, notes);
            }
        });
    }
}