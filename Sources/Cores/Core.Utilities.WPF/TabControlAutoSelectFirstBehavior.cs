using System.Windows;
using System.Windows.Controls;
using Microsoft.Xaml.Behaviors;
using Net.Utilities.Models;

namespace Core.Utilities.WPF;

public sealed class TabControlAutoSelectFirstBehavior : Behavior<TabControl>
{
    protected override void OnAttached()
    {
        base.OnAttached();

        AssociatedObject.Loaded -= OnTabControlLoaded;
        AssociatedObject.Loaded += OnTabControlLoaded;

        var itemsSourceDescriptor = System.ComponentModel.DependencyPropertyDescriptor.FromProperty(ItemsControl.ItemsSourceProperty, typeof(TabControl));
        GuardUtils.IsNotNullAndReturn(itemsSourceDescriptor).AddValueChanged(AssociatedObject, OnItemsSourceChanged);
    }

    protected override void OnDetaching()
    {
        base.OnDetaching();

        AssociatedObject.Loaded -= OnTabControlLoaded;

        var itemsSourceDescriptor = System.ComponentModel.DependencyPropertyDescriptor.FromProperty(ItemsControl.ItemsSourceProperty, typeof(TabControl));
        GuardUtils.IsNotNullAndReturn(itemsSourceDescriptor).RemoveValueChanged(AssociatedObject, OnItemsSourceChanged);
    }

    private void OnTabControlLoaded(object sender, RoutedEventArgs e) => TrySelectFirstItem();

    private void OnItemsSourceChanged(object sender, EventArgs e) => TrySelectFirstItem();

    private void TrySelectFirstItem()
    {
        // ReSharper disable once GenericEnumeratorNotDisposed
        var enumerator = AssociatedObject.ItemsSource.GetEnumerator();
        if (enumerator.MoveNext()) AssociatedObject.SelectedIndex = 0;
    }
}