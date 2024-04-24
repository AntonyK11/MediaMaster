﻿using CommunityToolkit.Mvvm.ComponentModel;
using MediaMaster.Interfaces.Services;
using MediaMaster.Services.Navigation;
using MediaMaster.Views;
using Microsoft.UI.Xaml.Navigation;

namespace MediaMaster.ViewModels;

public partial class ShellViewModel : ObservableObject
{
    [ObservableProperty] private bool _isBackEnabled;

    [ObservableProperty] private object? _selected;

    public INavigationService NavigationService { get; }

    public INavigationViewService NavigationViewService { get; }

    public ShellViewModel(MainNavigationService mainNavigationService, MainNavigationViewService mainNavigationViewService)
    {
        NavigationService = mainNavigationService;
        NavigationService.Navigated += OnNavigated;
        NavigationViewService = mainNavigationViewService;
    }

    private void OnNavigated(object sender, NavigationEventArgs args)
    {
        IsBackEnabled = NavigationService.CanGoBack;

        if (args.SourcePageType == typeof(SettingsPage))
        {
            Selected = NavigationViewService.SettingsItem;
            return;
        }

        var selectedItem = NavigationViewService.GetSelectedItem(args.SourcePageType);
        if (selectedItem != null)
        {
            Selected = selectedItem;
        }
    }
}