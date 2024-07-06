using Windows.Foundation;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Input;
using Windows.System;
using Windows.UI.Core;
using Microsoft.IdentityModel.Tokens;
using Microsoft.UI.Xaml;
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
            SetText(Text);
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
            SetText(Text);
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

    public static readonly DependencyProperty EditOnDoubleClickProperty
        = DependencyProperty.Register(
            nameof(EditOnDoubleClick),
            typeof(bool),
            typeof(EditableTextBlock),
            new PropertyMetadata(true));

    public bool EditOnDoubleClick
    {
        get => (bool)GetValue(EditOnDoubleClickProperty);
        set => SetValue(EditOnDoubleClickProperty, value);
    }

    public static readonly DependencyProperty EditOnClickProperty
        = DependencyProperty.Register(
            nameof(EditOnClick),
            typeof(bool),
            typeof(EditableTextBlock),
            new PropertyMetadata(true));

    public bool EditOnClick
    {
        get => (bool)GetValue(EditOnClickProperty);
        set => SetValue(EditOnClickProperty, value);
    }

    public event TypedEventHandler<EditableTextBlock, string>? TextConfirmed;
    public event TypedEventHandler<EditableTextBlock, string>? Edit;

    public EditableTextBlock()
    {
        InitializeComponent();
        SetText(_text);
    }

    private void EditButton_OnClick(object sender, RoutedEventArgs e)
    {
        App.DispatcherQueue.EnqueueAsync(() => Edit?.Invoke(this, Text));
        if (EditOnClick)
        {
            EditText();
        }
    }

    private void EditableTextBlock_OnDoubleTapped(object sender, DoubleTappedRoutedEventArgs e)
    {
        if (EditOnDoubleClick)
        {
            EditText();
        }
    }

    private void ConfirmButton_OnClick(object sender, RoutedEventArgs e)
    {
        ConfirmChanges();
    }

    private void TextBox_OnKeyDown(object sender, KeyRoutedEventArgs e)
    {
        if (e.Key == VirtualKey.Enter && ConfirmOnReturn)
        {
            ConfirmChanges();
            e.Handled = true;
        }
    }

    private async void EditableTextBlock_OnLosingFocus(object? sender, RoutedEventArgs? e)
    {
        await Task.Delay(1);
        if (!TextBox.ContextFlyout.IsOpen)
        {
            if (ConfirmOnFocusLoss)
            {
                ConfirmChanges();
            }
            else
            {
                CancelChanges();
            }
        }
    }

    public void EditText()
    {
        ShowTextBox();

        SetText(Text);
        TextBox.Focus(FocusState.Programmatic);
        TextBox.SelectAll();
    }

    public void ConfirmChanges()
    {
        HideTextBox();
        Text = TextBox.Text;
        App.DispatcherQueue.EnqueueAsync(() => TextConfirmed?.Invoke(this, Text));
    }

    public void CancelChanges()
    {
        HideTextBox();
        SetText(Text);
        TextBox.IsEnabled = false;
    }

    private void ShowTextBox()
    {
        TextBlock.Opacity = 0;
        TextBlock.IsHitTestVisible = false;
        EditButton.Opacity = 0;
        EditButton.IsHitTestVisible = false;
        EditButton.IsEnabled = false;

        TextBox.Opacity = 1;
        TextBox.IsHitTestVisible = true;
        TextBox.IsTabStop = true;
        TextBox.IsEnabled = true;
    }

    private void HideTextBox()
    {
        TextBox.Opacity = 0;
        TextBox.IsHitTestVisible = false;
        TextBox.IsTabStop = false;
        TextBox.IsEnabled = false;

        TextBlock.Opacity = 1;
        TextBlock.IsHitTestVisible = true;
        EditButton.Opacity = 1;
        EditButton.IsHitTestVisible = true;
        EditButton.IsEnabled = true;
    }

    private void TextBox_TextChanging(TextBox textBox, TextBoxTextChangingEventArgs textBoxTextChangingEventArgs)
    {
        var selectionStart = TextBox.SelectionStart;
        TextBox.SelectionStart = 0;
        SetText(TextBox.Text);
        TextBox.SelectionStart = selectionStart;
    }

    private void SetText(string text)
    {
        _text = text;

        VisualStateManager.GoToState(this, text.IsNullOrEmpty() ? "PlaceholderTextState" : "CurrentTextState", true);
        
        if (TextBox.Text != text)
        {
            TextBox.Text = text;
        }
        TextBlock.Text = text.IsNullOrEmpty() ? PlaceholderText : text;

        ResizeTextBox();
    }

    private async void TextBlock_OnSizeChanged(object sender, SizeChangedEventArgs e)
    {
        await Task.Yield();
        ResizeTextBox();
    }

    private void ResizeTextBox()
    {
        TextBlock.SizeChanged -= TextBlock_OnSizeChanged;
        TextBox.Height = 0;
        TextBox.Width = 0;
        UpdateLayout();
        TextBox.Height = Math.Max(TextBlock.MinHeight, Math.Min(TextBlock.DesiredSize.Height, TextBlock.MaxHeight));
        TextBox.Width = Math.Min(TextBlock.ActualSize.X + EditButton.Width + 16, TextGrid.ActualSize.X);
        UpdateLayout();
        TextBlock.SizeChanged += TextBlock_OnSizeChanged;
    }
}

public class ConfirmButton : Button
{
    public ConfirmButton()
    {
        ProtectedCursor = InputCursor.CreateFromCoreCursor(new CoreCursor(CoreCursorType.Arrow, 0));
    }
}
