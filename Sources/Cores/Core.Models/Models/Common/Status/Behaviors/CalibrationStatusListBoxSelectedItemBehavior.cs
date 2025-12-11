using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using Core.Models.Models.Common.Status.Interfaces;
using Microsoft.Xaml.Behaviors;
using Net.Utilities.Models;

namespace Core.Models.Models.Common.Status.Behaviors;

public class CalibrationStatusListBoxSelectedItemBehavior<TCalibrationStatus, TCalibrationSelectedItem> : Behavior<ListBox>
    where TCalibrationStatus : ICalibrationStatus<TCalibrationSelectedItem>
{
    public TCalibrationSelectedItem BindableSelectedItem
    {
        get => (TCalibrationSelectedItem)GetValue(BindableSelectedItemProperty);
        set => SetValue(BindableSelectedItemProperty, value);
    }

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

        var item = GuardUtils.IsAssignableToType<TCalibrationSelectedItem>(e.NewValue);

        behavior._isUpdatingSelection = true;
        try
        {
            behavior.AssociatedObject.SelectedItem = item;
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

        if (listBox.SelectedItem is null) return;
        BindableSelectedItem = GuardUtils.IsAssignableToType<TCalibrationStatus>(listBox.SelectedItem).SelectedItem;

        GuardUtils.IsNotNullAndReturn(BindingOperations.GetBindingExpression(this, BindableSelectedItemProperty)).UpdateSource();
    }
}