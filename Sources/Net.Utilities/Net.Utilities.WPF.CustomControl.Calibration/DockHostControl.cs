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

    public static readonly DependencyProperty ToolContentProperty = DependencyProperty.Register(
        nameof(ToolContent),
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

    public object? ToolContent
    {
        get => GetValue(ToolContentProperty);
        set => SetValue(ToolContentProperty, value);
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

    private static void OnChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        if (d is not DockHostControl control) return;

        control.Update();
    }

    private DrawingImage? _leftDrawingImage;
    private DrawingImage? _rightDrawingImage;

    private ColumnDefinition? _sideContentColumn;

    private Border? _toolBorder;
    private Image? _arrowImage;

    private ContentPresenter? _sideContentPresenter;
    private GridSplitter? _splitter;

    private double _lastSideContentColumnWidth;

    public override void OnApplyTemplate()
    {
        base.OnApplyTemplate();

        _leftDrawingImage = Guard.IsNotNullAndAssignableToTypeAndReturn<DrawingImage>(FindResource("LeftDrawingImage"));
        _rightDrawingImage = Guard.IsNotNullAndAssignableToTypeAndReturn<DrawingImage>(FindResource("RightDrawingImage"));

        _toolBorder?.MouseDown -= ToolBorderOnMouseDown;
        _splitter?.DragCompleted -= SplitterDragOnDragCompleted;

        _sideContentColumn = Guard.IsNotNullAndAssignableToTypeAndReturn<ColumnDefinition>(GetTemplateChild("PART_SideContentColumn"));

        _toolBorder = Guard.IsNotNullAndAssignableToTypeAndReturn<Border>(GetTemplateChild("PART_ToolBorder"));
        _arrowImage = Guard.IsNotNullAndAssignableToTypeAndReturn<Image>(GetTemplateChild("PART_ArrowImage"));

        _sideContentPresenter = Guard.IsNotNullAndAssignableToTypeAndReturn<ContentPresenter>(GetTemplateChild("PART_SideContent"));
        _splitter = Guard.IsNotNullAndAssignableToTypeAndReturn<GridSplitter>(GetTemplateChild("PART_Splitter"));

        _toolBorder.MouseDown -= ToolBorderOnMouseDown;
        _toolBorder.MouseDown += ToolBorderOnMouseDown;
        _splitter.DragCompleted -= SplitterDragOnDragCompleted;
        _splitter.DragCompleted += SplitterDragOnDragCompleted;

        _lastSideContentColumnWidth = _sideContentColumn.Width.Value;

        Update();
    }

    private void ToolBorderOnMouseDown(object sender, MouseButtonEventArgs e) => SetCurrentValue(IsExpandedProperty, !IsExpanded);

    private void SplitterDragOnDragCompleted(object sender, DragCompletedEventArgs e)
    {
        if (IsExpanded == false || _sideContentPresenter is null) return;

        _lastSideContentColumnWidth = Math.Max(_sideContentPresenter.ActualWidth, 0d);
    }

    private void Update()
    {
        FlowDirection = DockSideEnum == DockSideEnum.Right ? FlowDirection.RightToLeft : FlowDirection.LeftToRight;

        if (_leftDrawingImage is null
            || _rightDrawingImage is null
            || _sideContentColumn is null
            || _toolBorder is null
            || _arrowImage is null
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

        _arrowImage.Source = DockSideEnum switch
        {
            DockSideEnum.Left => IsExpanded ? _leftDrawingImage : _rightDrawingImage,
            DockSideEnum.Right => IsExpanded ? _rightDrawingImage : _leftDrawingImage,
            _ => ThrowHelper.ThrowArgumentOutOfRangeException<DrawingImage>(nameof(DockSideEnum))
        };
        _toolBorder.BorderThickness = DockSideEnum switch
        {
            DockSideEnum.Left => IsExpanded ? new Thickness(1, 1, 0, 1) : new Thickness(1d),
            DockSideEnum.Right => IsExpanded ? new Thickness(0, 1, 1, 1) : new Thickness(1d),
            _ => ThrowHelper.ThrowArgumentOutOfRangeException<Thickness>(nameof(DockSideEnum))
        };
        _toolBorder.CornerRadius = DockSideEnum switch
        {
            DockSideEnum.Left => IsExpanded ? new CornerRadius(3, 0, 0, 3) : new CornerRadius(3d),
            DockSideEnum.Right => IsExpanded ? new CornerRadius(0, 3, 3, 0) : new CornerRadius(3d),
            _ => ThrowHelper.ThrowArgumentOutOfRangeException<CornerRadius>(nameof(DockSideEnum))
        };
    }
}