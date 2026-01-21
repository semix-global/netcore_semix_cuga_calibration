using Core.Models.Models.Common.Status.Interfaces;
using Microsoft.Xaml.Behaviors;
using Net.Utilities.Models;
using System.ComponentModel;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;

namespace Core.Models.Models.Common.Status.Behaviors;

public class ListBoxSelectedItemsStatusBehavior<TStatus, TSelectedItem> : Behavior<ListBox> where TStatus : IStatus<TSelectedItem>
{
    public static readonly DependencyProperty BindableSelectedItemsProperty = DependencyProperty.Register(
        nameof(BindableSelectedItems),
        typeof(IReadOnlyList<TSelectedItem>),
        typeof(ListBoxSelectedItemsStatusBehavior<TStatus, TSelectedItem>),
        new FrameworkPropertyMetadata(Array.Empty<TSelectedItem>(), FrameworkPropertyMetadataOptions.BindsTwoWayByDefault, BindableSelectedItemsPropertyChangedCallback)
    );

    private static void BindableSelectedItemsPropertyChangedCallback(DependencyObject? d, DependencyPropertyChangedEventArgs e)
    {
        var behavior = GuardUtils.IsNotNullAndAssignableToType<ListBoxSelectedItemsStatusBehavior<TStatus, TSelectedItem>>(d);

        if (e.NewValue is null) return;

        behavior.OnItemsSourceChanged(behavior.AssociatedObject, EventArgs.Empty);
    }

    public IReadOnlyList<TSelectedItem> BindableSelectedItems
    {
        get => (IReadOnlyList<TSelectedItem>)GetValue(BindableSelectedItemsProperty);
        set => SetValue(BindableSelectedItemsProperty, value);
    }

    private bool _isUpdatingSelection;

    protected override void OnAttached()
    {
        base.OnAttached();

        AssociatedObject.SelectionChanged -= OnSelectionChanged;
        AssociatedObject.SelectionChanged += OnSelectionChanged;

        var itemsSourceDescriptor = DependencyPropertyDescriptor.FromProperty(ItemsControl.ItemsSourceProperty, typeof(ListBox));
        GuardUtils.IsNotNullAndReturn(itemsSourceDescriptor).AddValueChanged(AssociatedObject, OnItemsSourceChanged);
    }

    protected override void OnDetaching()
    {
        base.OnDetaching();

        AssociatedObject.SelectionChanged -= OnSelectionChanged;

        var itemsSourceDescriptor = DependencyPropertyDescriptor.FromProperty(ItemsControl.ItemsSourceProperty, typeof(ListBox));
        GuardUtils.IsNotNullAndReturn(itemsSourceDescriptor).RemoveValueChanged(AssociatedObject, OnItemsSourceChanged);
    }

    private void OnItemsSourceChanged(object? sender, EventArgs e)
    {
        _isUpdatingSelection = true;
        try
        {
            AssociatedObject.SelectedItems.Clear();

            if (AssociatedObject.ItemsSource is null) return;

            foreach (var calibrationStatus in AssociatedObject.ItemsSource
                         .Cast<TStatus>()
                         .Where(t => BindableSelectedItems.Contains(t.SelectedItem)))
            {
                AssociatedObject.SelectedItems.Add(calibrationStatus);
            }
        }
        finally
        {
            _isUpdatingSelection = false;
        }
    }

    private void OnSelectionChanged(object? sender, SelectionChangedEventArgs e)
    {
        if (_isUpdatingSelection) return;

        var listBox = GuardUtils.IsNotNullAndAssignableToType<ListBox>(sender);

        BindableSelectedItems =
        [
            ..listBox.SelectedItems
                .Cast<TStatus>()
                .Select(t => t.SelectedItem)
        ];

        GuardUtils.IsNotNullAndReturn(BindingOperations.GetBindingExpression(this, BindableSelectedItemsProperty)).UpdateSource();
    }
}