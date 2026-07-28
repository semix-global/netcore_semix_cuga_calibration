using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Shapes;
using CommunityToolkit.Diagnostics;

namespace Net.Utilities.WPF.CustomControl.Calibration;

public enum HorizontalExpandDirectionEnum
{
    Left,
    Right
}

public sealed class HorizontalExpandPanel : Control
{
    static HorizontalExpandPanel()
    {
        DefaultStyleKeyProperty.OverrideMetadata(typeof(HorizontalExpandPanel), new FrameworkPropertyMetadata(typeof(HorizontalExpandPanel)));
    }

    public static readonly DependencyProperty PanelBackgroundProperty = DependencyProperty.Register(
        nameof(PanelBackground),
        typeof(Brush),
        typeof(HorizontalExpandPanel),
        new PropertyMetadata(Brushes.Transparent));

    public static readonly DependencyProperty PanelHoverBackgroundProperty = DependencyProperty.Register(
        nameof(PanelHoverBackground),
        typeof(Brush),
        typeof(HorizontalExpandPanel),
        new PropertyMetadata(Brushes.Transparent));

    public static readonly DependencyProperty PanelForegroundProperty = DependencyProperty.Register(
        nameof(PanelForeground),
        typeof(Brush),
        typeof(HorizontalExpandPanel),
        new PropertyMetadata(Brushes.Transparent));

    public static readonly DependencyProperty PanelBorderBrushProperty = DependencyProperty.Register(
        nameof(PanelBorderBrush),
        typeof(Brush),
        typeof(HorizontalExpandPanel),
        new PropertyMetadata(Brushes.Black));

    public static readonly DependencyProperty PanelBorderThicknessProperty = DependencyProperty.Register(
        nameof(PanelBorderThickness),
        typeof(Thickness),
        typeof(HorizontalExpandPanel),
        new PropertyMetadata(default(Thickness), OnChanged));

    public static readonly DependencyProperty PanelBorderCornerRadiusProperty = DependencyProperty.Register(
        nameof(PanelBorderCornerRadius),
        typeof(CornerRadius),
        typeof(HorizontalExpandPanel),
        new PropertyMetadata(default(CornerRadius), OnChanged));

    public static readonly DependencyProperty PanelVisibilityProperty = DependencyProperty.Register(
        nameof(PanelVisibility),
        typeof(Visibility),
        typeof(HorizontalExpandPanel),
        new PropertyMetadata(Visibility.Visible, OnChanged));

    public static readonly DependencyProperty MainContentProperty = DependencyProperty.Register(
        nameof(MainContent),
        typeof(object),
        typeof(HorizontalExpandPanel),
        new PropertyMetadata(null));

    public static readonly DependencyProperty ExpandContentProperty = DependencyProperty.Register(
        nameof(ExpandContent),
        typeof(object),
        typeof(HorizontalExpandPanel),
        new PropertyMetadata(null));

    public static readonly DependencyProperty HorizontalExpandDirectionEnumProperty = DependencyProperty.Register(
        nameof(HorizontalExpandDirectionEnum),
        typeof(HorizontalExpandDirectionEnum),
        typeof(HorizontalExpandPanel),
        new PropertyMetadata(HorizontalExpandDirectionEnum.Right, OnChanged));

    public static readonly DependencyProperty IsExpandedProperty = DependencyProperty.Register(
        nameof(IsExpanded),
        typeof(bool),
        typeof(HorizontalExpandPanel),
        new PropertyMetadata(false, OnChanged));

    public Brush PanelBackground
    {
        get => (Brush)GetValue(PanelBackgroundProperty);
        set => SetValue(PanelBackgroundProperty, value);
    }

    public Brush PanelHoverBackground
    {
        get => (Brush)GetValue(PanelHoverBackgroundProperty);
        set => SetValue(PanelHoverBackgroundProperty, value);
    }

    public Brush PanelForeground
    {
        get => (Brush)GetValue(PanelForegroundProperty);
        set => SetValue(PanelForegroundProperty, value);
    }

    public Brush PanelBorderBrush
    {
        get => (Brush)GetValue(PanelBorderBrushProperty);
        set => SetValue(PanelBorderBrushProperty, value);
    }

    public Thickness PanelBorderThickness
    {
        get => (Thickness)GetValue(PanelBorderThicknessProperty);
        set => SetValue(PanelBorderThicknessProperty, value);
    }

    public CornerRadius PanelBorderCornerRadius
    {
        get => (CornerRadius)GetValue(PanelBorderCornerRadiusProperty);
        set => SetValue(PanelBorderCornerRadiusProperty, value);
    }

    public Visibility PanelVisibility
    {
        get => (Visibility)GetValue(PanelVisibilityProperty);
        set => SetValue(PanelVisibilityProperty, value);
    }

    public object? MainContent
    {
        get => GetValue(MainContentProperty);
        set => SetValue(MainContentProperty, value);
    }

    public object? ExpandContent
    {
        get => GetValue(ExpandContentProperty);
        set => SetValue(ExpandContentProperty, value);
    }

    public HorizontalExpandDirectionEnum HorizontalExpandDirectionEnum
    {
        get => (HorizontalExpandDirectionEnum)GetValue(HorizontalExpandDirectionEnumProperty);
        set => SetValue(HorizontalExpandDirectionEnumProperty, value);
    }

    public bool IsExpanded
    {
        get => (bool)GetValue(IsExpandedProperty);
        set => SetValue(IsExpandedProperty, value);
    }

    private static void OnChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        if (d is not HorizontalExpandPanel control) return;

        control.Update();
    }

    private static Geometry FreezeGeometry(string path)
    {
        var geometry = Geometry.Parse(path);
        geometry.Freeze();
        return geometry;
    }

    private static readonly Geometry LeftArrowGeometry = FreezeGeometry("F1 M1024,1024z M0,0z M724,218.3L724,141C724,134.3,716.3,130.6,711.1,134.7L260.3,486.8C243.9,499.6,243.9,524.3,260.3,537.1L711.1,889.2C716.4,893.3,724,889.6,724,882.9L724,805.6C724,800.7,721.7,796,717.9,793L357.9,512 717.9,230.9C721.7,227.9,724,223.2,724,218.3z");
    private static readonly Geometry RightArrowGeometry = FreezeGeometry("F1 M1024,1024z M0,0z M765.7,486.8L314.9,134.7C309.6,130.6,302,134.3,302,141L302,218.3C302,223.2,304.3,227.9,308.1,230.9L668.1,512 308.1,793.1C304.2,796.1,302,800.8,302,805.7L302,883C302,889.7,309.7,893.4,314.9,889.3L765.7,537.2C782.1,524.4,782.1,499.6,765.7,486.8z");

    private ColumnDefinition? _sideContentColumn;

    private Border? _toolBorder;
    private Path? _toolArrowPath;

    private ContentPresenter? _sideContentPresenter;
    private GridSplitter? _splitter;

    private double _lastSideContentColumnWidth;

    public override void OnApplyTemplate()
    {
        base.OnApplyTemplate();

        _toolBorder?.MouseDown -= ToolBorderOnMouseDown;

        _sideContentColumn = Guard.IsNotNullAndAssignableToTypeAndReturn<ColumnDefinition>(GetTemplateChild("PART_SideContentColumn"));

        _toolBorder = Guard.IsNotNullAndAssignableToTypeAndReturn<Border>(GetTemplateChild("PART_ToolBorder"));
        _toolArrowPath = Guard.IsNotNullAndAssignableToTypeAndReturn<Path>(GetTemplateChild("PART_ToolArrowPath"));

        _sideContentPresenter = Guard.IsNotNullAndAssignableToTypeAndReturn<ContentPresenter>(GetTemplateChild("PART_SideContent"));
        _splitter = Guard.IsNotNullAndAssignableToTypeAndReturn<GridSplitter>(GetTemplateChild("PART_Splitter"));

        _toolBorder.MouseDown -= ToolBorderOnMouseDown;
        _toolBorder.MouseDown += ToolBorderOnMouseDown;

        _lastSideContentColumnWidth = _sideContentColumn.Width.Value;

        Update();
    }

    private void ToolBorderOnMouseDown(object sender, MouseButtonEventArgs e) => SetCurrentValue(IsExpandedProperty, !IsExpanded);

    private void Update()
    {
        FlowDirection = HorizontalExpandDirectionEnum == HorizontalExpandDirectionEnum.Right ? FlowDirection.RightToLeft : FlowDirection.LeftToRight;

        if (_sideContentColumn is null
            || _toolBorder is null
            || _toolArrowPath is null
            || _sideContentPresenter is null
            || _splitter is null) return;

        if (IsExpanded)
        {
            _sideContentColumn.Width = new GridLength(_lastSideContentColumnWidth);
            _splitter.Visibility = Visibility.Visible;
        }
        else
        {
            var actualWidth = _sideContentPresenter.ActualWidth;
            if (actualWidth > 0) _lastSideContentColumnWidth = actualWidth;

            _sideContentColumn.Width = new GridLength(0);
            _splitter.Visibility = Visibility.Collapsed;
        }

        _toolArrowPath.Data = HorizontalExpandDirectionEnum switch
        {
            HorizontalExpandDirectionEnum.Left => IsExpanded ? LeftArrowGeometry : RightArrowGeometry,
            HorizontalExpandDirectionEnum.Right => IsExpanded ? RightArrowGeometry : LeftArrowGeometry,
            _ => ThrowHelper.ThrowArgumentOutOfRangeException<Geometry>(nameof(HorizontalExpandDirectionEnum))
        };
        _toolBorder.CornerRadius = HorizontalExpandDirectionEnum switch
        {
            HorizontalExpandDirectionEnum.Left => IsExpanded ? new CornerRadius(0, PanelBorderCornerRadius.TopRight, PanelBorderCornerRadius.BottomRight, 0) : PanelBorderCornerRadius,
            HorizontalExpandDirectionEnum.Right => IsExpanded ? new CornerRadius(PanelBorderCornerRadius.TopLeft, 0, 0, PanelBorderCornerRadius.BottomLeft) : PanelBorderCornerRadius,
            _ => ThrowHelper.ThrowArgumentOutOfRangeException<CornerRadius>(nameof(HorizontalExpandDirectionEnum))
        };
    }
}