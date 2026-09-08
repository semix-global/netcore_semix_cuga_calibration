using CommunityToolkit.Diagnostics;
using Net.Utilities.Graphics;
using Net.Utilities.Graphics.WPF;
using System.ComponentModel;
using System.Windows;
using System.Windows.Controls;

namespace Net.Utilities.OpticsFourierImageViewer.WPF;

[ToolboxItem(true)]
[DesignTimeVisible(true)]
[TemplatePart(Name = PartWPFCanvasControl, Type = typeof(WPFCanvasControl))]
public class OpticsFourierImageCanvasControl : Control
{
    private const string PartWPFCanvasControl = "PART_WPFCanvasControl";

    public static readonly DependencyProperty DocumentProperty = DependencyProperty.Register(
        nameof(Document),
        typeof(CanvasDocument),
        typeof(OpticsFourierImageCanvasControl),
        new FrameworkPropertyMetadata(new OpticsFourierImageDocument()));

    public OpticsFourierImageDocument Document
    {
        get => (OpticsFourierImageDocument)GetValue(DocumentProperty);
        set => SetValue(DocumentProperty, value);
    }

    static OpticsFourierImageCanvasControl()
    {
        DefaultStyleKeyProperty.OverrideMetadata(typeof(OpticsFourierImageCanvasControl), new FrameworkPropertyMetadata(typeof(OpticsFourierImageCanvasControl)));
    }

    // ReSharper disable NotAccessedField.Local
    private WPFCanvasControl? _canvasControl;
    // ReSharper restore NotAccessedField.Local

    public OpticsFourierImageCanvasControl()
    {
        var defaultCanvasDocument = new CanvasDocument();

        SetCurrentValue(DocumentProperty, defaultCanvasDocument);
    }

    public override void OnApplyTemplate()
    {
        base.OnApplyTemplate();

        _canvasControl = Guard.IsNotNullAndAssignableToTypeAndReturn<WPFCanvasControl>(GetTemplateChild(PartWPFCanvasControl));
    }
}