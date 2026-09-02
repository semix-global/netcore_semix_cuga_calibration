using CommunityToolkit.Diagnostics;
using Microsoft.Xaml.Behaviors;
using System.ComponentModel;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;

namespace Net.Utilities.WPF.Behaviors.Calibration;

public sealed class ComboBoxSelectedItemBehavior : Behavior<ComboBox>
{
    public static readonly DependencyProperty BindableSelectedItemProperty = DependencyProperty.Register(
        nameof(BindableSelectedItem),
        typeof(object),
        typeof(ComboBoxSelectedItemBehavior),
        new FrameworkPropertyMetadata(null, FrameworkPropertyMetadataOptions.BindsTwoWayByDefault, BindableSelectedItemsPropertyChangedCallback)
    );

    public static readonly DependencyProperty FallbackSelectItemProperty = DependencyProperty.Register(
        nameof(FallbackSelectItem),
        typeof(object),
        typeof(ComboBoxSelectedItemBehavior),
        new PropertyMetadata(null));

    private static void BindableSelectedItemsPropertyChangedCallback(DependencyObject? d, DependencyPropertyChangedEventArgs e)
    {
        var behavior = Guard.IsNotNullAndAssignableToTypeAndReturn<ComboBoxSelectedItemBehavior>(d);

        if (e.NewValue is null) return;

        behavior.OnItemsSourceChanged(behavior.AssociatedObject, EventArgs.Empty);
    }

    public object? BindableSelectedItem
    {
        get => (object?)GetValue(BindableSelectedItemProperty);
        set => SetValue(BindableSelectedItemProperty, value);
    }

    public object? FallbackSelectItem
    {
        get => (object?)GetValue(FallbackSelectItemProperty);
        set => SetValue(FallbackSelectItemProperty, value);
    }

    private bool _isUpdatingSelection;

    protected override void OnAttached()
    {
        base.OnAttached();

        AssociatedObject.SelectionChanged -= OnSelectionChanged;
        AssociatedObject.SelectionChanged += OnSelectionChanged;

        var itemsSourceDescriptor = DependencyPropertyDescriptor.FromProperty(ItemsControl.ItemsSourceProperty, typeof(ComboBox));
        Guard.IsNotNullAndReturn(itemsSourceDescriptor).AddValueChanged(AssociatedObject, OnItemsSourceChanged);
    }

    protected override void OnDetaching()
    {
        base.OnDetaching();

        AssociatedObject.SelectionChanged -= OnSelectionChanged;

        var itemsSourceDescriptor = DependencyPropertyDescriptor.FromProperty(ItemsControl.ItemsSourceProperty, typeof(ComboBox));
        Guard.IsNotNullAndReturn(itemsSourceDescriptor).RemoveValueChanged(AssociatedObject, OnItemsSourceChanged);
    }

    private void OnItemsSourceChanged(object? sender, EventArgs e)
    {
        _isUpdatingSelection = true;
        try
        {
            AssociatedObject.SelectedItem = null;
            // AssociatedObject.Text = string.Empty;

            if (AssociatedObject.ItemsSource is null) return;

            foreach (var item in AssociatedObject.ItemsSource)
            {
                if (Equals(BindableSelectedItem, item) == false) continue;

                AssociatedObject.SelectedItem = item;

                return;
            }

            // AssociatedObject.Text = FallbackSelectItem?.ToString() ?? string.Empty;
        }
        finally
        {
            _isUpdatingSelection = false;
        }
    }

    private void OnSelectionChanged(object? sender, SelectionChangedEventArgs e)
    {
        if (_isUpdatingSelection) return;

        var listBox = Guard.IsNotNullAndAssignableToTypeAndReturn<ComboBox>(sender);

        BindableSelectedItem = listBox.SelectedItem ?? FallbackSelectItem;

        Guard.IsNotNullAndReturn(BindingOperations.GetBindingExpression(this, BindableSelectedItemProperty)).UpdateSource();
    }
}