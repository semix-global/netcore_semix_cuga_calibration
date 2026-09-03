using System.Collections;
using System.ComponentModel;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using CommunityToolkit.Diagnostics;
using Microsoft.Xaml.Behaviors;
using Net.Utilities.Helpers.Helpers;

namespace Net.Utilities.WPF.Behaviors.Calibration;

public sealed class ListBoxSelectedItemsBehavior : Behavior<ListBox>
{
    public static readonly DependencyProperty TypeProperty = DependencyProperty.Register(
        nameof(Type),
        typeof(Type),
        typeof(ListBoxSelectedItemsBehavior),
        new PropertyMetadata(null));

    public static readonly DependencyProperty BindableSelectedItemsProperty = DependencyProperty.Register(
        nameof(BindableSelectedItems),
        typeof(IEnumerable),
        typeof(ListBoxSelectedItemsBehavior),
        new FrameworkPropertyMetadata(null, FrameworkPropertyMetadataOptions.BindsTwoWayByDefault, BindableSelectedItemsPropertyChangedCallback)
    );

    private static void BindableSelectedItemsPropertyChangedCallback(DependencyObject? d, DependencyPropertyChangedEventArgs e)
    {
        var behavior = Guard.IsNotNullAndAssignableToTypeAndReturn<ListBoxSelectedItemsBehavior>(d);

        if (e.NewValue is null) return;

        behavior.OnItemsSourceChanged(behavior.AssociatedObject, EventArgs.Empty);
    }

    public Type? Type
    {
        get => (Type?)GetValue(TypeProperty);
        set => SetValue(TypeProperty, value);
    }

    public IEnumerable? BindableSelectedItems
    {
        get => (IEnumerable?)GetValue(BindableSelectedItemsProperty);
        set => SetValue(BindableSelectedItemsProperty, value);
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
            AssociatedObject.SelectedItems.Clear();

            if (AssociatedObject.ItemsSource is null) return;

            if (BindableSelectedItems is null) return;

            foreach (var item in BindableSelectedItems)
            {
                if (AssociatedObject.Items.Contains(item)) AssociatedObject.SelectedItems.Add(item);
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

        Guard.IsNotNull(Type);

        var listBox = Guard.IsNotNullAndAssignableToTypeAndReturn<ListBox>(sender);

        BindableSelectedItems = ObjectHelper.ConvertToArray(listBox.SelectedItems, Type);

        Guard.IsNotNullAndReturn(BindingOperations.GetBindingExpression(this, BindableSelectedItemsProperty)).UpdateSource();
    }
}