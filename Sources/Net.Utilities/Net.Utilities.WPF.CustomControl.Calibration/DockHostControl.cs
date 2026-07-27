using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Input;
using System.Windows.Media;
using CommunityToolkit.Diagnostics;

namespace Net.Utilities.WPF.CustomControl.Calibration;

public enum DockSideEnum
{
    Left,
    Right
}

public sealed class DockHostControl : Control
{
    static DockHostControl()
    {
        DefaultStyleKeyProperty.OverrideMetadata(typeof(DockHostControl), new FrameworkPropertyMetadata(typeof(DockHostControl)));
    }

    public static readonly DependencyProperty BorderCornerRadiusProperty = DependencyProperty.Register(
        nameof(BorderCornerRadius),
        typeof(CornerRadius),
        typeof(DockHostControl),
        new PropertyMetadata(default(CornerRadius)));
    
    public static readonly DependencyProperty MainContentProperty = DependencyProperty.Register(
        nameof(MainContent),
        typeof(object),
        typeof(DockHostControl),
        new PropertyMetadata(null));

    public static readonly DependencyProperty SideContentProperty = DependencyProperty.Register(
        nameof(SideContent),
        typeof(object),
        typeof(DockHostControl),
        new PropertyMetadata(null));

    public static readonly DependencyProperty DockSideEnumProperty = DependencyProperty.Register(
        nameof(DockSideEnum),
        typeof(DockSideEnum),
        typeof(DockHostControl),
        new PropertyMetadata(DockSideEnum.Left, OnChanged));

    public static readonly DependencyProperty IsExpandedProperty = DependencyProperty.Register(
        nameof(IsExpanded),
        typeof(bool),
        typeof(DockHostControl),
        new PropertyMetadata(true, OnChanged));

    public static readonly DependencyProperty SideContentVisibilityProperty = DependencyProperty.Register(
        nameof(SideContentVisibility),
        typeof(Visibility),
        typeof(DockHostControl),
        new PropertyMetadata(Visibility.Visible, OnChanged));

    private static readonly DependencyPropertyKey IconGeometryPropertyKey = DependencyProperty.RegisterReadOnly(
        nameof(IconGeometry),
        typeof(Geometry),
        typeof(DockHostControl),
        new PropertyMetadata(null));

    public static readonly DependencyProperty IconGeometryProperty = IconGeometryPropertyKey.DependencyProperty;

    public static readonly DependencyProperty BorderHoverBrushProperty = DependencyProperty.Register(
        nameof(BorderHoverBrush),
        typeof(Brush),
        typeof(DockHostControl),
        new PropertyMetadata(Brushes.Black));

    public CornerRadius BorderCornerRadius
    {
        get => (CornerRadius)GetValue(BorderCornerRadiusProperty);
        set => SetValue(BorderCornerRadiusProperty, value);
    }

    public object? MainContent
    {
        get => GetValue(MainContentProperty);
        set => SetValue(MainContentProperty, value);
    }

    public object? SideContent
    {
        get => GetValue(SideContentProperty);
        set => SetValue(SideContentProperty, value);
    }

    public DockSideEnum DockSideEnum
    {
        get => (DockSideEnum)GetValue(DockSideEnumProperty);
        set => SetValue(DockSideEnumProperty, value);
    }

    public bool IsExpanded
    {
        get => (bool)GetValue(IsExpandedProperty);
        set => SetValue(IsExpandedProperty, value);
    }

    public Visibility SideContentVisibility
    {
        get => (Visibility)GetValue(SideContentVisibilityProperty);
        set => SetValue(SideContentVisibilityProperty, value);
    }

    public Geometry IconGeometry
    {
        get => (Geometry)GetValue(IconGeometryProperty);
        private set => SetValue(IconGeometryPropertyKey, value);
    }

    public Brush BorderHoverBrush
    {
        get => (Brush)GetValue(BorderHoverBrushProperty);
        set => SetValue(BorderHoverBrushProperty, value);
    }

    private static void OnChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        if (d is not DockHostControl control) return;

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

    private ContentPresenter? _sideContentPresenter;
    private GridSplitter? _splitter;

    private double _lastSideContentColumnWidth;

    public override void OnApplyTemplate()
    {
        base.OnApplyTemplate();

        _toolBorder?.MouseDown -= ToolBorderOnMouseDown;

        _sideContentColumn = Guard.IsNotNullAndAssignableToTypeAndReturn<ColumnDefinition>(GetTemplateChild("PART_SideContentColumn"));

        _toolBorder = Guard.IsNotNullAndAssignableToTypeAndReturn<Border>(GetTemplateChild("PART_ToolBorder"));

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
        FlowDirection = DockSideEnum == DockSideEnum.Right ? FlowDirection.RightToLeft : FlowDirection.LeftToRight;

        if (_sideContentColumn is null
            || _toolBorder is null
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

        IconGeometry = DockSideEnum switch
        {
            DockSideEnum.Left => IsExpanded ? LeftArrowGeometry : RightArrowGeometry,
            DockSideEnum.Right => IsExpanded ? RightArrowGeometry : LeftArrowGeometry,
            _ => ThrowHelper.ThrowArgumentOutOfRangeException<Geometry>(nameof(DockSideEnum))
        };
        _toolBorder.BorderThickness = DockSideEnum switch
        {
            DockSideEnum.Left => IsExpanded ? new Thickness(0, BorderThickness.Top, BorderThickness.Right, BorderThickness.Bottom) : BorderThickness,
            DockSideEnum.Right => IsExpanded ? new Thickness(BorderThickness.Left, BorderThickness.Top, 0, BorderThickness.Bottom) : BorderThickness,
            _ => ThrowHelper.ThrowArgumentOutOfRangeException<Thickness>(nameof(DockSideEnum))
        };
        _toolBorder.CornerRadius = DockSideEnum switch
        {
            DockSideEnum.Left => IsExpanded ? new CornerRadius(0, BorderCornerRadius.TopRight, BorderCornerRadius.BottomRight, 0) : BorderCornerRadius,
            DockSideEnum.Right => IsExpanded ? new CornerRadius(BorderCornerRadius.TopLeft, 0, 0, BorderCornerRadius.BottomLeft) : BorderCornerRadius,
            _ => ThrowHelper.ThrowArgumentOutOfRangeException<CornerRadius>(nameof(DockSideEnum))
        };
    }
}