using System.Diagnostics.CodeAnalysis;
using CommunityToolkit.WinUI.Animations;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Navigation;

using MediaMaster.Interfaces.Services;
using MediaMaster.Interfaces.ViewModels;
using MediaMaster.Helpers;

namespace MediaMaster.Services.Navigation;

public abstract class NavigationService(IPageService pageService) : INavigationService
{
    protected object? _lastParameterUsed;
    protected Frame? _frame;

    public event NavigatedEventHandler? Navigated;

    public abstract Frame? Frame { get; set; }

    [MemberNotNullWhen(true, nameof(Frame), nameof(_frame))]
    public bool CanGoBack => Frame != null && Frame.CanGoBack;

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

    public bool NavigateTo(string pageKey, object? parameter = null, bool clearNavigation = false)
    {
        var pageType = pageService.GetPageType(pageKey);

        if (_frame == null || _frame.Content?.GetType() == pageType && (parameter == null || parameter.Equals(_lastParameterUsed))) return false;

        _frame.Tag = clearNavigation;
        var vmBeforeNavigation = _frame.GetPageViewModel();
        var navigated = _frame.Navigate(pageType, parameter);

        if (!navigated) return navigated;

        _lastParameterUsed = parameter;
        if (vmBeforeNavigation is INavigationAware navigationAware)
        {
            navigationAware.OnNavigatedFrom();
        }

        return navigated;

    }

    protected void OnNavigated(object sender, NavigationEventArgs args)
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

    public void SetListDataItemForNextConnectedAnimation(object item) => Frame?.SetListDataItemForNextConnectedAnimation(item);
}
