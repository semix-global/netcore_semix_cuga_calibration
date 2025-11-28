using Core.Models.Models.Common.Pattern;
using Microsoft.Xaml.Behaviors;
using Net.Utilities.Models;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;

namespace Core.Models.Models.Common.Status.Behaviours;

public sealed class CIBInformationCalibrationStatusListBoxSelectedItemsBehavior : Behavior<ListBox>
{
    public IReadOnlyList<CIBInformation>? BindableSelectedItems
    {
        get => (IReadOnlyList<CIBInformation>?)GetValue(BindableSelectedItemsProperty);
        set => SetValue(BindableSelectedItemsProperty, value);
    }

    public static readonly DependencyProperty BindableSelectedItemsProperty = DependencyProperty.Register(
        nameof(BindableSelectedItems),
        typeof(IReadOnlyList<CIBInformation>),
        typeof(CIBInformationCalibrationStatusListBoxSelectedItemsBehavior),
        new FrameworkPropertyMetadata(null, FrameworkPropertyMetadataOptions.BindsTwoWayByDefault, BindableSelectedItemsPropertyChangedCallback)
    );

    private static void BindableSelectedItemsPropertyChangedCallback(DependencyObject? d, DependencyPropertyChangedEventArgs e)
    {
        var behavior = GuardUtils.IsNotNullAndAssignableToType<CIBInformationCalibrationStatusListBoxSelectedItemsBehavior>(d);

        if (e.NewValue is null) return;

        var items = GuardUtils.IsAssignableToType<IReadOnlyList<CIBInformation>>(e.NewValue);

        behavior._isUpdatingSelection = true;
        try
        {
            behavior.AssociatedObject.SelectedItems.Clear();

            foreach (var cibInformationCalibrationStatus in behavior.AssociatedObject.ItemsSource
                         .Cast<CIBInformationCalibrationStatus>()
                         .Where(t => items.Contains(t.CIBInformation)))
            {
                behavior.AssociatedObject.SelectedItems.Add(cibInformationCalibrationStatus);
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
                .Cast<CIBInformationCalibrationStatus>()
                .Select(t => t.CIBInformation)
        ];

        GuardUtils.IsNotNullAndReturn(BindingOperations.GetBindingExpression(this, BindableSelectedItemsProperty)).UpdateSource();
    }
}