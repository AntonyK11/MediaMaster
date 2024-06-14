using MediaMaster.Interfaces.Services;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;

namespace MediaMaster.Controls;

public sealed partial class TitleBarControl
{
    public TitleBarViewModel ViewModel { get; }

    public static readonly DependencyProperty TitleProperty
        = DependencyProperty.Register(
            nameof(Title),
            typeof(string),
            typeof(TitleBarControl),
            new PropertyMetadata(string.Empty));

    public string Title
    {
        get => (string)GetValue(TitleProperty);
        set => SetValue(TitleProperty, value);
    }

    public static readonly DependencyProperty SubtitleProperty
        = DependencyProperty.Register(
            nameof(Subtitle),
            typeof(string),
            typeof(TitleBarControl),
            new PropertyMetadata(string.Empty));

    public string Subtitle
    {
        get => (string)GetValue(SubtitleProperty);
        set => SetValue(SubtitleProperty, value);
    }

    public static readonly DependencyProperty IconProperty
    = DependencyProperty.Register(
        nameof(Icon),
        typeof(IconSource),
        typeof(TitleBarControl),
        new PropertyMetadata(null));

    public IconSource? Icon
    {
        get => (IconSource)GetValue(IconProperty);
        set 
        {
            SetValue(IconProperty, value);

            if (value != null)
            {
                var icon = value.CreateIconElement();
                AppIconElement.Child = icon;

                if (App.MainWindow != null)
                {
                    App.MainWindow.AppWindow.SetIcon(icon.BaseUri.AbsolutePath);
                }
            }
        }
    }

    public TitleBarControl()
    {
        InitializeComponent();

        ViewModel = new TitleBarViewModel(this);

        Loaded += (_, _) => ViewModel.SetDragRegionTitleBar();
        SizeChanged += (_, _) => ViewModel.SetDragRegionTitleBar();
        IThemeSelectorService themeSelectorService = App.GetService<IThemeSelectorService>();

        themeSelectorService.ThemeChanged += (_, theme) => ViewModel.UpdateTitleBar(theme);
        ViewModel.UpdateTitleBar(themeSelectorService.ActualTheme);

        AppIconElement.PointerPressed += ViewModel.AppIcon_LeftClick;
        AppIconElement.RightTapped += TitleBarViewModel.AppIcon_RightClick;
        AppIconElement.DoubleTapped += (_, _) => App.Shutdown();

        if (App.MainWindow != null)
        {
            App.MainWindow.Title = Title;
            App.MainWindow.SetTitleBar(this);

            App.MainWindow.Activated += MainWindow_Activated;
        }

        RegisterPropertyChangedCallback(TitleProperty, Callback);
    }

    private void Callback(DependencyObject sender, DependencyProperty dp)
    {
        if (App.MainWindow != null)
        {
            App.MainWindow.Title = Title;
        }
    }

    public void MainWindow_Activated(object sender, WindowActivatedEventArgs args)
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
