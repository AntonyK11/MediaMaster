using Windows.Foundation;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Input;
using Windows.System;
using Microsoft.IdentityModel.Tokens;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Media;
using CommunityToolkit.WinUI;

namespace MediaMaster.Controls;

public sealed partial class EditableTextBlock : UserControl
{
    public static readonly DependencyProperty TextProperty
        = DependencyProperty.Register(
            nameof(Text),
            typeof(string),
            typeof(EditableTextBlock),
            new PropertyMetadata(""));

    public string Text
    {
        get => (string)GetValue(TextProperty);
        set
        {
            SetValue(TextProperty, value);
            SetTextBlockText();
        }
    }

    public static readonly DependencyProperty PlaceholderTextProperty
        = DependencyProperty.Register(
            nameof(PlaceholderText),
            typeof(string),
            typeof(EditableTextBlock),
            new PropertyMetadata(""));

    public string PlaceholderText
    {
        get => (string)GetValue(PlaceholderTextProperty);
        set
        {
            SetValue(PlaceholderTextProperty, value);
            SetTextBlockText();
        }
    }

    public event TypedEventHandler<EditableTextBlock, string>? TextConfirmed;

    public EditableTextBlock()
    {
        InitializeComponent();
    }

    private void EditButton_OnClick(object sender, RoutedEventArgs e)
    {
        Edit();
    }

    private void EditableTextBlock_OnDoubleTapped(object sender, DoubleTappedRoutedEventArgs e)
    {
        Edit();
    }

    private void ConfirmButton_OnClick(object sender, RoutedEventArgs e)
    {
        Confirm();
    }
    private void TextBox_OnKeyDown(object sender, KeyRoutedEventArgs e)
    {
        if (e.Key == VirtualKey.Enter)
        {
            Confirm();
            e.Handled = true;
        }
    }

    private void EditableTextBlock_OnLostFocus(object? sender, RoutedEventArgs? e)
    {
        Confirm();
    }

    public void Edit()
    {
        TextBox.Visibility = Visibility.Visible;
        TextBlock.Visibility = Visibility.Collapsed;
        EditButton.Visibility = Visibility.Collapsed;

        TextBox.Focus(FocusState.Programmatic);
        TextBox.SelectAll();
    }

    public void Confirm()
    {
        TextBox.Visibility = Visibility.Collapsed;
        TextBlock.Visibility = Visibility.Visible;
        EditButton.Visibility = Visibility.Visible;

        SetTextBlockText();

        TextBox.SelectionStart = 0;
        TextBox.SelectionLength = 0;

        App.DispatcherQueue.EnqueueAsync(() => TextConfirmed?.Invoke(this, Text));
    }

    private void SetTextBlockText()
    {
        if (Text.IsNullOrEmpty())
        {
            TextBlock.Text = PlaceholderText;
            TextBlock.CharacterSpacing = 0;
            TextBlock.Foreground = (SolidColorBrush)Resources["TextControlPlaceholderForeground"];
        }
        else
        {
            TextBlock.Text = Text;
            TextBlock.CharacterSpacing = 18;
            TextBlock.Foreground = (SolidColorBrush)Resources["TextControlForeground"];
        }
    }
}

