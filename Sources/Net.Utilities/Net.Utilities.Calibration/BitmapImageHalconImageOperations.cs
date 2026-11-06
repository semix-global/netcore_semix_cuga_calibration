using Net.Utilities.Algorithms.Halcon;
using Net.Utilities.Algorithms.Halcon.Extensions;
using Net.Utilities.Graphics.Algorithms.Halcon;
using Net.Utilities.Graphics.Extensions;
using Net.Utilities.Graphics.Primitives.Medias.Imaging;
using Net.Utilities.Models.Geometries;

namespace Net.Utilities.Calibration;

#pragma warning disable IDISP012

/// <summary>
/// BitmapImage 显示域算子的 Halcon 实现（静态方法形态，与 AlgoCSharp 侧的 BitmapImageAlgoImageOperations 对称）。
/// <para>由 <see cref="BitmapImageExtensions"/> 分发面按引擎选择调用；不进同名双实现的方法（SaveImage/ToLinearImage）
/// 亦经分发面以 Halcon 实现供给，algo 侧以独立显式名字存在于 BitmapImageAlgoImageOperations。</para>
/// </summary>
public static class BitmapImageHalconImageOperations
{
    /// <summary>
    /// 保存图片（大于 8bit 时经 Halcon scale_image_max 拉伸后写文件）.
    /// </summary>
    /// <param name="image">待保存图片</param>
    /// <param name="filePath">文件路径</param>
    public static void SaveImage(BitmapImage image, string filePath)
    {
        using var hImage = image.ToHImage();

        hImage.Save(filePath);
    }

    /// <summary>
    /// log 域 12bit → 线性域转换（Halcon 双精度截断 LUT：RAW12BitsPerPixelLogToLinear）.
    /// </summary>
    /// <param name="image">log 域 12bit 图</param>
    /// <returns>线性域图（位深标记与输入一致）</returns>
    public static BitmapImage ToLinearImage(BitmapImage image)
    {
        using var hImage = image.ToHImage();
        using var temp = RAWImageFactory.RAW12BitsPerPixelLogToLinear(hImage);

        return temp.ToBitmapImage(image.ImageInfo.PixelFormatEnum.GetBitsPerPixel());
    }

    /// <summary>
    /// 图片逆时针旋转 90 度.
    /// </summary>
    public static BitmapImage RotateCounterClockwise90Degree(BitmapImage image)
    {
        using var hImage = image.ToHImage();

        using var temp = hImage.RotateCounterClockwise90Degree();

        return temp.ToBitmapImage(image.ImageInfo.PixelFormatEnum.GetBitsPerPixel());
    }

    /// <summary>
    /// 图片水平翻转.
    /// </summary>
    public static BitmapImage HorizontalFlip(BitmapImage image)
    {
        using var hImage = image.ToHImage();

        using var temp = hImage.HorizontalFlip();

        return temp.ToBitmapImage(image.ImageInfo.PixelFormatEnum.GetBitsPerPixel());
    }

    /// <summary>
    /// 图片垂直翻转.
    /// </summary>
    public static BitmapImage VerticalFlip(BitmapImage image)
    {
        using var hImage = image.ToHImage();

        using var temp = hImage.VerticalFlip();

        return temp.ToBitmapImage(image.ImageInfo.PixelFormatEnum.GetBitsPerPixel());
    }

    /// <summary>
    /// 在指定位置绘制十字（ToHImage 后调用现有 HImage.DrawCrossLine）.
    /// </summary>
    /// <param name="image">待绘制图片</param>
    /// <param name="point">十字中心</param>
    /// <returns>绘制后的结果图</returns>
    public static BitmapImage DrawCrossLine(BitmapImage image, Point point)
    {
        using var hImage = image.ToHImage();
        using var drawn = hImage.DrawCrossLine(point);

        return drawn.ToBitmapImage();
    }

    /// <summary>
    /// 按照 ROI 裁剪图片.
    /// </summary>
    /// <param name="rect">Roi</param>
    public static BitmapImage ToROI(BitmapImage image, Rect rect)
    {
        using var hImage = image.ToHImage();

        using var temp = hImage.ToROI(rect);

        return temp.ToBitmapImage(image.ImageInfo.PixelFormatEnum.GetBitsPerPixel());
    }

    /// <summary>
    /// 获取图片统计信息（均值与总体标准差）.
    /// </summary>
    public static (double Average, double Deviation) GetIntensity(BitmapImage image)
    {
        using var hImage = image.ToHImage();

        return hImage.GetIntensity();
    }

    /// <summary>
    /// 图片相减（Halcon sub_image 钳位语义：负值截断到 0，超出截断到类型最大值）.
    /// </summary>
    /// <param name="image">被减图</param>
    /// <param name="subImage">减图</param>
    /// <returns>相减结果</returns>
    public static BitmapImage SubImage(BitmapImage image, BitmapImage subImage)
    {
        using var hImage = image.ToHImage();
        using var hSubImage = subImage.ToHImage();

        using var resultImage = hImage.SubImage(hSubImage, 1d, 0d);

        return resultImage.ToBitmapImage(image.ImageInfo.PixelFormatEnum.GetBitsPerPixel());
    }

    /// <summary>
    /// 获取图片的水平方向投影（Halcon GrayProjections "simple" HorizontalProjection：每行灰度值均值）.
    /// </summary>
    /// <returns>水平方向投影</returns>
    public static double[] GetHorizontalProjects(BitmapImage image)
    {
        using var hImage = image.ToHImage();

        return hImage.GetHorizontalProjects();
    }

    /// <summary>
    /// 获取图片在指定矩形区域内的最小灰度值与最大灰度值及其对应的位置
    /// （Halcon GenRectangle1 语义：区域含 (Y+Height, X+Width) 端点，即比 rect 多含一行一列）.
    /// </summary>
    /// <param name="rect">感兴趣区域</param>
    /// <returns>最大灰度值及其坐标，最小灰度值及其坐标</returns>
    public static (double MaxGrayValue, Point[] maxGrayPoints, double MinGrayValue, Point[] minGrayPoints) GetMaxMinGrayValue(BitmapImage image, Rect rect)
    {
        using var hImage = image.ToHImage();

        return hImage.GetMaxMinGrayValue(rect);
    }
}
