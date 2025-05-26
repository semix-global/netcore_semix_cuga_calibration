using System.Collections;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;

namespace Net.Utilities.WPF.AttachedHelper;

public sealed class MultiSelectorHelper
{
    #region Bindable SelectedItems

    public static readonly DependencyProperty BindableSelectedItemsProperty = DependencyProperty.RegisterAttached(
        "BindableSelectedItems",
        typeof(IList),
        typeof(MultiSelectorHelper),
        new FrameworkPropertyMetadata(null, FrameworkPropertyMetadataOptions.BindsTwoWayByDefault)
    );

    public static IList? GetBindableSelectedItems(DependencyObject obj)
    {
        return (IList?)obj.GetValue(BindableSelectedItemsProperty);
    }

    public static void SetBindableSelectedItems(DependencyObject obj, IList value)
    {
        obj.SetValue(BindableSelectedItemsProperty, value);
    }

    #endregion Bindable SelectedItems

    #region Monitor SelectionChanged

    public static readonly DependencyProperty MonitorSelectionChangedProperty = DependencyProperty.RegisterAttached(
        "MonitorSelectionChanged",
        typeof(bool),
        typeof(MultiSelectorHelper),
        new PropertyMetadata(false, MonitorSelectionChangedPropertyChangedCallback)
    );

    public static bool GetMonitorSelectionChanged(DependencyObject obj)
    {
        return (bool)obj.GetValue(MonitorSelectionChangedProperty);
    }

    public static void SetMonitorSelectionChanged(DependencyObject obj, bool value)
    {
        obj.SetValue(MonitorSelectionChangedProperty, value);
    }

    private static void MonitorSelectionChangedPropertyChangedCallback(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        if (d is not MultiSelector selector) throw new InvalidOperationException();

        if ((bool)e.NewValue)
            selector.SelectionChanged += OnSelectionChanged;
        else
            selector.SelectionChanged -= OnSelectionChanged;
    }

    private static void OnSelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        var selector = (MultiSelector)sender;
        SetBindableSelectedItems(selector, selector.SelectedItems);
        selector.GetBindingExpression(BindableSelectedItemsProperty)?.UpdateSource();
    }

    #endregion Monitor SelectionChanged
}