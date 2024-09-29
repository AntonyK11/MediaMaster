using System.Numerics;
using DependencyPropertyGenerator;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Composition;
using Windows.Foundation;

namespace MediaMaster.Controls;

public enum ButtonType
{
    Favorite,
    Archive
}

[DependencyProperty("ButtonType", typeof(ButtonType), DefaultValue = ButtonType.Favorite)] 
[DependencyProperty("IsChecked", typeof(bool), DefaultValue = false)]
public sealed partial class IconToggleButton : UserControl
{
    private SpringScalarNaturalMotionAnimation? _rotationAnimation;
    private Vector3KeyFrameAnimation? _scaleAnimation;

    private SpringVector3NaturalMotionAnimation? _translationAnimation;

    public event TypedEventHandler<object, RoutedEventArgs>? Checked;
    public event TypedEventHandler<object, RoutedEventArgs>? Unchecked;
    public event TypedEventHandler<object, RoutedEventArgs>? Click;

    public IconToggleButton()
    {
        this.InitializeComponent();
    }

    private void CreateOrUpdateTranslationAnimation(float? initialValue, float finalValue)
    {
        if (_translationAnimation == null)
        {
            _translationAnimation = App.MainWindow!.Compositor.CreateSpringVector3Animation();
            _translationAnimation.Target = "Translation";
            _translationAnimation.Period = TimeSpan.FromMilliseconds(32);
            _translationAnimation.StopBehavior = AnimationStopBehavior.SetToFinalValue;
        }

        _translationAnimation.InitialValue = initialValue == null ? null : new Vector3(0, (float)initialValue, 0);
        _translationAnimation.FinalValue = new Vector3(0, finalValue, 0);
    }

    private void CreateOrUpdateRotationAnimation(float? initialValue, float finalValue)
    {
        if (_rotationAnimation == null)
        {
            _rotationAnimation = App.MainWindow!.Compositor.CreateSpringScalarAnimation();
            _rotationAnimation.Target = "Rotation";
            _rotationAnimation.Period = TimeSpan.FromMilliseconds(64);
            _rotationAnimation.StopBehavior = AnimationStopBehavior.SetToFinalValue;
        }

        _rotationAnimation.InitialValue = initialValue;
        _rotationAnimation.FinalValue = finalValue;
    }

    private void CreateOrUpdateScaleAnimation(float initialValue, float middleValue, float finalValue)
    {
        if (_scaleAnimation == null)
        {
            _scaleAnimation = App.MainWindow!.Compositor.CreateVector3KeyFrameAnimation();
            _scaleAnimation.Target = "Scale";
            _scaleAnimation.Duration = TimeSpan.FromMilliseconds(256);
            _scaleAnimation.StopBehavior = AnimationStopBehavior.SetToFinalValue;
        }

        _scaleAnimation.InsertKeyFrame(0, new Vector3(initialValue));
        _scaleAnimation.InsertKeyFrame(0.5f, new Vector3(middleValue));
        _scaleAnimation.InsertKeyFrame(1, new Vector3(finalValue));
    }

    private void ToggleButton_OnChecked(object sender, RoutedEventArgs e)
    {
        CreateOrUpdateTranslationAnimation(2, 0);
        if (ButtonType == ButtonType.Favorite)
        {
            FavoriteGrid.StartAnimation(_translationAnimation);
        }
        else
        {
            ArchiveGrid.StartAnimation(_translationAnimation);
        }

        if (ButtonType == ButtonType.Favorite)
        {
            CreateOrUpdateRotationAnimation(-360, 0);
            FavoriteGrid.StartAnimation(_rotationAnimation);
        }
        else
        {
            CreateOrUpdateScaleAnimation(1, 1.2f, 1);
            ArchiveGrid.StartAnimation(_scaleAnimation);
        }

        Checked?.Invoke(this, e);
    }

    private void ToggleButton_OnUnchecked(object sender, RoutedEventArgs e)
    {
        CreateOrUpdateTranslationAnimation(2, 0);
        if (ButtonType == ButtonType.Favorite)
        {
            FavoriteGrid.StartAnimation(_translationAnimation);
        }
        else
        {
            ArchiveGrid.StartAnimation(_translationAnimation);
        }

        Unchecked?.Invoke(this, e);
    }

    private void ToggleButton_OnPointerEntered(object sender, PointerRoutedEventArgs e)
    {
        if (!e.GetCurrentPoint(ToggleButtonControl).Properties.IsLeftButtonPressed) return;
        CreateOrUpdateTranslationAnimation(0, 2);
        if (ButtonType == ButtonType.Favorite)
        {
            FavoriteGrid.StartAnimation(_translationAnimation);
        }
        else
        {
            ArchiveGrid.StartAnimation(_translationAnimation);
        }
    }

    private void ToggleButton_OnPointerPressed(object sender, PointerRoutedEventArgs e)
    {
        CreateOrUpdateTranslationAnimation(0, 2);
        if (ButtonType == ButtonType.Favorite)
        {
            FavoriteGrid.StartAnimation(_translationAnimation);
        }
        else
        {
            ArchiveGrid.StartAnimation(_translationAnimation);
        }
    }

    private void ToggleButton_OnPointerExited(object sender, PointerRoutedEventArgs e)
    {
        CreateOrUpdateTranslationAnimation(null, 0);
        if (ButtonType == ButtonType.Favorite)
        {
            FavoriteGrid.StartAnimation(_translationAnimation);
        }
        else
        {
            ArchiveGrid.StartAnimation(_translationAnimation);
        }
    }

    private void ToggleButtonControl_OnClick(object sender, RoutedEventArgs e)
    {
        Click?.Invoke(this, e);
    }
}

