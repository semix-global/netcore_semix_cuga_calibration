using System;
using System.Windows;
using System.Windows.Media.Animation;

namespace Net.Utilities.WPF.CustomControl.Calibration;

/// <summary>
/// Animates the value of a <see cref="GridLength"/> dependency property
/// between two target values using linear interpolation over a specified duration.
/// </summary>
public class GridLengthAnimation : AnimationTimeline
{
    public static readonly DependencyProperty FromProperty = DependencyProperty.Register(
        nameof(From),
        typeof(GridLength),
        typeof(GridLengthAnimation),
        new PropertyMetadata(new GridLength(0.0)));

    public static readonly DependencyProperty ToProperty = DependencyProperty.Register(
        nameof(To),
        typeof(GridLength),
        typeof(GridLengthAnimation),
        new PropertyMetadata(new GridLength(0.0)));

    public static readonly DependencyProperty EasingFunctionProperty = DependencyProperty.Register(
        nameof(EasingFunction),
        typeof(IEasingFunction),
        typeof(GridLengthAnimation),
        new PropertyMetadata(null));

    public GridLength From
    {
        get => (GridLength)GetValue(FromProperty);
        set => SetValue(FromProperty, value);
    }

    public GridLength To
    {
        get => (GridLength)GetValue(ToProperty);
        set => SetValue(ToProperty, value);
    }

    public IEasingFunction? EasingFunction
    {
        get => (IEasingFunction?)GetValue(EasingFunctionProperty);
        set => SetValue(EasingFunctionProperty, value);
    }

    public override Type TargetPropertyType => typeof(GridLength);

    public override object GetCurrentValue(object defaultOriginValue, object defaultDestinationValue, AnimationClock animationClock)
    {
        if (animationClock.CurrentProgress is not { } progress)
        {
            return From;
        }

        var easedProgress = EasingFunction?.Ease(progress) ?? progress;
        var fromValue = From.IsAbsolute ? From.Value : 0.0;
        var toValue = To.IsAbsolute ? To.Value : 0.0;

        // Preserve the source grid unit type (the implementation only animates absolute pixel lengths).
        var currentValue = fromValue + (toValue - fromValue) * easedProgress;
        return new GridLength(currentValue, From.GridUnitType);
    }

    protected override Freezable CreateInstanceCore() => new GridLengthAnimation();
}
