using System.ComponentModel;
using System.Windows;
using System.Windows.Controls;
using Microsoft.Xaml.Behaviors;
using Net.Utilities.Models;

namespace Core.Utilities.WPF;

public sealed class ComboBoxFallbackBehavior : Behavior<ComboBox>
{
    public static readonly DependencyProperty FallbackDisplayTextProperty = DependencyProperty.Register(
        nameof(FallbackDisplayText),
        typeof(string),
        typeof(ComboBoxFallbackBehavior),
        new PropertyMetadata("-"));

    public static readonly DependencyProperty FallbackSelectItemProperty = DependencyProperty.Register(
        nameof(FallbackSelectItem),
        typeof(object),
        typeof(ComboBoxFallbackBehavior),
        new PropertyMetadata(null));

    public string FallbackDisplayText
    {
        get => (string)GetValue(FallbackDisplayTextProperty);
        set => SetValue(FallbackDisplayTextProperty, value);
    }

    public object? FallbackSelectItem
    {
        get => (object?)GetValue(FallbackSelectItemProperty);
        set => SetValue(FallbackSelectItemProperty, value);
    }

    protected override void OnAttached()
    {
        base.OnAttached();

        AssociatedObject.Loaded -= OnComboBoxLoaded;
        AssociatedObject.Loaded += OnComboBoxLoaded;
        AssociatedObject.SelectionChanged -= OnSelectionChanged;
        AssociatedObject.SelectionChanged += OnSelectionChanged;

        var itemsSourceDescriptor = DependencyPropertyDescriptor.FromProperty(ItemsControl.ItemsSourceProperty, typeof(ComboBox));
        GuardUtils.IsNotNullAndReturn(itemsSourceDescriptor).AddValueChanged(AssociatedObject, OnItemsSourceChanged);
    }

    protected override void OnDetaching()
    {
        base.OnDetaching();

        AssociatedObject.Loaded -= OnComboBoxLoaded;
        AssociatedObject.SelectionChanged -= OnSelectionChanged;

        var itemsSourceDescriptor = DependencyPropertyDescriptor.FromProperty(ItemsControl.ItemsSourceProperty, typeof(ComboBox));
        GuardUtils.IsNotNullAndReturn(itemsSourceDescriptor).RemoveValueChanged(AssociatedObject, OnItemsSourceChanged);
    }

    private void OnComboBoxLoaded(object sender, RoutedEventArgs e) => CheckAndApplyFallback();

    private void OnItemsSourceChanged(object sender, EventArgs e) => CheckAndApplyFallback();

    private void OnSelectionChanged(object sender, SelectionChangedEventArgs e) => CheckAndApplyFallback();

    private void CheckAndApplyFallback()
    {
        var selectedItem = AssociatedObject.SelectedItem;
        if (AssociatedObject.ItemsSource?.Cast<object>().Any(item => Equals(item, selectedItem)) ?? false) return;

        AssociatedObject.SelectedItem = FallbackSelectItem;
        AssociatedObject.Text = FallbackDisplayText;
    }
}