using System.Diagnostics.CodeAnalysis;
using CommunityToolkit.WinUI.Animations;
using MediaMaster.Extensions;
using MediaMaster.Interfaces.Services;
using MediaMaster.Interfaces.ViewModels;
using Microsoft.UI.Xaml.Media.Animation;
using Microsoft.UI.Xaml.Navigation;

namespace MediaMaster.Services.Navigation;

public abstract class NavigationService(IPageService pageService) : INavigationService
{
    protected Frame? _frame;
    private object? _lastParameterUsed;

    public event NavigatedEventHandler? Navigated;

    public abstract Frame? Frame { get; set; }

    [MemberNotNullWhen(true, nameof(Frame), nameof(_frame))]
    public bool CanGoBack => Frame != null && Frame.CanGoBack;

    public bool GoBack()
    {
        if (!CanGoBack) return false;

        var vmBeforeNavigation = _frame.GetPageViewModel();
        _frame.GoBack();
        if (vmBeforeNavigation is INavigationAware navigationAware)
        {
            navigationAware.OnNavigatedFrom();
        }

        return true;
    }

    public bool NavigateTo(string? pageKey, object? parameter = null, NavigationTransitionInfo? infoOverride = null, bool clearNavigation = false)
    {
        if (pageKey == null) return false;

        Type pageType = pageService.GetPageType(pageKey);

        if (_frame == null ||(_frame.Content?.GetType() == pageType && (parameter == null || parameter.Equals(_lastParameterUsed)))) return false;

        _frame.Tag = clearNavigation;
        var vmBeforeNavigation = _frame.GetPageViewModel();
        bool navigated;
        if (infoOverride == null)
        {
            navigated = _frame.Navigate(pageType, parameter);
        }
        else
        {
            navigated = _frame.Navigate(pageType, parameter, infoOverride);
        }

        if (!navigated) return false;

        _lastParameterUsed = parameter;
        if (vmBeforeNavigation is INavigationAware navigationAware)
        {
            navigationAware.OnNavigatedFrom();
        }

        return true;
    }

    public void SetListDataItemForNextConnectedAnimation(object item)
    {
        Frame?.SetListDataItemForNextConnectedAnimation(item);
    }

    protected void RegisterFrameEvents()
    {
        if (_frame != null)
        {
            _frame.Navigated += OnNavigated;
        }
    }

    protected void UnregisterFrameEvents()
    {
        if (_frame != null)
        {
            _frame.Navigated -= OnNavigated;
        }
    }

    private void OnNavigated(object sender, NavigationEventArgs args)
    {
        if (sender is not Frame frame) return;


        var clearNavigation = (bool)frame.Tag;
        if (clearNavigation)
        {
            frame.BackStack.Clear();
        }

        if (frame.GetPageViewModel() is INavigationAware navigationAware)
        {
            navigationAware.OnNavigatedTo(args.Parameter);
        }

        Navigated?.Invoke(sender, args);
    }
}