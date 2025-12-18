using Core.Models.Models.Common.Status.Interfaces;
using Microsoft.Xaml.Behaviors;
using Net.Utilities.Models;
using System.ComponentModel;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;

namespace Core.Models.Models.Common.Status.Behaviors;

public class CalibrationStatusListBoxSelectedItemsBehavior<TCalibrationStatus, TCalibrationSelectedItem> : Behavior<ListBox>
    where TCalibrationStatus : ICalibrationStatus<TCalibrationSelectedItem>
{
    public static readonly DependencyProperty BindableSelectedItemsProperty = DependencyProperty.Register(
        nameof(BindableSelectedItems),
        typeof(IReadOnlyList<TCalibrationSelectedItem>),
        typeof(CalibrationStatusListBoxSelectedItemsBehavior<TCalibrationStatus, TCalibrationSelectedItem>),
        new FrameworkPropertyMetadata(Array.Empty<TCalibrationSelectedItem>(), FrameworkPropertyMetadataOptions.BindsTwoWayByDefault, BindableSelectedItemsPropertyChangedCallback)
    );

    private static void BindableSelectedItemsPropertyChangedCallback(DependencyObject? d, DependencyPropertyChangedEventArgs e)
    {
        var behavior = GuardUtils.IsNotNullAndAssignableToType<CalibrationStatusListBoxSelectedItemsBehavior<TCalibrationStatus, TCalibrationSelectedItem>>(d);

        if (e.NewValue is null) return;

        behavior.OnItemsSourceChanged(behavior.AssociatedObject, EventArgs.Empty);
    }

    public IReadOnlyList<TCalibrationSelectedItem> BindableSelectedItems
    {
        get => (IReadOnlyList<TCalibrationSelectedItem>)GetValue(BindableSelectedItemsProperty);
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

    private void OnItemsSourceChanged(object sender, EventArgs e)
    {
        _isUpdatingSelection = true;
        try
        {
            AssociatedObject.SelectedItems.Clear();

            if (AssociatedObject.ItemsSource is null) return;

            foreach (var calibrationStatus in AssociatedObject.ItemsSource
                         .Cast<TCalibrationStatus>()
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
                .Cast<TCalibrationStatus>()
                .Select(t => t.SelectedItem)
        ];

        GuardUtils.IsNotNullAndReturn(BindingOperations.GetBindingExpression(this, BindableSelectedItemsProperty)).UpdateSource();
    }
}