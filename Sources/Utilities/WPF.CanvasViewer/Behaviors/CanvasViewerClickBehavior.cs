using CanvasViewer.Media.Drawing.Enum;
using CanvasViewer.View;
using Microsoft.Xaml.Behaviors;
using System.Windows;
using System.Windows.Input;
using Point = Net.Utilities.Models.Point;

namespace CanvasViewer.Behaviors;

public sealed class CanvasViewerClickBehavior : Behavior<CanvasView>
{
    public required ICommand Command
    {
        get => (ICommand)GetValue(CommandProperty);
        set => SetValue(CommandProperty, value);
    }

    public static readonly DependencyProperty CommandProperty =
        DependencyProperty.Register(nameof(Command), typeof(ICommand), typeof(CanvasViewerClickBehavior), new PropertyMetadata(null));

    public MouseButtonsEnum ClickMouseButtonsEnum
    {
        get => (MouseButtonsEnum)GetValue(ClickMouseButtonsEnumProperty);
        set => SetValue(ClickMouseButtonsEnumProperty, value);
    }

    public static readonly DependencyProperty ClickMouseButtonsEnumProperty =
        DependencyProperty.Register(nameof(ClickMouseButtonsEnum), typeof(MouseButtonsEnum), typeof(CanvasViewerClickBehavior), new PropertyMetadata(MouseButtonsEnum.Left));

    protected override void OnAttached()
    {
        base.OnAttached();
        AssociatedObject.OnCursorClick -= OnCursorClick;
        AssociatedObject.OnCursorClick += OnCursorClick;
    }

    protected override void OnDetaching()
    {
        base.OnDetaching();
        AssociatedObject.OnCursorClick -= OnCursorClick;
    }

    private void OnCursorClick(object sender, Media.EventArg.CursorEventArgs e)
    {
        if (e.Button == ClickMouseButtonsEnum && Command.CanExecute(new Point(e.Location.X, e.Location.Y))) Command.Execute(new Point(e.Location.X, e.Location.Y));
    }
}