using algocv_sharp;
using CommunityToolkit.Diagnostics;
using Net.Utilities.Graphics.Extensions;
using Net.Utilities.Graphics.Primitives.Medias.Imaging;
using Net.Utilities.Models.Geometries;

namespace Net.Utilities.Algorithms.AlgoCSharp;

/// <summary>
/// BitmapImage 显示域算子的 AlgoCSharp 实现（algolib 现成方法的薄封装）。
/// <para>2026-10-23 裁定：能用 algolib（algocv_sharp）现成方法的不自造（如 LogToLinear、ScaleImageMax、Rotate、Mirror、Crop）；
/// algolib 暂无现成算子但有生产消费的（GetIntensity、GetHorizontalProjects）以 C# 自写等价实现补齐，并标记 TODO(algo-未完成)，
/// 待 algolib 长出原生算子后替换为原生薄封装；生产零消费的（区域极值、钳位相减）不自写复刻，VM 调用点显式调用 Halcon 实现。</para>
/// <para>几何类方法（翻转/旋转/ROI）经等价测试锁定与 Halcon 实现逐像素等价，可接入 BitmapImage 同名分流；
/// ToLinearImage/SaveImage 与 Halcon 版存在 ±1 数值精度差（float32 取整 vs 双精度），按规则③保留各自显式名字。</para>
/// </summary>
public static class BitmapImageAlgoImageOperations
{
    /// <summary>
    /// 保存图片（对齐 Halcon Save 语义：大于 8bit 时线性拉伸到 8bit 后写文件）.
    /// <para>【容差标记】拉伸用 algolib 现成的 <see cref="ImageProc.ScaleImageMax(Image, ScaleTargetMax, ImageDataType)"/>（min-max 归一化 + float 取整），
    /// 与 Halcon scale_image_max 黑盒取整存在数值差（视觉近似）；2026-10-24 裁定进同名分流，等价测试以容差断言锁定差异界。</para>
    /// </summary>
    /// <param name="image">待保存图片</param>
    /// <param name="filePath">文件路径</param>
    public static void SaveImage(BitmapImage image, string filePath)
    {
        using var algoImage = image.ToAlgoCVImage();

        if (algoImage.DataType == ImageDataType.UInt8)
        {
            DeleteIfExistsAndSave(algoImage, filePath);
            return;
        }

        using var scaled = ImageProc.ScaleImageMax(algoImage, ScaleTargetMax.UInt8, ImageDataType.UInt8);
        DeleteIfExistsAndSave(scaled, filePath);

        static void DeleteIfExistsAndSave(Image img, string path)
        {
            var fullPath = Path.GetFullPath(path);
            Directory.CreateDirectory(Path.GetDirectoryName(fullPath)!);
            File.Delete(path);
            img.Save(path);
        }
    }

    /// <summary>
    /// log 域 12bit → 线性域转换（algolib 现成的 <see cref="ImageProc.LogToLinear(Image, ImageDataType)"/> 薄封装）.
    /// <para>【容差标记】原生 float32 计算取整与 Halcon 版（RAW12BitsPerPixelLogToLinear 双精度截断 LUT）存在 ±1 灰度系统差；
    /// 2026-10-24 裁定进同名分流，等价测试以 ±1 容差断言锁定差异界。</para>
    /// </summary>
    /// <param name="image">log 域 12bit 图</param>
    /// <returns>线性域图（位深标记与输入一致）</returns>
    public static BitmapImage ToLinearImage(BitmapImage image)
    {
        using var algoImage = image.ToAlgoCVImage();
        Guard.IsTrue(algoImage.DataType == ImageDataType.UInt16 && algoImage.Channels == 1);

        using var linearImage = ImageProc.LogToLinear(algoImage);

        return linearImage.ToBitmapImage(image.ImageInfo.PixelFormatEnum.GetBitsPerPixel());
    }

    /// <summary>
    /// 图片逆时针旋转 90 度.
    /// </summary>
    public static BitmapImage RotateCounterClockwise90Degree(BitmapImage image) => Transform(image, static t => t.Rotate(RotateAngle.Rotate90CounterClockwise));

    /// <summary>
    /// 图片水平翻转.
    /// </summary>
    public static BitmapImage HorizontalFlip(BitmapImage image) => Transform(image, static t => t.Mirror(MirrorMode.Horizontal));

    /// <summary>
    /// 图片垂直翻转.
    /// </summary>
    public static BitmapImage VerticalFlip(BitmapImage image) => Transform(image, static t => t.Mirror(MirrorMode.Vertical));

    /// <summary>
    /// 在指定位置绘制通幅绿色十字（algolib 现成的 <see cref="ImageProc.DrawCross(Image, int, int, int, int, double[])"/> 薄封装）.
    /// 显示域输出：大于 8bit 先 <see cref="ImageProc.ScaleImageMax(Image, ScaleTargetMax, ImageDataType)"/> 到 UInt8 再画,
    /// 避免 DrawCross 保深度升通道得到 3 通道 UInt16, 自动推导 ToBitmapImage 无匹配模式.
    /// 8bit 不拉伸, 十字颜色按 8 位 BGR [0, 255, 0]; 不按源图 12/16bit 回写.
    /// </summary>
    /// <param name="image">待绘制图片</param>
    /// <param name="point">十字中心</param>
    /// <returns>8bit 彩色结果图（Bgr8888）</returns>
    public static BitmapImage DrawCrossLine(BitmapImage image, Point point)
    {
        using var algoImage = image.ToAlgoCVImage();
        using var display8 = algoImage.DataType == ImageDataType.UInt8
            ? null
            : ImageProc.ScaleImageMax(algoImage, ScaleTargetMax.UInt8, ImageDataType.UInt8);
        var canvas = display8 ?? algoImage;
        var radius = Math.Max(canvas.Width, canvas.Height);
        using var drawn = canvas.DrawCross((int)Math.Round(point.X), (int)Math.Round(point.Y), radius, 1, [0d, 255d, 0d]);

        return drawn.ToBitmapImage();
    }

    /// <summary>
    /// 按照 ROI 裁剪图片（algo Crop 为 (x, y, w, h) 列优先，Halcon CropPart 为行优先，此处对齐语义）.
    /// </summary>
    /// <param name="image">待裁剪图片</param>
    /// <param name="rect">Roi</param>
    /// <returns>裁剪图片</returns>
    public static BitmapImage ToROI(BitmapImage image, Rect rect)
    {
        using var algoImage = image.ToAlgoCVImage();
        using var cropped = algoImage.Crop((int)rect.X, (int)rect.Y, (int)rect.Width, (int)rect.Height);

        return ToBitmapImageBySource(cropped, image);
    }

    /// <summary>
    /// 获取图片统计信息（均值与总体标准差，对齐 Halcon Intensity 语义）.
    /// </summary>
    // TODO(algo-未完成): algolib 暂无 Intensity 统计算子，此为 C# 自写等价实现（有生产消费）；algolib 原生算子就绪后替换为原生薄封装（2026-10-23 用户裁定恢复）
    public static (double Average, double Deviation) GetIntensity(BitmapImage image)
    {
        using var algoImage = image.ToAlgoCVImage();
        Guard.IsTrue(algoImage.Channels == 1);

        return algoImage.DataType == ImageDataType.UInt8
            ? GetIntensityCore(algoImage.ToSpan<byte>())
            : GetIntensityCore(algoImage.ToSpan<ushort>());
    }

    // TODO(algo-未完成): algolib 暂无投影算子，此为 C# 自写等价实现（有生产消费）；algolib 原生算子就绪后替换为原生薄封装（2026-10-23 用户裁定恢复）
    /// <summary>
    /// 获取图片的水平方向投影（每行灰度值均值，长度等于图片高度，对齐 Halcon GrayProjections "simple" HorizontalProjection 语义：行均值而非行求和）.
    /// </summary>
    /// <returns>水平方向投影</returns>
    public static double[] GetHorizontalProjects(BitmapImage image)
    {
        using var algoImage = image.ToAlgoCVImage();
        Guard.IsTrue(algoImage.Channels == 1);

        var width = algoImage.Width;
        var results = new double[algoImage.Height];

        if (algoImage.DataType == ImageDataType.UInt8)
        {
            var pixels = algoImage.ToSpan<byte>();
            for (var row = 0; row < algoImage.Height; row++)
            {
                var offset = row * width;
                double sum = 0;
                for (var col = 0; col < width; col++) sum += pixels[offset + col];
                results[row] = sum / width;
            }
        }
        else
        {
            var pixels = algoImage.ToSpan<ushort>();
            for (var row = 0; row < algoImage.Height; row++)
            {
                var offset = row * width;
                double sum = 0;
                for (var col = 0; col < width; col++) sum += pixels[offset + col];
                results[row] = sum / width;
            }
        }

        return results;
    }

    /// <summary>
    /// 均值与总体标准差（对齐 Halcon Intensity 的 Deviation 语义：sqrt(Σ(x-μ)²/n)）.
    /// </summary>
    private static (double Average, double Deviation) GetIntensityCore<T>(Span<T> pixels) where T : struct, IConvertible
    {
        double sum = 0;
        foreach (var t in pixels) sum += t.ToDouble(null);
        var mean = sum / pixels.Length;

        double squareSum = 0;
        foreach (var t in pixels) squareSum += (t.ToDouble(null) - mean) * (t.ToDouble(null) - mean);

        return (mean, Math.Sqrt(squareSum / pixels.Length));
    }

    /// <summary>
    /// 几何变换统一模式：转 algo 图 → 原生操作 → 按输入位深转回 BitmapImage.
    /// </summary>
    private static BitmapImage Transform(BitmapImage image, Func<Image, Image> operation)
    {
        using var algoImage = image.ToAlgoCVImage();
        using var transformed = operation(algoImage);

        return ToBitmapImageBySource(transformed, image);
    }

    /// <summary>
    /// 按源图位深转回 BitmapImage：8bit 走自动推导桥；12/16bit 走位深标记重载（保持 log/linear 域标记语义）.
    /// </summary>
    private static BitmapImage ToBitmapImageBySource(Image image, BitmapImage source)
    {
        var bitsPerPixel = source.ImageInfo.PixelFormatEnum.GetBitsPerPixel();

        return bitsPerPixel == 8 ? image.ToBitmapImage() : image.ToBitmapImage(bitsPerPixel);
    }
}
