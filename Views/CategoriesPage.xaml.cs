using MediaMaster.ViewModels;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml;
using MediaMaster.DataBase;

namespace MediaMaster.Views;

public sealed partial class CategoriesPage
{
    public CategoriesViewModel ViewModel { get; }

    public CategoriesPage()
    {
        ViewModel = App.GetService<CategoriesViewModel>();

        InitializeComponent();
    }
}

//class CategoriesTemplateSelector : DataTemplateSelector
//{
//    public DataTemplate CategoryTemplate { get; set; }
//    public DataTemplate ExtensionTemplate { get; set; }

//    protected override DataTemplate SelectTemplateCore(object item)
//    {
//        return item is Category ? CategoryTemplate : ExtensionTemplate;
//    }
//}
