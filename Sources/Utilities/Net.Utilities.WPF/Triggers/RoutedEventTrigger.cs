using Microsoft.Xaml.Behaviors;
using System.Windows;

namespace Net.Utilities.WPF.Triggers;

public sealed class RoutedEventTrigger : EventTriggerBase<DependencyObject>
{
    private FrameworkElement? _associatedElement;

    public required RoutedEvent RoutedEvent { get; init; }

    protected override void OnAttached()
    {
        base.OnAttached();

        _associatedElement = AssociatedObject as FrameworkElement;
        if (_associatedElement is null && AssociatedObject is IAttachedObject behavior) _associatedElement = behavior.AssociatedObject as FrameworkElement;

        if (_associatedElement is null) throw new ArgumentException("This only works with framework elements");

        _associatedElement.AddHandler(RoutedEvent, new RoutedEventHandler(OnRoutedEvent));
    }

    protected override void OnDetaching()
    {
        base.OnDetaching();

        _associatedElement?.RemoveHandler(RoutedEvent, new RoutedEventHandler(OnRoutedEvent));
    }

    private void OnRoutedEvent(object sender, RoutedEventArgs args)
    {
        OnEvent(args);
    }

    protected override string GetEventName()
    {
        return RoutedEvent.Name;
    }
}