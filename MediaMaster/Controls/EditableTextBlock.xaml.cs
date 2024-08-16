using Windows.Foundation;
using Microsoft.UI.Xaml.Input;
using Windows.System;
using CommunityToolkit.WinUI;
using DependencyPropertyGenerator;
using MediaMaster.Extensions;
using Microsoft.UI.Input;

namespace MediaMaster.Controls;

public class TextConfirmedArgs(string oldText, string newText)
{
    public string OldText = oldText;
    public string NewText = newText;
}

[DependencyProperty("Text", typeof(string), DefaultValue = "")]
[DependencyProperty("PlaceholderText", typeof(string), DefaultValue = "")]
[DependencyProperty("ConfirmOnReturn", typeof(bool), DefaultValue = true)]
[DependencyProperty("ConfirmOnFocusLoss", typeof(bool), DefaultValue = true)]
[DependencyProperty("EditOnDoubleClick", typeof(bool), DefaultValue = true)]
[DependencyProperty("EditOnClick", typeof(bool), DefaultValue = true)]
public sealed partial class EditableTextBlock : UserControl
{
    private string _text = "";

    partial void OnTextChanged(string newValue)
    {
        SetText(newValue);
    }

    partial void OnPlaceholderTextChanged()
    {
        SetText(Text);
    }

    public event TypedEventHandler<EditableTextBlock, TextConfirmedArgs>? TextConfirmed;
    public event TypedEventHandler<EditableTextBlock, string>? EditButtonPressed;

    private DateTime? _focusGainedTime;

    public EditableTextBlock()
    {
        InitializeComponent();
        Loaded += (_, _) => SetText(_text);

        TextBlock.AddHandler(DoubleTappedEvent, new DoubleTappedEventHandler(OnDoubleTapped), true);
    }

    private void EditButton_OnClick(object sender, RoutedEventArgs e)
    {
        App.DispatcherQueue.EnqueueAsync(() => EditButtonPressed?.Invoke(this, Text));
        if (EditOnClick)
        {
            EditText();
        }
    }

    private void OnDoubleTapped(object sender, DoubleTappedRoutedEventArgs e)
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

    private async void EditableTextBlock_OnLosingFocus(object sender, LosingFocusEventArgs e)
    {
        await Task.Yield();
        if (_focusGainedTime != null && DateTime.Now - _focusGainedTime < TimeSpan.FromMilliseconds(200))
        {
            
            TextBox.Focus(FocusState.Programmatic);
            TextBox.SelectAll();
            return;
        }
        _focusGainedTime = null;

        if (!TextBox.ContextFlyout.IsOpen && e.NewFocusedElement != TextBox)
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
        _focusGainedTime = DateTime.Now;
        ShowTextBox();

        SetText(Text);
        TextBox.Focus(FocusState.Programmatic);
        TextBox.SelectAll();
    }

    public void ConfirmChanges()
    {
        HideTextBox();
        var oldText = Text;
        Text = TextBox.Text;
        var args = new TextConfirmedArgs(oldText, Text);
        App.DispatcherQueue.EnqueueAsync(() => TextConfirmed?.Invoke(this, args));
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

        if (TextBox.Text != text)
        {
            TextBox.TextChanging -= TextBox_TextChanging;
            TextBox.Text = text;
            TextBox.TextChanging += TextBox_TextChanging;
        }

        var newText = text.IsNullOrEmpty() ? PlaceholderText : text;
        if (TextBlock.Text != newText)
        {
            TextBlock.Text = newText;
            VisualStateManager.GoToState(this, text.IsNullOrEmpty() ? "PlaceholderTextState" : "CurrentTextState", true);
        }
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

public partial class ConfirmButton : Button
{
    public ConfirmButton()
    {
        ProtectedCursor = InputSystemCursor.Create(InputSystemCursorShape.Arrow);
    }
}
