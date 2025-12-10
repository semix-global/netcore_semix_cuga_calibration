using Core.Models.Models.Common.Status.Interfaces;
using Microsoft.Xaml.Behaviors;
using Net.Utilities.Models;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;

namespace Core.Models.Models.Common.Status.Behaviours;

public class CalibrationStatusListBoxSelectedItemsBehavior<TCalibrationStatus, TCalibrationSelectedItem> : Behavior<ListBox>
    where TCalibrationStatus : ICalibrationStatus<TCalibrationSelectedItem>
{
    public IReadOnlyList<TCalibrationSelectedItem>? BindableSelectedItems
    {
        get => (IReadOnlyList<TCalibrationSelectedItem>?)GetValue(BindableSelectedItemsProperty);
        set => SetValue(BindableSelectedItemsProperty, value);
    }

    public static readonly DependencyProperty BindableSelectedItemsProperty = DependencyProperty.Register(
        nameof(BindableSelectedItems),
        typeof(IReadOnlyList<TCalibrationSelectedItem>),
        typeof(CalibrationStatusListBoxSelectedItemsBehavior<TCalibrationStatus, TCalibrationSelectedItem>),
        new FrameworkPropertyMetadata(null, FrameworkPropertyMetadataOptions.BindsTwoWayByDefault, BindableSelectedItemsPropertyChangedCallback)
    );

    private static void BindableSelectedItemsPropertyChangedCallback(DependencyObject? d, DependencyPropertyChangedEventArgs e)
    {
        var behavior = GuardUtils.IsNotNullAndAssignableToType<CalibrationStatusListBoxSelectedItemsBehavior<TCalibrationStatus, TCalibrationSelectedItem>>(d);

        if (e.NewValue is null) return;

        var items = GuardUtils.IsAssignableToType<IReadOnlyList<TCalibrationSelectedItem>>(e.NewValue);

        behavior._isUpdatingSelection = true;
        try
        {
            behavior.AssociatedObject.SelectedItems.Clear();

            foreach (var calibrationStatus in behavior.AssociatedObject.ItemsSource
                         .Cast<TCalibrationStatus>()
                         .Where(t => items.Contains(t.SelectedItem)))
            {
                behavior.AssociatedObject.SelectedItems.Add(calibrationStatus);
            }
        }
        finally
        {
            behavior._isUpdatingSelection = false;
        }
    }

    private bool _isUpdatingSelection;

    protected override void OnAttached()
    {
        base.OnAttached();

        AssociatedObject.SelectionChanged -= OnSelectionChanged;
        AssociatedObject.SelectionChanged += OnSelectionChanged;
    }

    protected override void OnDetaching()
    {
        base.OnDetaching();

        AssociatedObject.SelectionChanged -= OnSelectionChanged;
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