﻿using Microsoft.IdentityModel.Tokens;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using WinUI3Localizer;

namespace MediaMaster.Services.MediaInfo;

public abstract class MediaInfoTextBase(StackPanel parent) : MediaInfoControlBase(parent)
{
    public TextBlock? Text;
    public StackPanel? StackPanel;

    public override void Setup(bool isCompact)
    {
        StackPanel = new StackPanel
        {
            Spacing = 4
        };
        Title = GetTitle();
        Text = new TextBlock
        {
            IsTextSelectionEnabled = true,
            Padding = new Thickness(11,6,8,7),
            TextWrapping = TextWrapping.WrapWholeWords
        };
        StackPanel.Children.Add(Title);
        StackPanel.Children.Add(Text);
        Parent.Children.Add(StackPanel);
    }

    public override void Show(bool isCompact)
    {
        if (StackPanel != null)
        {
            StackPanel.Visibility = Visibility.Visible;
        }
    }

    public override void Hide()
    {
        if (StackPanel != null)
        {
            StackPanel.Visibility = Visibility.Collapsed;
        }
    }

    public override void SetupTranslations()
    {
        if (Title != null && !TranslationKey.IsNullOrEmpty())
        {
            Uids.SetUid(Title, $"/Media/{TranslationKey}_Title");
        }
    }
}

