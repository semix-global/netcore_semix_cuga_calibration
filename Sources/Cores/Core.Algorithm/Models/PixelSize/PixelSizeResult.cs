using Net.Utilities.Graphics.Primitives.Medias.Imaging;
using Net.Utilities.Models.Geometries;

namespace Core.Algorithm.Models.PixelSize;

/// <summary>
/// BF 像素尺寸引擎结果
/// </summary>
public sealed class PixelSizeResult
{
    /// <summary>
    /// 像素尺寸 um/px
    /// </summary>
    public Size PixelSize { get; init; }

    /// <summary>
    /// 结果可视化图像
    /// </summary>
    public required BitmapImage DrawingImage { get; init; }

    /// <summary>
    /// 网格水平夹角（度）
    /// </summary>
    public double Angle { get; init; }

    public object ToHtmlAnonymous() => new
    {
        PixelSize,
        Angle
    };
}