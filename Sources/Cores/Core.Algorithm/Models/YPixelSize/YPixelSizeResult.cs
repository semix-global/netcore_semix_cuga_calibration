using Net.Utilities.Graphics.Primitives.Medias.Imaging;
using Net.Utilities.Models.Geometries;

namespace Core.Algorithm.Models.YPixelSize;

public sealed class YPixelSizeResult
{
    /// <summary>
    /// Y像素尺寸um
    /// </summary>
    public double YPixelSize { get; init; }

    /// <summary>
    /// 结果可视化图像
    /// </summary>
    public required BitmapImage DrawingImage { get; init; }

    /// <summary>
    /// Y 投影曲线
    /// </summary>
    public Point[] YProjects { get; init; } = [];

    /// <summary>
    /// 投影峰位索引（Y 投影曲线 AMPD 峰位，对齐 HAlgorithm 版语义）
    /// </summary>
    public int[] ResultIndexes { get; init; } = [];

    public object ToHtmlAnonymous() => new
    {
        YPixelSize
    };
}
