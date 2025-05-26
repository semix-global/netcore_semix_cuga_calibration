using CanvasViewer.Media.Drawing.Enum;

namespace CanvasViewer.Media.Drawing;

public sealed class TextStyle(string name, string fontFamily, FontStyleEnum fontStyle)
{
    /// <summary>
    /// 默认样式
    /// </summary>
    public static TextStyle Default => new("0", "Calibri Light", FontStyleEnum.Regular);

    #region 属性

    /// <summary>
    /// 字体样式名称
    /// </summary>
    public string Name { get; private set; } = name;

    /// <summary>
    /// 字体名称
    /// </summary>
    public string FontFamily { get; set; } = fontFamily;

    /// <summary>
    /// 字体样式
    /// </summary>
    public FontStyleEnum FontStyle { get; set; } = fontStyle;

    #endregion 属性

    public TextStyle() : this("0", "Calibri Light", FontStyleEnum.Regular)
    {
    }

    public (System.Windows.FontStyle FontStyle, System.Windows.FontWeight FontWeight) GetFontStyle()
    {
        var style = System.Windows.FontStyles.Normal;
        var weight = System.Windows.FontWeights.Normal;

        if ((FontStyle & FontStyleEnum.Bold) != 0) weight = System.Windows.FontWeights.Bold;

        if ((FontStyle & FontStyleEnum.Italic) != 0) style = System.Windows.FontStyles.Italic;

        return (style, weight);
    }
}