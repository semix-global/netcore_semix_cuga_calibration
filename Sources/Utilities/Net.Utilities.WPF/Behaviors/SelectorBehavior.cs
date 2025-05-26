using Microsoft.Xaml.Behaviors;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Data;

namespace Net.Utilities.WPF.Behaviors;

public sealed class SelectorBehavior : Behavior<Selector>
{
    #region 依赖属性

    public required object BindableSelectedItem
    {
        get => GetValue(BindableSelectedItemProperty);
        set => SetValue(BindableSelectedItemProperty, value);
    }

    public static readonly DependencyProperty BindableSelectedItemProperty =
        DependencyProperty.Register(
            nameof(BindableSelectedItem),
            typeof(object),
            typeof(SelectorBehavior),
            new FrameworkPropertyMetadata(null, FrameworkPropertyMetadataOptions.BindsTwoWayByDefault, OnPropertyChangedCallback)
        );

    private static void OnPropertyChangedCallback(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        if (d is not SelectorBehavior behaviors) return;

        var b = behaviors.AssociatedObject.ItemsSource.Cast<object>().Count(t => t == e.NewValue) == 1;
        behaviors.AssociatedObject.SelectedItem = b ? e.NewValue : null;
    }

    #endregion 依赖属性

    protected override void OnAttached()
    {
        base.OnAttached();

        AssociatedObject.SelectionChanged += OnSelectionChanged;
    }

    protected override void OnDetaching()
    {
        base.OnDetaching();

        AssociatedObject.SelectionChanged -= OnSelectionChanged;
    }

    private void OnSelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        var selector = (Selector)sender;
        BindableSelectedItem = selector.SelectedItem;
        BindingOperations.GetBindingExpression(this, BindableSelectedItemProperty)!.UpdateSource();
    }
}