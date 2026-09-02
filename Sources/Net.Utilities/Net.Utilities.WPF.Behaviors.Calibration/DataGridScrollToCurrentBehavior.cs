using System.Collections;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Windows.Controls;
using Microsoft.Xaml.Behaviors;

namespace Net.Utilities.WPF.Behaviors.Calibration;

public sealed class DataGridScrollToCurrentBehavior : Behavior<DataGrid>
{
    private INotifyCollectionChanged? _observableCollection;

    protected override void OnAttached()
    {
        base.OnAttached();

        AssociatedObject.SelectionChanged -= OnSelectionChanged;
        AssociatedObject.SelectionChanged += OnSelectionChanged;

        var descriptor = DependencyPropertyDescriptor.FromProperty(ItemsControl.ItemsSourceProperty, typeof(ListBox));
        descriptor.RemoveValueChanged(AssociatedObject, OnItemsSourceChanged);
        descriptor.AddValueChanged(AssociatedObject, OnItemsSourceChanged);
    }

    protected override void OnDetaching()
    {
        base.OnDetaching();
        AssociatedObject.SelectionChanged -= OnSelectionChanged;

        var descriptor = DependencyPropertyDescriptor.FromProperty(ItemsControl.ItemsSourceProperty, typeof(ListBox));
        descriptor.RemoveValueChanged(AssociatedObject, OnItemsSourceChanged);

        if (_observableCollection is not null) _observableCollection.CollectionChanged -= ObservableCollectionOnCollectionChanged;
    }

    private void OnSelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (AssociatedObject.SelectedItem is not null) AssociatedObject.ScrollIntoView(AssociatedObject.SelectedItem);
    }

    private void OnItemsSourceChanged(object? sender, EventArgs e)
    {
        AttachToCollection(AssociatedObject.ItemsSource);
    }

    private void AttachToCollection(IEnumerable itemSource)
    {
        _observableCollection?.CollectionChanged -= ObservableCollectionOnCollectionChanged;
        _observableCollection = null;

        if (itemSource is INotifyCollectionChanged observableCollection)
        {
            _observableCollection = observableCollection;

            _observableCollection.CollectionChanged -= ObservableCollectionOnCollectionChanged;
            _observableCollection.CollectionChanged += ObservableCollectionOnCollectionChanged;
        }
        else if (AssociatedObject.Items.Count > 0)
        {
            AssociatedObject.ScrollIntoView(AssociatedObject.Items[^1]);
        }
    }

    private void ObservableCollectionOnCollectionChanged(object? sender, NotifyCollectionChangedEventArgs e)
    {
        if (e.Action == NotifyCollectionChangedAction.Add && AssociatedObject.Items.Count > 0)
        {
            AssociatedObject.ScrollIntoView(AssociatedObject.Items[^1]);
        }
    }
}