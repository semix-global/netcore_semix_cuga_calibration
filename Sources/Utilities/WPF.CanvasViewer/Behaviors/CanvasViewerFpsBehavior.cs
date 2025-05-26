using CanvasViewer.Drawables;
using CanvasViewer.View;
using Microsoft.Xaml.Behaviors;
using System.Windows;

namespace CanvasViewer.Behaviors;

public sealed class CanvasViewerFpsBehavior : Behavior<CanvasView>
{
    public static readonly DependencyProperty FpsValueProperty = DependencyProperty.Register(
        nameof(FpsValue),
        typeof(double),
        typeof(CanvasViewerFpsBehavior),
        new PropertyMetadata(0.0, OnPropertyChangedCallback)
    );

    public double FpsValue
    {
        get => (double)GetValue(FpsValueProperty);
        set => SetValue(FpsValueProperty, value);
    }

    private static void OnPropertyChangedCallback(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        if (d is not CanvasViewerFpsBehavior behavior) return;
        behavior._fps.FpsValue = (double)e.NewValue;
    }

    private readonly Fps _fps = new();

    protected override void OnAttached()
    {
        base.OnAttached();

        AssociatedObject.Document.Model.Add(_fps);
    }

    protected override void OnDetaching()
    {
        base.OnDetaching();

        AssociatedObject.Document.Model.Remove(_fps);
    }
}