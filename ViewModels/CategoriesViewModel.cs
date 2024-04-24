using CommunityToolkit.Mvvm.ComponentModel;
using MediaMaster.DataBase;

namespace MediaMaster.ViewModels;

public partial class CategoriesViewModel : ObservableObject
{
    public DataBaseService dataBaseService = App.GetService<DataBaseService>();
}