using Microsoft.Xaml.Behaviors;
using System.Collections;
using System.Windows;
using System.Windows.Controls;

namespace Net.Utilities.WPF.Behaviors;

public sealed class MultiSelectorListViewSelectedItemsBehavior : Behavior<ListView>
{
    public IList BindableSelectedItems
    {
        get => (IList)GetValue(BindableSelectedItemsProperty);
        set => SetValue(BindableSelectedItemsProperty, value);
    }

    public static readonly DependencyProperty BindableSelectedItemsProperty =
        DependencyProperty.Register("BindableSelectedItems", typeof(IList), typeof(MultiSelectorListViewSelectedItemsBehavior), new PropertyMetadata(null));

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
        if (BindableSelectedItems == null) return;

        BindableSelectedItems.Clear();

        foreach (var item in AssociatedObject.SelectedItems)
        {
            BindableSelectedItems.Add(item);
        }
    }
}