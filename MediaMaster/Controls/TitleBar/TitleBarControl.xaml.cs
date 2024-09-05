using DependencyPropertyGenerator;
using MediaMaster.Interfaces.Services;
using Microsoft.UI.Xaml.Media.Imaging;

namespace MediaMaster.Controls;

[DependencyProperty("Title", typeof(string), DefaultValue = "")]
[DependencyProperty("Subtitle", typeof(string), DefaultValue = "")]
[DependencyProperty("Icon", typeof(BitmapImage))]
public partial class TitleBarControl
{
    public TitleBarControl()
    {
        InitializeComponent();

        ViewModel = new TitleBarViewModel(this);

        Loaded += (_, _) => ViewModel.SetDragRegionTitleBar();
        SizeChanged += (_, _) => ViewModel.SetDragRegionTitleBar();
        var themeSelectorService = App.GetService<IThemeSelectorService>();

        themeSelectorService.ThemeChanged += (_, theme) => ViewModel.UpdateTitleBar(theme);
        ViewModel.UpdateTitleBar(themeSelectorService.ActualTheme);

        if (App.MainWindow != null)
        {
            App.MainWindow.Title = Title;
            App.MainWindow.SetTitleBar(this);

            App.MainWindow.Activated += MainWindow_Activated;
        }

        RegisterPropertyChangedCallback(TitleProperty, Callback);

        Loaded += (_, _) =>
        {
            AppIconElement.PointerPressed += ViewModel.AppIcon_LeftClick;
            AppIconElement.RightTapped += ViewModel.AppIcon_RightClick;
            AppIconElement.DoubleTapped += (_, _) => App.MainWindow?.Close();
        };
    }

    public TitleBarViewModel ViewModel { get; }

    partial void OnIconChanged(BitmapImage? newValue)
    {
        if (newValue != null && App.MainWindow != null)
        {
            App.MainWindow.AppWindow.SetIcon(newValue.UriSource.AbsoluteUri);
        }
    }

    private void Callback(DependencyObject sender, DependencyProperty dp)
    {
        if (App.MainWindow != null)
        {
            App.MainWindow.Title = Title;
        }
    }

    private void MainWindow_Activated(object sender, WindowActivatedEventArgs args)
    {
        VisualStateManager.GoToState(this,
            args.WindowActivationState != WindowActivationState.Deactivated
                ? "WindowActivated"
                : "WindowDeactivated", false);
    }


    public void SetLeftMargin(double leftMargin)
    {
        ViewModel.SetLeftMargin(leftMargin);
        ViewModel.SetDragRegionTitleBar();
    }

    public void SetRightMargin(double rightMargin)
    {
        ViewModel.SetRightMargin(rightMargin);
        ViewModel.SetDragRegionTitleBar();
    }

    public void SetTopMargin(double topMargin)
    {
        ViewModel.SetTopMargin(topMargin);
        ViewModel.SetDragRegionTitleBar();
    }

    public void SetBottomMargin(double bottomMargin)
    {
        ViewModel.SetBottomMargin(bottomMargin);
        ViewModel.SetDragRegionTitleBar();
    }
}