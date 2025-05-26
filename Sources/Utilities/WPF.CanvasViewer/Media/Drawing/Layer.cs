namespace CanvasViewer.Media.Drawing;

/// <summary>
/// 图层
/// </summary>
public sealed class Layer
{
    /// <summary>
    /// 默认图层
    /// </summary>
    public static Layer Default => new("0", new Style(Color.White));

    #region 属性

    /// <summary>
    /// 图层名称
    /// </summary>
    public string Name { get; private set; }

    /// <summary>
    /// 图层样式
    /// </summary>
    public Style Style { get; set; } = new(Color.White);

    /// <summary>
    /// 图层是否可见
    /// </summary>
    public bool IsVisible { get; set; } = true;

    #endregion 属性

    #region 构造

    public Layer()
    {
        Name = "0";
    }

    public Layer(string name, Style style)
    {
        Name = name;
        Style = style;
    }

    #endregion 构造
}