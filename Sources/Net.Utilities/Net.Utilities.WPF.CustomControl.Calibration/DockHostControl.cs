using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;

namespace Net.Utilities.WPF.CustomControl.Calibration;

public enum DockSide
{
    Left,
    Right
}

public class DockHostControl : Control
{
    static DockHostControl()
    {
        DefaultStyleKeyProperty.OverrideMetadata(
            typeof(DockHostControl),
            new FrameworkPropertyMetadata(typeof(DockHostControl)));
    }

    private ColumnDefinition? _sideColumn;

    private ColumnDefinition? _toggleColumn;

    private GridSplitter? _splitter;

    private TextBlock? _arrowText;

    private ToggleButton? _toggleButton;

    private double _lastWidth = 300;

    private double _toggleLastWidth = 28;

    #region SideContent

    public static readonly DependencyProperty SideContentProperty = DependencyProperty.Register(
        nameof(SideContent),
        typeof(object),
        typeof(DockHostControl),
        new PropertyMetadata(null));

    public object? SideContent
    {
        get => GetValue(SideContentProperty);
        set => SetValue(SideContentProperty, value);
    }

    #endregion

    #region MainContent

    public static readonly DependencyProperty MainContentProperty = DependencyProperty.Register(
        nameof(MainContent),
        typeof(object),
        typeof(DockHostControl),
        new PropertyMetadata(null));

    public object? MainContent
    {
        get => GetValue(MainContentProperty);
        set => SetValue(MainContentProperty, value);
    }

    #endregion

    #region Side

    public static readonly DependencyProperty SideProperty = DependencyProperty.Register(
        nameof(Side),
        typeof(DockSide),
        typeof(DockHostControl),
        new PropertyMetadata(DockSide.Left, OnSideChanged));

    public DockSide Side
    {
        get => (DockSide)GetValue(SideProperty);
        set => SetValue(SideProperty, value);
    }

    #endregion

    #region IsExpanded

    public static readonly DependencyProperty IsExpandedProperty = DependencyProperty.Register(
        nameof(IsExpanded),
        typeof(bool),
        typeof(DockHostControl),
        new PropertyMetadata(true, OnExpandedChanged));

    public bool IsExpanded
    {
        get => (bool)GetValue(IsExpandedProperty);
        set => SetValue(IsExpandedProperty, value);
    }

    #endregion

    public static readonly DependencyProperty SideContentVisibilityProperty = DependencyProperty.Register(
        nameof(SideContentVisibility),
        typeof(Visibility),
        typeof(DockHostControl),
        new PropertyMetadata(Visibility.Visible, OnSideContentVisibilityChanged));

    public Visibility SideContentVisibility
    {
        get => (Visibility)GetValue(SideContentVisibilityProperty);
        set => SetValue(SideContentVisibilityProperty, value);
    }

    public override void OnApplyTemplate()
    {
        base.OnApplyTemplate();

        if (_toggleButton != null)
        {
            _toggleButton.Click -= OnToggleButtonClick;
        }

        _sideColumn = GetTemplateChild("PART_SideColumn") as ColumnDefinition;

        _toggleColumn = GetTemplateChild("PART_ToggleColumn") as ColumnDefinition;

        _splitter = GetTemplateChild("PART_Splitter") as GridSplitter;

        _arrowText = GetTemplateChild("PART_ArrowText") as TextBlock;

        _toggleButton = GetTemplateChild("PART_Toggle") as ToggleButton;

        if (_toggleButton != null)
        {
            _toggleButton.Click += OnToggleButtonClick;
        }

        UpdateFlowDirection();
        UpdateLayoutState();
    }

    private static void OnExpandedChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        if (d is DockHostControl control)
        {
            control.UpdateLayoutState();
        }
    }

    private static void OnSideChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        if (d is not DockHostControl control) return;
        control.UpdateFlowDirection();
        control.UpdateArrow();
    }

    private static void OnSideContentVisibilityChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        if (d is DockHostControl control)
        {
            control.UpdateLayoutState();
        }
    }

    private void UpdateFlowDirection()
    {
        FlowDirection = Side == DockSide.Right
            ? FlowDirection.RightToLeft
            : FlowDirection.LeftToRight;
    }

    private void OnToggleButtonClick(object sender, RoutedEventArgs e)
    {
        SetCurrentValue(IsExpandedProperty, !IsExpanded);
    }

    private void UpdateLayoutState()
    {
        if (_sideColumn == null) return;

        if (IsExpanded)
        {
            _sideColumn.Width =
                new GridLength(_lastWidth);

            _splitter?.Visibility = Visibility.Visible;
        }
        else
        {
            if (_sideColumn.ActualWidth > 0)
            {
                _lastWidth = _sideColumn.ActualWidth;
            }

            _sideColumn.Width = new GridLength(0);

            _splitter?.Visibility = Visibility.Collapsed;
        }

        _toggleColumn?.Width = SideContentVisibility != Visibility.Visible ? new GridLength(0) : new GridLength(_toggleLastWidth);

        _toggleButton?.IsChecked = IsExpanded;

        UpdateArrow();
    }

    private void UpdateArrow()
    {
        _arrowText?.Text = Side switch
        {
            DockSide.Left => IsExpanded ? "◀" : "▶",
            DockSide.Right => IsExpanded ? "▶" : "◀",
            _ => "◀"
        };
    }
}