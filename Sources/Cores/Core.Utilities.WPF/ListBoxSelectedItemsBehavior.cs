using CommunityToolkit.Diagnostics;
using Microsoft.Xaml.Behaviors;
using Net.Utilities.Models;
using System.Collections;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;

namespace Core.Utilities.WPF;

public sealed class ListBoxSelectedItemsBehavior : Behavior<ListBox>
{
    public Type? Type { get; set; }

    public IEnumerable? BindableSelectedItems
    {
        get => (IEnumerable?)GetValue(BindableSelectedItemsProperty);
        set => SetValue(BindableSelectedItemsProperty, value);
    }

    public static readonly DependencyProperty BindableSelectedItemsProperty = DependencyProperty.Register(
        nameof(BindableSelectedItems),
        typeof(IEnumerable),
        typeof(ListBoxSelectedItemsBehavior),
        new FrameworkPropertyMetadata(null, FrameworkPropertyMetadataOptions.BindsTwoWayByDefault, BindableSelectedItemsPropertyChangedCallback)
    );

    private static void BindableSelectedItemsPropertyChangedCallback(DependencyObject? d, DependencyPropertyChangedEventArgs e)
    {
        var behavior = GuardUtils.IsNotNullAndAssignableToType<ListBoxSelectedItemsBehavior>(d);

        if (e.NewValue is null) return;

        var items = GuardUtils.IsAssignableToType<IEnumerable>(e.NewValue);

        behavior._isUpdatingSelection = true;
        try
        {
            behavior.AssociatedObject.SelectedItems.Clear();

            foreach (var item in items) behavior.AssociatedObject.SelectedItems.Add(item);
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

        Guard.IsNotNull(Type);

        var listBox = GuardUtils.IsNotNullAndAssignableToType<ListBox>(sender);

        BindableSelectedItems = ObjectHelper1.ConvertToArray(listBox.SelectedItems, Type);

        GuardUtils.IsNotNullAndReturn(BindingOperations.GetBindingExpression(this, BindableSelectedItemsProperty)).UpdateSource();
    }
}