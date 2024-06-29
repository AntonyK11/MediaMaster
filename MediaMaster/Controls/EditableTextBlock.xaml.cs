using Windows.Foundation;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Input;
using Windows.System;
using Windows.UI.Core;
using Microsoft.IdentityModel.Tokens;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Media;
using CommunityToolkit.WinUI;
using Microsoft.UI.Input;

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

    private string _text = "";

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

    public static readonly DependencyProperty ConfirmOnReturnProperty
    = DependencyProperty.Register(
        nameof(ConfirmOnReturn),
        typeof(bool),
        typeof(EditableTextBlock),
        new PropertyMetadata(true));

    public bool ConfirmOnReturn
    {
        get => (bool)GetValue(ConfirmOnReturnProperty);
        set => SetValue(ConfirmOnReturnProperty, value);
    }

    public static readonly DependencyProperty ConfirmOnFocusLossProperty
        = DependencyProperty.Register(
            nameof(ConfirmOnFocusLoss),
            typeof(bool),
            typeof(EditableTextBlock),
            new PropertyMetadata(true));

    public bool ConfirmOnFocusLoss
    {
        get => (bool)GetValue(ConfirmOnFocusLossProperty);
        set => SetValue(ConfirmOnFocusLossProperty, value);
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
        if (e.Key == VirtualKey.Enter && ConfirmOnReturn)
        {
            Confirm();
            e.Handled = true;
        }
    }

    private void EditableTextBlock_OnLosingFocus(object? sender, RoutedEventArgs? e)
    {
        if (!TextBox.ContextFlyout.IsOpen)
        {
            if (ConfirmOnFocusLoss)
            {
                Confirm();
            }
            else
            {
                Cancel();
            }
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

        TextBox.Text = _text;
        TextChanged(_text);

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

        Text = TextBox.Text;

        TextBox.SelectionStart = 0;
        TextBox.SelectionLength = 0;
    }

    public void Cancel()
    {
        TextBox.Opacity = 0;
        TextBox.IsHitTestVisible = false;
        TextBlock.Opacity = 1;
        TextBlock.IsHitTestVisible = true;
        EditButton.Opacity = 1;
        EditButton.IsHitTestVisible = true;
        EditButton.IsEnabled = true;

        TextBox.Text = Text;
        TextChanged(Text);

        TextBox.SelectionStart = 0;
        TextBox.SelectionLength = 0;

        App.DispatcherQueue.EnqueueAsync(() => TextConfirmed?.Invoke(this, Text));
    }

    private void TextBox_OnBeforeTextChanging(TextBox textBox, TextBoxBeforeTextChangingEventArgs args)
    {
        TextBox.Text = args.NewText;
        TextChanged(args.NewText);
        TextBox.SelectionStart = args.NewText.Length;
    }

    private void TextChanged(string text)
    {
        _text = text;
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
        TextBox.Height = Math.Max(TextBlock.MinHeight, Math.Min(TextBlock.ActualHeight, TextBlock.MaxHeight));
        TextBox.Width = Math.Min(TextBlock.ActualWidth + EditButton.ActualWidth + 16, TextGrid.Width);
        UpdateLayout();
    }
}

public class ConfirmButton : Button
{
    public ConfirmButton()
    {
        ProtectedCursor = InputCursor.CreateFromCoreCursor(new CoreCursor(CoreCursorType.Arrow, 0));
    }
}

