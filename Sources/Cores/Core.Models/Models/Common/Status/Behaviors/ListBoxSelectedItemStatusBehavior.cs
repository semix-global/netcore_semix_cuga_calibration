using Core.Models.Models.Common.Status.Interfaces;
using Microsoft.Xaml.Behaviors;
using System.ComponentModel;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using CommunityToolkit.Diagnostics;

namespace Core.Models.Models.Common.Status.Behaviors;

public class ListBoxSelectedItemStatusBehavior<TStatus, TSelectedItem> : Behavior<ListBox> where TStatus : IStatus<TSelectedItem>
{
    public static readonly DependencyProperty BindableSelectedItemProperty = DependencyProperty.Register(
        nameof(BindableSelectedItem),
        typeof(TSelectedItem),
        typeof(ListBoxSelectedItemStatusBehavior<TStatus, TSelectedItem>),
        new FrameworkPropertyMetadata(default(TSelectedItem), FrameworkPropertyMetadataOptions.BindsTwoWayByDefault, BindableSelectedItemsPropertyChangedCallback)
    );

    private static void BindableSelectedItemsPropertyChangedCallback(DependencyObject? d, DependencyPropertyChangedEventArgs e)
    {
        var behavior = Guard.IsNotNullAndAssignableToTypeAndReturn<ListBoxSelectedItemStatusBehavior<TStatus, TSelectedItem>>(d);

        if (e.NewValue is null) return;

        behavior.OnItemsSourceChanged(behavior.AssociatedObject, EventArgs.Empty);
    }

    public TSelectedItem BindableSelectedItem
    {
        get => (TSelectedItem)GetValue(BindableSelectedItemProperty);
        set => SetValue(BindableSelectedItemProperty, value);
    }

    private bool _isUpdatingSelection;

    protected override void OnAttached()
    {
        base.OnAttached();

        AssociatedObject.SelectionChanged -= OnSelectionChanged;
        AssociatedObject.SelectionChanged += OnSelectionChanged;

        var itemsSourceDescriptor = DependencyPropertyDescriptor.FromProperty(ItemsControl.ItemsSourceProperty, typeof(ListBox));
        Guard.IsNotNullAndReturn(itemsSourceDescriptor).AddValueChanged(AssociatedObject, OnItemsSourceChanged);
    }

    protected override void OnDetaching()
    {
        base.OnDetaching();

        AssociatedObject.SelectionChanged -= OnSelectionChanged;

        var itemsSourceDescriptor = DependencyPropertyDescriptor.FromProperty(ItemsControl.ItemsSourceProperty, typeof(ListBox));
        Guard.IsNotNullAndReturn(itemsSourceDescriptor).RemoveValueChanged(AssociatedObject, OnItemsSourceChanged);
    }

    private void OnItemsSourceChanged(object? sender, EventArgs e)
    {
        _isUpdatingSelection = true;
        try
        {
            AssociatedObject.SelectedItem = null;

            if (AssociatedObject.ItemsSource is null) return;

            AssociatedObject.SelectedItem = AssociatedObject.ItemsSource
                .Cast<TStatus>()
                .SingleOrDefault(t => Equals(BindableSelectedItem, t.SelectedItem));
        }
        finally
        {
            _isUpdatingSelection = false;
        }
    }

    private void OnSelectionChanged(object? sender, SelectionChangedEventArgs e)
    {
        if (_isUpdatingSelection) return;

        var listBox = Guard.IsNotNullAndAssignableToTypeAndReturn<ListBox>(sender);

        if (listBox.SelectedItem is null) return;
        BindableSelectedItem = Guard.IsAssignableToTypeAndReturn<TStatus>(listBox.SelectedItem).SelectedItem;

        Guard.IsNotNullAndReturn(BindingOperations.GetBindingExpression(this, BindableSelectedItemProperty)).UpdateSource();
    }
}