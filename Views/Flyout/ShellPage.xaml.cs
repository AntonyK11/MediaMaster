using MediaMaster.Interfaces.Services;
using Microsoft.UI.Xaml;
using MediaMaster.Services.Navigation;
using MediaMaster.ViewModels.Flyout;
namespace MediaMaster.Views.Flyout;

public sealed partial class ShellPage
{
    public ShellViewModel ViewModel { get; }

    public ShellPage()
    {
        InitializeComponent();
        Loaded += OnLoaded;
        App.GetService<FlyoutNavigationService>().Frame = ContentFrame;
        ViewModel = App.GetService<ShellViewModel>();

        App.GetService<IThemeSelectorService>().ThemeChanged += async (_, _) =>
        {
            await Task.Delay(1); // To account for the delay in the theme change
            ViewModel.IsFocused = !ViewModel.IsFocused;
            ViewModel.IsFocused = !ViewModel.IsFocused;
        };
    }

    public void OnLoaded(object sender, RoutedEventArgs e)
    {
        App.Flyout!.Activated += MainWindow_Activated;
    }

    public void Hide_Flyout(object sender, RoutedEventArgs routedEventArgs)
    {
        App.Flyout?.Hide_Flyout();
    }

    public void MainWindow_Activated(object sender, WindowActivatedEventArgs args)
    {
        ViewModel.IsFocused = args.WindowActivationState != WindowActivationState.Deactivated;
    }
}