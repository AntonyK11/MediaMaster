using MediaMaster.Controls.VIewModels;
using MediaMaster.Interfaces.Services;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
namespace MediaMaster.Controls;

public sealed partial class TitleBarControl
{
    public TitleBarViewModel ViewModel { get; }

    public static readonly DependencyProperty TextProperty
        = DependencyProperty.Register(
            nameof(Text),
            typeof(string),
            typeof(TitleBarControl),
            new PropertyMetadata(string.Empty));

    public string Text
    {
        get => (string)GetValue(TextProperty);
        set
        {
            SetValue(TextProperty, value);

            System.Diagnostics.Debug.WriteLine(value);
        }
    }

    public static readonly DependencyProperty IconProperty
    = DependencyProperty.Register(
        nameof(Icon),
        typeof(IconElement),
        typeof(TitleBarControl),
        new PropertyMetadata(null));

    public IconElement? Icon
    {
        get => (IconElement)GetValue(IconProperty);
        set 
        {
            SetValue(IconProperty, value);

            if (value is null) return;
            

            value.PointerPressed += (_, args) => TitleBarViewModel.AppIcon_LeftClick(args, this);
            value.RightTapped += (_, _) => TitleBarViewModel.AppIcon_RightClick();
            value.DoubleTapped += (_, _) => TitleBarViewModel.AppIcon_DoubleClick();
        }
    }

    public SearchBox SearchBox => TitleBarSearchBox;


    public TitleBarControl()
    {
        InitializeComponent();

        ViewModel = new TitleBarViewModel();

        Loaded += (_, _) => TitleBarViewModel.SetDragRegionTitleBar(this);
        SizeChanged += (_, _) => TitleBarViewModel.SetDragRegionTitleBar(this);
        IThemeSelectorService themeSelectorService = App.GetService<IThemeSelectorService>();

        themeSelectorService.ThemeChanged += (_, theme) => TitleBarViewModel.UpdateTitleBar(theme);
        TitleBarViewModel.UpdateTitleBar(themeSelectorService.ActualTheme);
        //SuggestionsPopup.XamlRoot = XamlRoot;

        //Button_1.Click += (_, _) =>
        //{
        //    SuggestionsPopup.IsOpen = true;
        //};
    }


    public void SetLeftMargin(double leftMargin)
    {
        ViewModel.SetLeftMargin(leftMargin);
        TitleBarViewModel.SetDragRegionTitleBar(this);
    }

    public void SetRightMargin(double rightMargin)
    {
        ViewModel.SetRightMargin(rightMargin);
        TitleBarViewModel.SetDragRegionTitleBar(this);
    }

    public void SetTopMargin(double topMargin)
    {
        ViewModel.SetTopMargin(topMargin);
        TitleBarViewModel.SetDragRegionTitleBar(this);
    }

    public void SetBottomMargin(double bottomMargin)
    {
        ViewModel.SetBottomMargin(bottomMargin);
        TitleBarViewModel.SetDragRegionTitleBar(this);
    }
}
