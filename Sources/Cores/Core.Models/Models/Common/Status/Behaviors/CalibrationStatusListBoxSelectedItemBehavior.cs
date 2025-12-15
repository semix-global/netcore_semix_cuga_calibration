using System.ComponentModel;
using Core.Models.Models.Common.Status.Interfaces;
using Microsoft.Xaml.Behaviors;
using Net.Utilities.Models;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;

namespace Core.Models.Models.Common.Status.Behaviors;

public class CalibrationStatusListBoxSelectedItemBehavior<TCalibrationStatus, TCalibrationSelectedItem> : Behavior<ListBox>
    where TCalibrationStatus : ICalibrationStatus<TCalibrationSelectedItem>
{
    public static readonly DependencyProperty BindableSelectedItemProperty = DependencyProperty.Register(
        nameof(BindableSelectedItem),
        typeof(TCalibrationSelectedItem),
        typeof(CalibrationStatusListBoxSelectedItemBehavior<TCalibrationStatus, TCalibrationSelectedItem>),
        new FrameworkPropertyMetadata(default(TCalibrationSelectedItem), FrameworkPropertyMetadataOptions.BindsTwoWayByDefault, BindableSelectedItemsPropertyChangedCallback)
    );

    private static void BindableSelectedItemsPropertyChangedCallback(DependencyObject? d, DependencyPropertyChangedEventArgs e)
    {
        var behavior = GuardUtils.IsNotNullAndAssignableToType<CalibrationStatusListBoxSelectedItemBehavior<TCalibrationStatus, TCalibrationSelectedItem>>(d);

        if (e.NewValue is null) return;

        behavior.OnItemsSourceChanged(behavior.AssociatedObject, EventArgs.Empty);
    }

    public TCalibrationSelectedItem BindableSelectedItem
    {
        get => (TCalibrationSelectedItem)GetValue(BindableSelectedItemProperty);
        set => SetValue(BindableSelectedItemProperty, value);
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

    private void OnItemsSourceChanged(object sender, EventArgs e)
    {
        _isUpdatingSelection = true;
        try
        {
            AssociatedObject.SelectedItem = null;

            if (AssociatedObject.ItemsSource is null) return;

            AssociatedObject.SelectedItem = AssociatedObject.ItemsSource
                .Cast<TCalibrationStatus>()
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

        var listBox = GuardUtils.IsNotNullAndAssignableToType<ListBox>(sender);

        if (listBox.SelectedItem is null) return;
        BindableSelectedItem = GuardUtils.IsAssignableToType<TCalibrationStatus>(listBox.SelectedItem).SelectedItem;

        GuardUtils.IsNotNullAndReturn(BindingOperations.GetBindingExpression(this, BindableSelectedItemProperty)).UpdateSource();
    }
}