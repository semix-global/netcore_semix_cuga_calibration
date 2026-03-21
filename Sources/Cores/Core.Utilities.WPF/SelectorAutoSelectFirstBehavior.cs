using CommunityToolkit.Diagnostics;
using Microsoft.Xaml.Behaviors;
using System.ComponentModel;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;

namespace Core.Utilities.WPF;

public sealed class SelectorAutoSelectFirstBehavior : Behavior<Selector>
{
    protected override void OnAttached()
    {
        base.OnAttached();

        AssociatedObject.Loaded -= OnTabControlLoaded;
        AssociatedObject.Loaded += OnTabControlLoaded;

        var itemsSourceDescriptor = DependencyPropertyDescriptor.FromProperty(ItemsControl.ItemsSourceProperty, typeof(Selector));
        Guard.IsNotNullAndReturn(itemsSourceDescriptor).AddValueChanged(AssociatedObject, OnItemsSourceChanged);
    }

    protected override void OnDetaching()
    {
        base.OnDetaching();

        AssociatedObject.Loaded -= OnTabControlLoaded;

        var itemsSourceDescriptor = DependencyPropertyDescriptor.FromProperty(ItemsControl.ItemsSourceProperty, typeof(Selector));
        Guard.IsNotNullAndReturn(itemsSourceDescriptor).RemoveValueChanged(AssociatedObject, OnItemsSourceChanged);
    }

    private void OnTabControlLoaded(object? sender, RoutedEventArgs e) => TrySelectFirstItem();

    private void OnItemsSourceChanged(object? sender, EventArgs e) => TrySelectFirstItem();

    private void TrySelectFirstItem()
    {
        AssociatedObject.SelectedIndex = 0;
    }
}