using Microsoft.Xaml.Behaviors;
using System.Windows;
using System.Windows.Controls;

namespace Net.Utilities.WPF.Behaviors;

public sealed class BindableSelectedItemBehavior : Behavior<TreeView>
{
    #region SelectedItem Property

    public object? SelectedItem
    {
        get => (object?)GetValue(SelectedItemProperty);
        set => SetValue(SelectedItemProperty, value);
    }

    public static readonly DependencyProperty SelectedItemProperty =
        DependencyProperty.Register(nameof(SelectedItem), typeof(object), typeof(BindableSelectedItemBehavior), new PropertyMetadata(null));

    #endregion SelectedItem Property

    protected override void OnAttached()
    {
        base.OnAttached();

        AssociatedObject.SelectedItemChanged -= OnTreeViewSelectedItemChanged;
        AssociatedObject.SelectedItemChanged += OnTreeViewSelectedItemChanged;
    }

    protected override void OnDetaching()
    {
        base.OnDetaching();

        AssociatedObject.SelectedItemChanged -= OnTreeViewSelectedItemChanged;
    }

    private void OnTreeViewSelectedItemChanged(object sender, RoutedPropertyChangedEventArgs<object> e)
    {
        SelectedItem = e.NewValue;
    }
}