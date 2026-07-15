using System.Collections;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;

namespace Core.Models.Models.Common.Cookies.UI;

public sealed partial class CalibrationTreeView : UserControl
{
    public static readonly DependencyProperty ItemsSourceProperty = DependencyProperty.Register(
        nameof(ItemsSource),
        typeof(IEnumerable),
        typeof(CalibrationTreeView),
        new PropertyMetadata(null));

    public static readonly DependencyProperty CheckBoxVisibilityProperty = DependencyProperty.Register(
        nameof(CheckBoxVisibility),
        typeof(Visibility),
        typeof(CalibrationTreeView),
        new PropertyMetadata(Visibility.Visible));

    public static readonly DependencyProperty CheckBoxToolTipProperty = DependencyProperty.Register(
        nameof(CheckBoxToolTip),
        typeof(object),
        typeof(CalibrationTreeView),
        new PropertyMetadata(null));

    public static readonly DependencyProperty CheckBoxStyleProperty = DependencyProperty.Register(
        nameof(CheckBoxStyle),
        typeof(Style),
        typeof(CalibrationTreeView),
        new PropertyMetadata(null));

    public static readonly DependencyProperty MouseDoubleClickCommandProperty = DependencyProperty.Register(
        nameof(MouseDoubleClickCommand),
        typeof(ICommand),
        typeof(CalibrationTreeView),
        new PropertyMetadata(null));

    public static readonly DependencyProperty DetailCommandProperty = DependencyProperty.Register(
        nameof(DetailCommand),
        typeof(ICommand),
        typeof(CalibrationTreeView),
        new PropertyMetadata(null));

    public static readonly DependencyProperty DetailButtonVisibilityProperty = DependencyProperty.Register(
        nameof(DetailButtonVisibility),
        typeof(Visibility),
        typeof(CalibrationTreeView),
        new PropertyMetadata(Visibility.Collapsed));

    public static readonly DependencyProperty DetailButtonStyleProperty = DependencyProperty.Register(
        nameof(DetailButtonStyle),
        typeof(Style),
        typeof(CalibrationTreeView),
        new PropertyMetadata(null));

    public static readonly DependencyProperty DetailButtonIconGeometryProperty = DependencyProperty.Register(
        nameof(DetailButtonIconGeometry),
        typeof(Geometry),
        typeof(CalibrationTreeView),
        new PropertyMetadata(null));

    public IEnumerable? ItemsSource
    {
        get => (IEnumerable?)GetValue(ItemsSourceProperty);
        set => SetValue(ItemsSourceProperty, value);
    }

    public Visibility CheckBoxVisibility
    {
        get => (Visibility)GetValue(CheckBoxVisibilityProperty);
        set => SetValue(CheckBoxVisibilityProperty, value);
    }

    public object? CheckBoxToolTip
    {
        get => GetValue(CheckBoxToolTipProperty);
        set => SetValue(CheckBoxToolTipProperty, value);
    }

    public Style? CheckBoxStyle
    {
        get => (Style?)GetValue(CheckBoxStyleProperty);
        set => SetValue(CheckBoxStyleProperty, value);
    }

    public ICommand? MouseDoubleClickCommand
    {
        get => (ICommand?)GetValue(MouseDoubleClickCommandProperty);
        set => SetValue(MouseDoubleClickCommandProperty, value);
    }

    public ICommand? DetailCommand
    {
        get => (ICommand?)GetValue(DetailCommandProperty);
        set => SetValue(DetailCommandProperty, value);
    }

    public Visibility DetailButtonVisibility
    {
        get => (Visibility)GetValue(DetailButtonVisibilityProperty);
        set => SetValue(DetailButtonVisibilityProperty, value);
    }

    public Style? DetailButtonStyle
    {
        get => (Style?)GetValue(DetailButtonStyleProperty);
        set => SetValue(DetailButtonStyleProperty, value);
    }

    public Geometry? DetailButtonIconGeometry
    {
        get => (Geometry?)GetValue(DetailButtonIconGeometryProperty);
        set => SetValue(DetailButtonIconGeometryProperty, value);
    }

    public CalibrationTreeView()
    {
        InitializeComponent();
    }
}
