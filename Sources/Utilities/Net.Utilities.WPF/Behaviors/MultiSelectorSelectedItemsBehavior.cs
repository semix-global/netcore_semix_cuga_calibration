using Microsoft.Xaml.Behaviors;
using System.Collections;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;

namespace Net.Utilities.WPF.Behaviors;

public sealed class MultiSelectorSelectedItemsBehavior : Behavior<MultiSelector>
{
    #region Bindable SelectedItems

    public required IList BindableSelectedItems
    {
        get => (IList)GetValue(BindableSelectedItemsProperty);
        set => SetValue(BindableSelectedItemsProperty, value);
    }

    public static readonly DependencyProperty BindableSelectedItemsProperty =
        DependencyProperty.Register(
            nameof(BindableSelectedItems),
            typeof(IList),
            typeof(MultiSelectorSelectedItemsBehavior),
            new FrameworkPropertyMetadata(null, FrameworkPropertyMetadataOptions.BindsTwoWayByDefault)
        );

    #endregion Bindable SelectedItems

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
        var selector = (MultiSelector)sender;
        BindableSelectedItems = selector.SelectedItems;
        selector.GetBindingExpression(BindableSelectedItemsProperty)?.UpdateSource();
    }
}