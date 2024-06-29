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
            TextChanged(value);
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
            TextChanged(Text);
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
        if (!TextBox.ContextFlyout.IsOpen)
        {
            Confirm();
        }
    }

    public void Edit()
    {
        TextBlock.Opacity = 0;
        TextBlock.IsHitTestVisible = false;
        EditButton.Opacity = 0;
        EditButton.IsHitTestVisible = false;
        EditButton.IsEnabled = false;
        TextBox.Opacity = 1;
        TextBox.IsHitTestVisible = true;

        ResizeTextBox();

        TextBox.Focus(FocusState.Programmatic);
        TextBox.SelectAll();
    }

    public void Confirm()
    {
        Text = TextBox.Text;
        TextBox.Opacity = 0;
        TextBox.IsHitTestVisible = false;
        TextBlock.Opacity = 1;
        TextBlock.IsHitTestVisible = true;
        EditButton.Opacity = 1;
        EditButton.IsHitTestVisible = true;
        EditButton.IsEnabled = true;

        TextChanged(Text);

        TextBox.SelectionStart = 0;
        TextBox.SelectionLength = 0;

        App.DispatcherQueue.EnqueueAsync(() => TextConfirmed?.Invoke(this, Text));
    }

    private void TextBox_OnTextChanged(object sender, TextChangedEventArgs e)
    {
        TextChanged(TextBox.Text);
    }

    private void TextChanged(string text)
    {
        if (text.IsNullOrEmpty())
        {
            TextBlock.Text = PlaceholderText;
            TextBlock.CharacterSpacing = 0;
            TextBlock.Foreground = (SolidColorBrush)Resources["TextControlPlaceholderForeground"];
        }
        else
        {
            TextBlock.Text = text;
            TextBlock.CharacterSpacing = 23;
            TextBlock.Foreground = (SolidColorBrush)Resources["TextControlForeground"];
        }
        ResizeTextBox();
    }

    private void Grid_OnSizeChanged(object sender, SizeChangedEventArgs e)
    {
        ResizeTextBox();
    }

    private void ResizeTextBox()
    {
        TextBox.Height = 0;
        TextBox.Width = 0;
        UpdateLayout();
        TextBox.Height = TextBlock.ActualHeight;
        TextBox.Width = Math.Min(TextBlock.ActualWidth + EditButton.ActualWidth + 16, TextGrid.Width);
    }
}

