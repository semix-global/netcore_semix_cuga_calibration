using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Input;
using System.Windows.Media.Animation;

namespace Net.Utilities.WPF.CustomControl.Calibration;

/// <summary>
/// Specifies the side of the main content area at which the side panel is docked.
/// </summary>
public enum DockSide
{
    Left,
    Right,
    Top,
    Bottom
}

/// <summary>
/// A dock host control that arranges a collapsible side panel next to a main content area.
/// Supports all four sides, animated expand/collapse, routed state change events,
/// built-in routed commands, and splitter-based resizing with size persistence.
/// </summary>
public class DockHostControl : Control
{
    static DockHostControl()
    {
        DefaultStyleKeyProperty.OverrideMetadata(
            typeof(DockHostControl),
            new FrameworkPropertyMetadata(typeof(DockHostControl)));
    }

    public DockHostControl()
    {
        InitializeCommands();
    }

    private const double DefaultSideLength = 300.0;
    private const double ToggleLength = 28.0;
    private const double SplitterLength = 5.0;
    private const double LengthTolerance = 0.5;

    private Grid? _layoutRoot;
    private Border? _sideHost;
    private ContentPresenter? _sideContentPresenter;
    private ContentPresenter? _mainContentPresenter;
    private GridSplitter? _splitter;
    private ToggleButton? _toggleButton;
    private TextBlock? _arrowText;

    private double _lastSideLength = DefaultSideLength;
    private Storyboard? _activeStoryboard;
    private bool _isFirstArrange = true;

    #region Commands

    public static RoutedCommand ExpandCommand { get; } = new(nameof(ExpandCommand), typeof(DockHostControl));
    public static RoutedCommand CollapseCommand { get; } = new(nameof(CollapseCommand), typeof(DockHostControl));
    public static RoutedCommand ToggleCommand { get; } = new(nameof(ToggleCommand), typeof(DockHostControl));

    private void InitializeCommands()
    {
        CommandBindings.Add(new CommandBinding(ExpandCommand, OnExpandCommandExecuted, OnExpandCommandCanExecute));
        CommandBindings.Add(new CommandBinding(CollapseCommand, OnCollapseCommandExecuted, OnCollapseCommandCanExecute));
        CommandBindings.Add(new CommandBinding(ToggleCommand, OnToggleCommandExecuted, OnToggleCommandCanExecute));
    }

    private void OnExpandCommandExecuted(object sender, ExecutedRoutedEventArgs e) => IsExpanded = true;

    private void OnExpandCommandCanExecute(object sender, CanExecuteRoutedEventArgs e)
    {
        e.CanExecute = CanToggle && !IsExpanded;
    }

    private void OnCollapseCommandExecuted(object sender, ExecutedRoutedEventArgs e) => IsExpanded = false;

    private void OnCollapseCommandCanExecute(object sender, CanExecuteRoutedEventArgs e)
    {
        e.CanExecute = CanToggle && IsExpanded;
    }

    private void OnToggleCommandExecuted(object sender, ExecutedRoutedEventArgs e) => IsExpanded = !IsExpanded;

    private void OnToggleCommandCanExecute(object sender, CanExecuteRoutedEventArgs e)
    {
        e.CanExecute = CanToggle;
    }

    #endregion

    #region Routed Events

    public static readonly RoutedEvent ExpandedEvent = EventManager.RegisterRoutedEvent(
        nameof(Expanded),
        RoutingStrategy.Bubble,
        typeof(RoutedEventHandler),
        typeof(DockHostControl));

    public static readonly RoutedEvent CollapsedEvent = EventManager.RegisterRoutedEvent(
        nameof(Collapsed),
        RoutingStrategy.Bubble,
        typeof(RoutedEventHandler),
        typeof(DockHostControl));

    public event RoutedEventHandler Expanded
    {
        add => AddHandler(ExpandedEvent, value);
        remove => RemoveHandler(ExpandedEvent, value);
    }

    public event RoutedEventHandler Collapsed
    {
        add => AddHandler(CollapsedEvent, value);
        remove => RemoveHandler(CollapsedEvent, value);
    }

    #endregion

    #region Dependency Properties

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

    public static readonly DependencyProperty IsExpandedProperty = DependencyProperty.Register(
        nameof(IsExpanded),
        typeof(bool),
        typeof(DockHostControl),
        new PropertyMetadata(true, OnIsExpandedChanged));

    public bool IsExpanded
    {
        get => (bool)GetValue(IsExpandedProperty);
        set => SetValue(IsExpandedProperty, value);
    }

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

    public static readonly DependencyProperty ShowToggleButtonProperty = DependencyProperty.Register(
        nameof(ShowToggleButton),
        typeof(bool),
        typeof(DockHostControl),
        new PropertyMetadata(true, OnShowToggleButtonChanged));

    public bool ShowToggleButton
    {
        get => (bool)GetValue(ShowToggleButtonProperty);
        set => SetValue(ShowToggleButtonProperty, value);
    }

    public static readonly DependencyProperty MinSideLengthProperty = DependencyProperty.Register(
        nameof(MinSideLength),
        typeof(double),
        typeof(DockHostControl),
        new PropertyMetadata(50.0));

    public double MinSideLength
    {
        get => (double)GetValue(MinSideLengthProperty);
        set => SetValue(MinSideLengthProperty, value);
    }

    public static readonly DependencyProperty AnimationDurationProperty = DependencyProperty.Register(
        nameof(AnimationDuration),
        typeof(Duration),
        typeof(DockHostControl),
        new PropertyMetadata(new Duration(TimeSpan.FromMilliseconds(200.0))));

    public Duration AnimationDuration
    {
        get => (Duration)GetValue(AnimationDurationProperty);
        set => SetValue(AnimationDurationProperty, value);
    }

    public static readonly DependencyProperty CanToggleProperty = DependencyProperty.Register(
        nameof(CanToggle),
        typeof(bool),
        typeof(DockHostControl),
        new PropertyMetadata(true, OnCanToggleChanged));

    public bool CanToggle
    {
        get => (bool)GetValue(CanToggleProperty);
        set => SetValue(CanToggleProperty, value);
    }

    #endregion

    public override void OnApplyTemplate()
    {
        base.OnApplyTemplate();

        UnsubscribeEvents();

        _layoutRoot = GetTemplateChild("PART_LayoutRoot") as Grid;
        _sideHost = GetTemplateChild("PART_SideHost") as Border;
        _sideContentPresenter = GetTemplateChild("PART_SideContent") as ContentPresenter;
        _mainContentPresenter = GetTemplateChild("PART_MainContent") as ContentPresenter;
        _splitter = GetTemplateChild("PART_Splitter") as GridSplitter;
        _toggleButton = GetTemplateChild("PART_Toggle") as ToggleButton;
        _arrowText = GetTemplateChild("PART_ArrowText") as TextBlock;

        SubscribeEvents();

        _isFirstArrange = true;
        ArrangeLayout();
        UpdateVisualState();
    }

    private void UnsubscribeEvents()
    {
        if (_toggleButton != null)
        {
            _toggleButton.Click -= OnToggleButtonClick;
        }

        if (_splitter != null)
        {
            _splitter.DragCompleted -= OnSplitterDragCompleted;
        }
    }

    private void SubscribeEvents()
    {
        if (_toggleButton != null)
        {
            _toggleButton.Click += OnToggleButtonClick;
        }

        if (_splitter != null)
        {
            _splitter.DragCompleted += OnSplitterDragCompleted;
        }
    }

    private static void OnSideChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        if (d is DockHostControl control)
        {
            control.ArrangeLayout();
            control.UpdateVisualState();
        }
    }

    private static void OnIsExpandedChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        if (d is not DockHostControl control) return;
        control.UpdateVisualState();
        control.RaiseStateChanged((bool)e.NewValue);
    }

    private static void OnSideContentVisibilityChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        if (d is DockHostControl control)
        {
            control.UpdateVisualState();
        }
    }

    private static void OnShowToggleButtonChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        if (d is DockHostControl control)
        {
            control.UpdateVisualState();
        }
    }

    private static void OnCanToggleChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        if (d is not DockHostControl control) return;
        control.UpdateVisualState();
        CommandManager.InvalidateRequerySuggested();
    }

    private void OnToggleButtonClick(object sender, RoutedEventArgs e)
    {
        if (CanToggle)
        {
            SetCurrentValue(IsExpandedProperty, !IsExpanded);
        }
    }

    private void OnSplitterDragCompleted(object sender, DragCompletedEventArgs e)
    {
        if (IsExpanded)
        {
            var current = GetCurrentSideLength();
            if (current > 0.0)
            {
                _lastSideLength = Math.Max(current, MinSideLength);
            }
        }
    }

    private void RaiseStateChanged(bool expanded)
    {
        var routedEvent = expanded ? ExpandedEvent : CollapsedEvent;
        RaiseEvent(new RoutedEventArgs(routedEvent, this));
        CommandManager.InvalidateRequerySuggested();
    }

    private bool IsHorizontalSide => Side is DockSide.Left or DockSide.Right;

    private double ExpandedSideLength => Math.Max(_lastSideLength, MinSideLength);

    private void ArrangeLayout()
    {
        _activeStoryboard?.Stop();
        _activeStoryboard = null;

        if (_layoutRoot == null) return;

        _layoutRoot.Children.Clear();
        _layoutRoot.RowDefinitions.Clear();
        _layoutRoot.ColumnDefinitions.Clear();

        if (IsHorizontalSide)
        {
            BuildHorizontalLayout();
        }
        else
        {
            BuildVerticalLayout();
        }

        if (_toggleButton != null)
        {
            _layoutRoot.Children.Add(_toggleButton);
        }

        if (_sideHost != null)
        {
            _layoutRoot.Children.Add(_sideHost);
        }

        if (_splitter != null)
        {
            _layoutRoot.Children.Add(_splitter);
        }

        if (_mainContentPresenter != null)
        {
            _layoutRoot.Children.Add(_mainContentPresenter);
        }
    }

    private void BuildHorizontalLayout()
    {
        var isRight = Side == DockSide.Right;

        _layoutRoot!.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(ToggleLength) });
        _layoutRoot.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(ExpandedSideLength), MinWidth = MinSideLength });
        _layoutRoot.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(SplitterLength) });
        _layoutRoot.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1.0, GridUnitType.Star) });

        if (isRight)
        {
            SetGridColumn(_mainContentPresenter, 0);
            SetGridColumn(_splitter, 1);
            SetGridColumn(_sideHost, 2);
            SetGridColumn(_toggleButton, 3);
        }
        else
        {
            SetGridColumn(_toggleButton, 0);
            SetGridColumn(_sideHost, 1);
            SetGridColumn(_splitter, 2);
            SetGridColumn(_mainContentPresenter, 3);
        }

        ClearGridRow(_toggleButton, _sideHost, _splitter, _mainContentPresenter);
        ConfigureSplitterForColumns();
    }

    private void BuildVerticalLayout()
    {
        var isBottom = Side == DockSide.Bottom;

        _layoutRoot!.RowDefinitions.Add(new RowDefinition { Height = new GridLength(ToggleLength) });
        _layoutRoot.RowDefinitions.Add(new RowDefinition { Height = new GridLength(ExpandedSideLength), MinHeight = MinSideLength });
        _layoutRoot.RowDefinitions.Add(new RowDefinition { Height = new GridLength(SplitterLength) });
        _layoutRoot.RowDefinitions.Add(new RowDefinition { Height = new GridLength(1.0, GridUnitType.Star) });

        if (isBottom)
        {
            SetGridRow(_mainContentPresenter, 0);
            SetGridRow(_splitter, 1);
            SetGridRow(_sideHost, 2);
            SetGridRow(_toggleButton, 3);
        }
        else
        {
            SetGridRow(_toggleButton, 0);
            SetGridRow(_sideHost, 1);
            SetGridRow(_splitter, 2);
            SetGridRow(_mainContentPresenter, 3);
        }

        ClearGridColumn(_toggleButton, _sideHost, _splitter, _mainContentPresenter);
        ConfigureSplitterForRows();
    }

    private static void SetGridColumn(UIElement? element, int column)
    {
        if (element != null)
        {
            Grid.SetColumn(element, column);
        }
    }

    private static void SetGridRow(UIElement? element, int row)
    {
        if (element != null)
        {
            Grid.SetRow(element, row);
        }
    }

    private static void ClearGridColumn(params UIElement?[] elements)
    {
        foreach (var element in elements)
        {
            if (element != null)
            {
                Grid.SetColumn(element, 0);
            }
        }
    }

    private static void ClearGridRow(params UIElement?[] elements)
    {
        foreach (var element in elements)
        {
            if (element != null)
            {
                Grid.SetRow(element, 0);
            }
        }
    }

    private void ConfigureSplitterForColumns()
    {
        if (_splitter == null) return;
        _splitter.Width = SplitterLength;
        _splitter.Height = double.NaN;
        _splitter.ResizeDirection = GridResizeDirection.Columns;
        _splitter.ResizeBehavior = GridResizeBehavior.PreviousAndNext;
    }

    private void ConfigureSplitterForRows()
    {
        if (_splitter == null) return;
        _splitter.Width = double.NaN;
        _splitter.Height = SplitterLength;
        _splitter.ResizeDirection = GridResizeDirection.Rows;
        _splitter.ResizeBehavior = GridResizeBehavior.PreviousAndNext;
    }

    private void UpdateVisualState()
    {
        if (_layoutRoot == null) return;

        var isVisible = SideContentVisibility == Visibility.Visible;

        if (_toggleButton != null)
        {
            _toggleButton.Visibility = isVisible && ShowToggleButton ? Visibility.Visible : Visibility.Collapsed;
            _toggleButton.IsChecked = IsExpanded;
            _toggleButton.IsEnabled = CanToggle;
        }

        if (_splitter != null)
        {
            _splitter.Visibility = isVisible && IsExpanded ? Visibility.Visible : Visibility.Collapsed;
        }

        if (_sideHost != null)
        {
            _sideHost.Visibility = isVisible ? Visibility.Visible : Visibility.Collapsed;
        }

        if (!isVisible)
        {
            SetSideLength(0.0);
            UpdateArrow();
            return;
        }

        if (!IsExpanded)
        {
            var current = GetCurrentSideLength();
            if (current > 0.0)
            {
                _lastSideLength = Math.Max(current, MinSideLength);
            }
        }

        if (_isFirstArrange)
        {
            SetSideLength(IsExpanded ? ExpandedSideLength : 0.0);
            _isFirstArrange = false;
        }
        else
        {
            AnimateSideLength(IsExpanded);
        }

        UpdateArrow();
    }

    private void AnimateSideLength(bool expanded)
    {
        var definition = GetSideDefinition();
        if (definition == null) return;

        var currentLength = GetDefinitionLength(definition);
        var targetLength = Math.Max(expanded ? ExpandedSideLength : 0.0, 0.0);

        // Capture the current animated value into the base value before stopping the old animation
        // so that there is no one-frame revert when swapping storyboards.
        SetDefinitionLength(definition, new GridLength(currentLength, GridUnitType.Pixel));

        _activeStoryboard?.Stop();
        _activeStoryboard = null;

        if (!IsVisible || !AnimationDuration.HasTimeSpan)
        {
            SetDefinitionLength(definition, new GridLength(targetLength));
            return;
        }

        var duration = AnimationDuration.TimeSpan;
        if (duration <= TimeSpan.Zero || Math.Abs(currentLength - targetLength) < LengthTolerance)
        {
            SetDefinitionLength(definition, new GridLength(targetLength));
            return;
        }

        var animation = new GridLengthAnimation
        {
            From = new GridLength(currentLength, GridUnitType.Pixel),
            To = new GridLength(targetLength, GridUnitType.Pixel),
            Duration = new Duration(duration),
            EasingFunction = new CubicEase { EasingMode = EasingMode.EaseInOut },
            FillBehavior = FillBehavior.HoldEnd
        };

        var storyboard = new Storyboard();
        storyboard.Children.Add(animation);
        Storyboard.SetTarget(animation, definition);
        Storyboard.SetTargetProperty(animation, new PropertyPath(GetSideLengthProperty()));
        storyboard.Completed += OnAnimationCompleted;
        storyboard.Begin();
        _activeStoryboard = storyboard;
    }

    private void OnAnimationCompleted(object? sender, EventArgs e)
    {
        if (_activeStoryboard == sender)
        {
            _activeStoryboard = null;
        }
    }

    private void SetSideLength(double length)
    {
        var definition = GetSideDefinition();
        if (definition == null) return;
        SetDefinitionLength(definition, new GridLength(Math.Max(length, 0.0)));
    }

    private DependencyObject? GetSideDefinition()
    {
        if (_layoutRoot == null) return null;

        return IsHorizontalSide
            ? _layoutRoot.ColumnDefinitions.Count > 1 ? _layoutRoot.ColumnDefinitions[1] : null
            : _layoutRoot.RowDefinitions.Count > 1 ? _layoutRoot.RowDefinitions[1] : null;
    }

    private double GetDefinitionLength(DependencyObject definition)
    {
        return definition switch
        {
            ColumnDefinition col => col.Width.IsAbsolute ? col.Width.Value : col.ActualWidth,
            RowDefinition row => row.Height.IsAbsolute ? row.Height.Value : row.ActualHeight,
            _ => 0.0
        };
    }

    private void SetDefinitionLength(DependencyObject definition, GridLength length)
    {
        switch (definition)
        {
            case ColumnDefinition col:
                col.Width = length;
                break;
            case RowDefinition row:
                row.Height = length;
                break;
        }
    }

    private DependencyProperty GetSideLengthProperty()
    {
        return IsHorizontalSide ? ColumnDefinition.WidthProperty : RowDefinition.HeightProperty;
    }

    private double GetCurrentSideLength()
    {
        if (_sideHost == null) return _lastSideLength;

        return IsHorizontalSide ? _sideHost.ActualWidth : _sideHost.ActualHeight;
    }

    private void UpdateArrow()
    {
        if (_arrowText == null) return;

        _arrowText.Text = Side switch
        {
            DockSide.Left => IsExpanded ? "◀" : "▶",
            DockSide.Right => IsExpanded ? "▶" : "◀",
            DockSide.Top => IsExpanded ? "▲" : "▼",
            DockSide.Bottom => IsExpanded ? "▼" : "▲",
            _ => "◀"
        };
    }
}
