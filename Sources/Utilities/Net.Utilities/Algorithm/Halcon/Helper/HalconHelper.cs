using CommunityToolkit.Diagnostics;
using HalconDotNet;
using Net.Utilities.Helper.File;
using Net.Utilities.Models;

namespace Net.Utilities.Algorithm.Halcon.Helper;

public static class HalconHelper
{
    #region 通用

    /// <summary>
    /// 空的HObject, 指向IntPtr.Zero, 空的
    /// </summary>
    public static HObject EmptyHObject => new();

    /// <summary>
    /// 空的HTuple, 空的Array
    /// </summary>
    public static HTuple EmptyHTuple => new();

    /// <summary>
    /// 读取图片
    /// </summary>
    /// <param name="imageFilePath">图片路径</param>
    /// <returns>图片</returns>
    public static HObject ReadImage(string imageFilePath)
    {
        HOperatorSet.ReadImage(out var image, imageFilePath);
        return image;
    }

    /// <summary>
    /// 获取图片尺寸
    /// </summary>
    /// <param name="image">图片</param>
    /// <returns>尺寸</returns>
    public static Size GetSize(HObject image)
    {
        HOperatorSet.GetImageSize(image, out var width, out var height);
        using var _0 = width;
        using var _1 = height;

        var size = new Size(width.D, height.D);
        return size;
    }

    /// <summary>
    /// 按照ROI裁剪图片
    /// </summary>
    /// <param name="image">图片</param>
    /// <param name="rect">Roi</param>
    /// <returns>裁剪图片</returns>
    public static HObject ToRoi(HObject image, Rect rect)
    {
        // 生成矩形(row: y, column: x)
        HOperatorSet.GenRectangle1(out var rectangle, rect.Y, rect.X, rect.Y + rect.Height, rect.X + rect.Width);
        using var _0 = rectangle;

        // 图片相减
        HOperatorSet.ReduceDomain(image, rectangle, out var partImage);
        using var _1 = partImage;

        // 裁剪图片
        HOperatorSet.CropDomain(partImage, out var cropImage);

        return cropImage;
    }

    /// <summary>
    /// 创建副本
    /// </summary>
    /// <param name="image">图片</param>
    /// <returns>图片副本</returns>
    public static HObject Copy(HObject image)
    {
        HOperatorSet.CopyImage(image, out var imageCopy);
        return imageCopy;
    }

    /// <summary>
    /// 彩色图转灰度图
    /// </summary>
    /// <param name="image">图片</param>
    /// <returns>图片副本</returns>
    public static HObject ToGray(HObject image)
    {
        HOperatorSet.CountChannels(image, out var channels);
        using var _ = channels;

        if (channels == 1) return Copy(image);

        HOperatorSet.Rgb1ToGray(image, out var gray);
        return gray;
    }

    /// <summary>
    /// 线性拉伸到8位图片0-255
    /// </summary>
    /// <param name="image">图片</param>
    /// <returns>8位图片0-255</returns>
    public static HObject ScaleImageTo8Bit(HObject image)
    {
        HOperatorSet.GetImageType(image, out var type); // 对于多通道输入图像，返回第一个通道的类型
        using var _ = type;

        if (type == "byte") return Copy(image);

        HOperatorSet.ScaleImageMax(image, out var resultImage); // 线性拉伸到8位图片0-255
        return resultImage;
    }

    /// <summary>
    /// 图片 逆时针旋转90度
    /// </summary>
    /// <param name="image">图片</param>
    /// <returns>逆时针旋转90度图片</returns>
    public static HObject RotateCounterClockwise90Degree(HObject image)
    {
        HOperatorSet.RotateImage(image, out var rotateImage, 90, "constant");
        return rotateImage;
    }

    /// <summary>
    /// 图片 顺时针旋转90度
    /// </summary>
    /// <param name="image">图片</param>
    /// <returns>顺时针旋转90度图片</returns>
    public static HObject RotateClockwise90Degree(HObject image)
    {
        HOperatorSet.RotateImage(image, out var rotateImage, -90, "constant");
        return rotateImage;
    }

    /// <summary>
    /// 图片 水平翻转
    /// </summary>
    /// <param name="image">图片</param>
    /// <returns>水平翻转图片</returns>
    public static HObject HorizontalFlip(HObject image)
    {
        HOperatorSet.MirrorImage(image, out var mirrorImage, "column");
        return mirrorImage;
    }

    /// <summary>
    /// 图片 垂直翻转
    /// </summary>
    /// <param name="image">图片</param>
    /// <returns>垂直翻转图片</returns>
    public static HObject VerticalFlip(HObject image)
    {
        HOperatorSet.MirrorImage(image, out var mirrorImage, "row");
        return mirrorImage;
    }

    /// <summary>
    /// 获取图片清晰度
    /// </summary>
    /// <param name="image">图片</param>
    /// <returns>清晰读</returns>
    public static double ToQualityScore(HObject image)
    {
        HOperatorSet.RegionToMean(image, image, out var imageMean); // 用平均灰度值绘制区域
        using var _0 = imageMean;

        HOperatorSet.ConvertImageType(imageMean, out var imageMeanConverted, "real"); // 转换图像
        using var _1 = imageMeanConverted;

        HOperatorSet.ConvertImageType(image, out var imageConverted, "real"); // 转换图像, real类型表示浮点数类型
        using var _2 = imageConverted;

        HOperatorSet.SubImage(imageConverted, imageMeanConverted, out var imageSub, 1, 0); // 图像相减, 最后两个参数: 校正系数、校正值
        using var _3 = imageSub;

        HOperatorSet.MultImage(imageSub, imageSub, out var imageResult, 1, 0); // 图像相乘, 最后两个参数: 校正系数、校正值
        using var _4 = imageResult;

        HOperatorSet.Intensity(imageResult, imageResult, out var meanTuple, out var deviationTuple); // 用于计算灰度值的平均值和偏差, 最后两个参数: 区域的平均灰度值, 区域内灰度值的偏差
        using var _5 = meanTuple;
        using var _6 = deviationTuple;

        var quality = meanTuple.D;

        return quality;
    }

    /// <summary>
    /// 获取图像在指定矩形区域内的最小灰度值与最大灰度值及其对应的位置。
    /// </summary>
    /// <param name="image">输入图像（HObject 类型，灰度图）</param>
    /// <param name="rect">感兴趣区域（System.Drawing.Rect）</param>
    /// <returns>最大灰度值及其坐标，最小灰度值及其坐标</returns>
    public static (double MaxGrayValue, Point[] maxGrayPoints, double MinGrayValue, Point[] minGrayPoints) GetMaxMinGrayValue(HObject image, Rect rect)
    {
        using var gray = ToGray(image);
        HOperatorSet.GenRectangle1(out var region, rect.Y, rect.X, rect.Y + rect.Height, rect.X + rect.Width);
        using var _0 = region;

        HOperatorSet.MinMaxGray(region, gray, 0, out var minGrayValue, out var maxGrayValue, out var range);
        using var _1 = minGrayValue;
        using var _2 = maxGrayValue;
        using var _3 = range;

        return (maxGrayValue.D, GetGrayValueRegion(maxGrayValue), minGrayValue.D, GetGrayValueRegion(minGrayValue));

        Point[] GetGrayValueRegion(HTuple grayValue)
        {
            HOperatorSet.Threshold(image, out var thresholdRegion, grayValue, grayValue);
            using var _4 = thresholdRegion;

            HOperatorSet.Intersection(region, thresholdRegion, out var regionIntersection);
            using var _5 = regionIntersection;

            HOperatorSet.Connection(regionIntersection, out var connectedRegions);
            using var _6 = connectedRegions;

            HOperatorSet.AreaCenter(connectedRegions, out var areas, out var yHTuple, out var xHTuple);
            using var _7 = areas;
            using var _8 = yHTuple;
            using var _9 = xHTuple;

            return [.. yHTuple.ToDArr().Select((t, i) => new Point(xHTuple[i], t))];
        }
    }

    /// <summary>
    /// 保存更快, 有压缩
    /// </summary>
    /// <param name="image">图片</param>
    /// <param name="filePath">图片路径</param>
    public static void Save(HObject image, string filePath)
    {
        // HOperatorSet.WriteObject(image, "C:\\Users\\Administrator\\Desktop\\test1.hobj");
        DirectoryHelper.CreateFileDirectoryIfNotExists(filePath);
        FileHelper.DeleteFileIfExists(filePath);

        using var scaleImage = ScaleImageTo8Bit(image);

        HOperatorSet.WriteImage(scaleImage, Path.GetExtension(filePath)[1..], 0, filePath);
    }

    #endregion 通用

    #region Bytes

    /// <summary>
    /// 图片的 raw array to HObject(注意:第一次使用慢, 后面快)
    /// </summary>
    /// <param name="bytes">byte raw array, 排列方式都是bgrx顺序</param>
    /// <param name="width">宽度</param>
    /// <param name="height">高度</param>
    /// <param name="channels">通道数</param>
    /// <returns>HObject</returns>
    public static HObject ImageRawBytesToHObject(byte[] bytes, int width, int height, int channels)
    {
        // HObject.Key // 非托管指针, hObject.Dispose() 释放非托管指针 // 可以反复Dispose, 查看反编译源码
        unsafe
        {
            fixed (byte* ptr = bytes)
            {
                switch (channels)
                {
                    case 1:
                        HOperatorSet.GenImage1(out var channel1Image, "byte", width, height, new IntPtr(ptr)); // net8.0 nint
                        return channel1Image;

                    case 3:
                        HOperatorSet.GenImageInterleaved(out var channel3Image, new IntPtr(ptr), "bgr", width, height, 0, "byte", 0, 0, 0, 0, -1, 0);
                        return channel3Image;

                    case 4:
                        HOperatorSet.GenImageInterleaved(out var channel4Image, new IntPtr(ptr), "bgrx", width, height, 0, "byte", 0, 0, 0, 0, -1, 0);
                        return channel4Image;

                    default:
                        return ThrowHelper.ThrowArgumentException<HObject>("Unsupported channels");
                }
            }
        }
    }

    #endregion Bytes

    #region 模板匹配

    /// <summary>
    /// 生成Sharpe模板
    /// </summary>
    /// <param name="image">图片</param>
    /// <param name="templateFilePath">Sharpe模板文件路径</param>
    public static void SaveSharpeTemplate(HObject image, string templateFilePath)
    {
        DirectoryHelper.CreateFileDirectoryIfNotExists(templateFilePath);
        FileHelper.DeleteFileIfExists(templateFilePath);

        HOperatorSet.CreateShapeModel(image, "auto", -2, 2, "auto", "auto", "use_polarity", 30, 10, out var modelId);
        HOperatorSet.WriteShapeModel(modelId, templateFilePath);
        using var _ = modelId;
    }

    /// <summary>
    /// 读取Sharpe模板
    /// </summary>
    /// <param name="templateFilePath">Sharpe模板文件路径</param>
    /// <returns>Sharpe模板</returns>
    public static HTuple ReadSharpeTemplate(string templateFilePath)
    {
        HOperatorSet.ReadShapeModel(templateFilePath, out var templateId); // 读取模板
        return templateId;
    }

    /// <summary>
    /// 清理Sharpe模板
    /// </summary>
    /// <param name="templateId">Sharpe模板</param>
    public static void CleanSharpeTemplate(HTuple templateId)
    {
        HOperatorSet.ClearShapeModel(templateId);
    }

    /// <summary>
    /// 模板匹配Sharpe模板
    /// </summary>
    /// <param name="image">图片</param>
    /// <param name="templateId">Sharpe模板</param>
    /// <param name="templatePoint">Sharpe模板匹配的位置</param>
    /// <param name="score">匹配得分</param>
    /// <param name="angle">匹配角度</param>
    /// <returns>是否成功</returns>
    public static bool TrySharpeTemplateMathToOffset(HObject image, HTuple templateId, out Point templatePoint, out double score, out double angle)
    {
        templatePoint = new Point();
        score = 0;
        angle = 0;

        using var scaleImageTo8Bit = ScaleImageTo8Bit(image);
        HOperatorSet.FindShapeModel(scaleImageTo8Bit, templateId, -5, 5, 0.3, 1, 0.2, "least_squares", 0, 0.9, out var yHTuple, out var xHTuple, out var angleHTuple, out var scoreHTuple);

        using var _0 = yHTuple;
        using var _1 = xHTuple;
        using var _2 = angleHTuple;
        using var _3 = scoreHTuple;
        if (xHTuple.Length == 0 || yHTuple.Length == 0 || scoreHTuple.Length == 0 || angleHTuple.Length == 0) return false;

        templatePoint = new Point(xHTuple.D, yHTuple.D);
        score = scoreHTuple.D;
        angle = angleHTuple.D;

        return false;
    }

    /// <summary>
    /// 生成NCC模板
    /// </summary>
    /// <param name="image">图片</param>
    /// <param name="templateFilePath">NCC模板文件路径</param>
    public static void SaveNccTemplate(HObject image, string templateFilePath)
    {
        DirectoryHelper.CreateFileDirectoryIfNotExists(templateFilePath);
        FileHelper.DeleteFileIfExists(templateFilePath);

        var gray = ToGray(image);

        HOperatorSet.CreateNccModel(gray, "auto", -2, 2, "auto", "use_polarity", out var modelId);
        HOperatorSet.WriteNccModel(modelId, templateFilePath);
        using var _ = modelId;
    }

    /// <summary>
    /// 读取NCC模板
    /// </summary>
    /// <param name="templateFilePath">NCC模板文件路径</param>
    /// <returns>NCC模板</returns>
    public static HTuple ReadNccTemplate(string templateFilePath)
    {
        HOperatorSet.ReadNccModel(templateFilePath, out var templateId);
        return templateId;
    }

    /// <summary>
    /// 清理NCC模板
    /// </summary>
    /// <param name="templateId">NCC模板</param>
    public static void CleanNccTemplate(HTuple templateId)
    {
        HOperatorSet.ClearNccModel(templateId);
    }

    /// <summary>
    /// 模板匹配NCC模板
    /// </summary>
    /// <param name="image">图片</param>
    /// <param name="templateId">NCC模板</param>
    /// <param name="templatePoint">NCC模板匹配的位置</param>
    /// <param name="score">匹配得分</param>
    /// <param name="angle">匹配角度</param>
    /// <returns>是否成功</returns>
    public static bool TryNccTemplateMathToOffset(HObject image, HTuple templateId, out Point templatePoint, out double score, out double angle)
    {
        templatePoint = new Point();
        score = 0;
        angle = 0;

        using var scaleImageTo8Bit = ScaleImageTo8Bit(image);
        HOperatorSet.FindNccModel(scaleImageTo8Bit, templateId, -5, 5, 0.3, 1, 0.2, "true", 0, out var yHTuple, out var xHTuple, out var angleHTuple, out var scoreHTuple);

        using var _0 = yHTuple;
        using var _1 = xHTuple;
        using var _2 = angleHTuple;
        using var _3 = scoreHTuple;
        if (xHTuple.Length == 0 || yHTuple.Length == 0 || scoreHTuple.Length == 0 || angleHTuple.Length == 0) return false;

        templatePoint = new Point(xHTuple.D, yHTuple.D);
        score = scoreHTuple.D;
        angle = angleHTuple.D;

        return false;
    }

    #endregion 模板匹配

    #region 绘图

    /// <summary>
    /// 创建缓存窗口
    /// </summary>
    /// <param name="size">大小</param>
    /// <returns>缓存窗口</returns>
    public static HTuple CreateBufferWindow(Size size)
    {
        HOperatorSet.OpenWindow(0, 0, size.Width, size.Height, 0, "buffer", string.Empty, out var window);
        return window;
    }

    /// <summary>
    /// 关闭窗口
    /// </summary>
    /// <param name="window">窗口</param>
    public static void CloseWindow(HTuple window)
    {
        HOperatorSet.CloseWindow(window);
    }

    /// <summary>
    /// 绘制圆形
    /// </summary>
    /// <param name="window">窗口</param>
    /// <param name="point">圆心</param>
    /// <param name="radius">搬家</param>
    /// <param name="color">颜色</param>
    public static void FillCircle(HTuple window, Point point, int radius, string color)
    {
        HOperatorSet.SetColor(window, color);
        HOperatorSet.DispCircle(window, point.X, point.Y, radius);
    }

    /// <summary>
    /// 窗口转图片
    /// </summary>
    /// <param name="window">窗口</param>
    /// <returns>图片</returns>
    public static HObject WindowToHObject(HTuple window)
    {
        HOperatorSet.DumpWindowImage(out var image, window);
        return image;
    }

    /// <summary>
    /// 图片中心画十字线
    /// </summary>
    /// <param name="image">图片</param>
    /// <returns>图片中心画十字线</returns>
    public static HObject DrawCrossLine(HObject image)
    {
        var size = GetSize(image);
        return DrawCrossLine(image, (Point)(size / 2d));
    }

    /// <summary>
    /// 图片画十字线
    /// </summary>
    /// <param name="image">图片</param>
    /// <param name="point">十字线位置</param>
    /// <returns>图片画十字线</returns>
    public static HObject DrawCrossLine(HObject image, Point point)
    {
        var size = GetSize(image);

        HOperatorSet.OpenWindow(0, 0, size.Width, size.Height, 0, "invisible", "", out var window);
        using var _0 = window;

        HOperatorSet.SetPart(window, 0, 0, size.Height - 1, size.Width - 1);
        HOperatorSet.DispObj(image, window);

        HOperatorSet.SetColor(window, "red");
        HOperatorSet.SetLineWidth(window, 1);

        HOperatorSet.GenCrossContourXld(out var crossImage, point.Y, point.X, Math.Min(size.Width / 4, size.Height / 4), 0);
        using var _1 = crossImage;
        HOperatorSet.DispObj(crossImage, window);

        HOperatorSet.DumpWindowImage(out var resultImage, window);
        HOperatorSet.CloseWindow(window);

        return resultImage;
    }

    /// <summary>
    /// 图片中心画矩形
    /// </summary>
    /// <param name="image">图片</param>
    /// <param name="rect">矩形</param>
    /// <returns>图片中心画矩形</returns>
    public static HObject DrawRect(HObject image, Rect rect)
    {
        var size = GetSize(image);
        HOperatorSet.OpenWindow(0, 0, size.Width, size.Height, 0, "invisible", "", out var window);
        using var _0 = window;

        HOperatorSet.SetPart(window, 0, 0, size.Height - 1, size.Width - 1);
        HOperatorSet.DispObj(image, window);

        HOperatorSet.SetColor(window, "red");
        HOperatorSet.SetLineWidth(window, 1);

        HOperatorSet.GenRectangle1(out var crossImage, rect.Y, rect.X, rect.Y + rect.Height, rect.X + rect.Width);
        using var _1 = crossImage;
        HOperatorSet.SetDraw(window, "margin");
        HOperatorSet.DispObj(crossImage, window);

        HOperatorSet.DumpWindowImage(out var resultImage, window);
        HOperatorSet.CloseWindow(window);

        return resultImage;
    }

    #endregion 绘图
}