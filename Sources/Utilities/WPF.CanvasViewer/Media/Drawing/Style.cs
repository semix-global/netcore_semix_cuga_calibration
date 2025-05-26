using CanvasViewer.Media.Drawing.Enum;

namespace CanvasViewer.Media.Drawing;

public sealed class Style(Color color, double lineWeight = 1, DashStyleEnum dashStyle = DashStyleEnum.Solid)
{
    /// <summary>
    /// 线宽使用Layer的线宽
    /// </summary>
    public const int ByLayer = -1;

    /// <summary>
    /// 默认样式
    /// </summary>
    public static Style Default => new(Color.ByLayer, ByLayer, DashStyleEnum.ByLayer);

    #region 属性

    /// <summary>
    /// 颜色
    /// </summary>
    public Color Color { get; set; } = color;

    /// <summary>
    /// 线宽: 度量单位通常为像素。 如果 Width 为 0，则会导致绘图， Pen 就像 Width 是 1 一样
    /// </summary>
    public double LineWeight { get; set; } = lineWeight;

    /// <summary>
    /// dash样式
    /// </summary>
    public DashStyleEnum DashStyle { get; set; } = dashStyle;

    #endregion 属性

    #region 构造

    public Style() : this(Color.ByLayer, ByLayer, DashStyleEnum.ByLayer)
    {
    }

    #endregion 构造

    #region 方法

    public Style ApplyLayer(Layer layer)
    {
        if (Color.IsByLayer) Color = layer.Style.Color; // 样式颜色通过Layer来设置
        if (LineWeight - ByLayer == 0) LineWeight = layer.Style.LineWeight; // 样式颜色通过Layer来设置
        if (DashStyle == DashStyleEnum.ByLayer) DashStyle = layer.Style.DashStyle; // 样式颜色通过Layer来设置

        return this;
    }

    public Style Clone()
    {
        return new Style(Color, LineWeight, DashStyle);
    }

    #endregion 方法
}