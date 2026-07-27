using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Shapes;
using CommunityToolkit.Diagnostics;

namespace Net.Utilities.WPF.CustomControl.Calibration.CircleProgresses;

public sealed class CircleProgressButton : Button
{
    static CircleProgressButton()
    {
        DefaultStyleKeyProperty.OverrideMetadata(typeof(CircleProgressButton), new FrameworkPropertyMetadata(typeof(CircleProgressButton)));
    }

    public static readonly DependencyProperty EffectBackgroundProperty = DependencyProperty.Register(
        nameof(EffectBackground),
        typeof(Brush),
        typeof(CircleProgressButton),
        new PropertyMetadata(Brushes.DarkGray, OnChanged));

    public static readonly DependencyProperty ProgressProperty = DependencyProperty.Register(
        nameof(Progress),
        typeof(double),
        typeof(CircleProgressButton),
        new PropertyMetadata(0d, OnChanged));

    public static readonly DependencyProperty IsOkProperty = DependencyProperty.Register(
        nameof(IsOk),
        typeof(bool),
        typeof(CircleProgressButton),
        new PropertyMetadata(false, OnChanged));

    private static void OnChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        if (d is not CircleProgressButton circleProgressButton) return;

        circleProgressButton.Update();
    }

    public Brush EffectBackground
    {
        get => (Brush)GetValue(EffectBackgroundProperty);
        set => SetValue(EffectBackgroundProperty, value);
    }

    public double Progress
    {
        get => (double)GetValue(ProgressProperty);
        set => SetValue(ProgressProperty, value);
    }

    public bool IsOk
    {
        get => (bool)GetValue(IsOkProperty);
        set => SetValue(IsOkProperty, value);
    }

    private Ellipse? _progressEllipse;
    private RectangleGeometry? _progressEllipseClipGeometry;
    private Image? _checkMarkImage;

    public override void OnApplyTemplate()
    {
        base.OnApplyTemplate();

        _progressEllipse = Guard.IsNotNullAndAssignableToTypeAndReturn<Ellipse>(GetTemplateChild("PART_ProgressEllipse"));
        _progressEllipseClipGeometry = Guard.IsNotNullAndAssignableToTypeAndReturn<RectangleGeometry>(GetTemplateChild("PART_ProgressEllipseClipGeometry"));
        _checkMarkImage = Guard.IsNotNullAndAssignableToTypeAndReturn<Image>(GetTemplateChild("PART_CheckMarkImage"));

        Update();
    }

    private void Update()
    {
        if (_progressEllipse is null ||
            _progressEllipseClipGeometry is null ||
            _checkMarkImage is null) return;

        if (IsOk)
        {
            _progressEllipse.Visibility = Visibility.Collapsed;
            _checkMarkImage.Visibility = Visibility.Visible;
        }
        else
        {
            var progress = Math.Max(0, Math.Min(100, Progress));
            var height = ActualHeight * progress / 100.0;

            _progressEllipseClipGeometry.Rect = new Rect(0, ActualHeight - height, ActualWidth, height);

            _progressEllipse.Visibility = Visibility.Visible;
            _checkMarkImage.Visibility = Visibility.Collapsed;
        }

        ToolTip = $"Percentage of Completion: {Progress:0.###}%";
    }
}