using Net.Utilities.Algorithms.AlgoCSharp;
using Net.Utilities.Graphics.Extensions;
using Net.Utilities.Graphics.Primitives.Medias.Imaging;
using Net.Utilities.Models.Geometries;

namespace Net.Utilities.Calibration;

/// <summary>
/// BitmapImage 双库分流
/// <para>双库分流结构：以可选参数 <see cref="AlgorithmEngineTypeEnum"/>? 显式指定引擎，
/// 未指定（null）时取 <see cref="AlgorithmEngineConstantHelper"/> 全局默认（默认 HAlgorithm）</para>
/// </summary>
public static class BitmapImageExtensions
{
#pragma warning disable IDISP012

    public static BitmapImage Empty => BitmapImage.Random(1, 1, 0);

#pragma warning restore IDISP012

    extension(BitmapImage @this)
    {
        /// <summary>
        /// 保存图片（大于 8bit 时先拉伸到 8bit 再写文件）
        /// <para>【容差标记】双实现视觉近似：algo 侧用原生 ScaleImageMax（min-max 归一化）替代 Halcon scale_image_max，接受取整差异</para>
        /// </summary>
        /// <param name="filePath">文件路径</param>
        /// <param name="algorithmEngineType">算法引擎（null 时取全局默认 AlgorithmEngineTypeDefaults）</param>
        public void SaveImage(string filePath, AlgorithmEngineTypeEnum? algorithmEngineType = null)
        {
            if ((algorithmEngineType ?? AlgorithmEngineConstantHelper.Current) == AlgorithmEngineTypeEnum.AlgoCSharp)
                BitmapImageAlgoImageOperations.SaveImage(@this, filePath);
            else
                BitmapImageHalconImageOperations.SaveImage(@this, filePath);
        }

        /// <summary>
        /// log 域 12bit → 线性域转换
        /// <para>【容差标记】双实现存在 ±1 灰度系统差：Halcon 为双精度截断 LUT，algo 侧为原生 LogToLinear
        /// （float32 计算四舍五入），差异界以等价测试 ±1 ，接受差异</para>
        /// </summary>
        /// <param name="algorithmEngineType">算法引擎（null 时取全局默认 AlgorithmEngineTypeDefaults）</param>
        /// <returns>线性域图（位深标记与输入一致）</returns>
        public BitmapImage ToLinearImage(AlgorithmEngineTypeEnum? algorithmEngineType = null) =>
            (algorithmEngineType ?? AlgorithmEngineConstantHelper.Current) == AlgorithmEngineTypeEnum.AlgoCSharp
                ? BitmapImageAlgoImageOperations.ToLinearImage(@this)
                : BitmapImageHalconImageOperations.ToLinearImage(@this);

        /// <summary>
        /// 图片逆时针旋转 90 度
        /// </summary>
        /// <param name="algorithmEngineType">算法引擎（null 时取全局默认 AlgorithmEngineTypeDefaults）</param>
        public BitmapImage RotateCounterClockwise90Degree(AlgorithmEngineTypeEnum? algorithmEngineType = null) =>
            (algorithmEngineType ?? AlgorithmEngineConstantHelper.Current) == AlgorithmEngineTypeEnum.AlgoCSharp
                ? BitmapImageAlgoImageOperations.RotateCounterClockwise90Degree(@this)
                : BitmapImageHalconImageOperations.RotateCounterClockwise90Degree(@this);

        /// <summary>
        /// 图片水平翻转
        /// </summary>
        /// <param name="algorithmEngineType">算法引擎（null 时取全局默认 AlgorithmEngineTypeDefaults）</param>
        public BitmapImage HorizontalFlip(AlgorithmEngineTypeEnum? algorithmEngineType = null) =>
            (algorithmEngineType ?? AlgorithmEngineConstantHelper.Current) == AlgorithmEngineTypeEnum.AlgoCSharp
                ? BitmapImageAlgoImageOperations.HorizontalFlip(@this)
                : BitmapImageHalconImageOperations.HorizontalFlip(@this);

        /// <summary>
        /// 图片垂直翻转
        /// </summary>
        /// <param name="algorithmEngineType">算法引擎（null 时取全局默认 AlgorithmEngineTypeDefaults）</param>
        public BitmapImage VerticalFlip(AlgorithmEngineTypeEnum? algorithmEngineType = null) =>
            (algorithmEngineType ?? AlgorithmEngineConstantHelper.Current) == AlgorithmEngineTypeEnum.AlgoCSharp
                ? BitmapImageAlgoImageOperations.VerticalFlip(@this)
                : BitmapImageHalconImageOperations.VerticalFlip(@this);

        /// <summary>
        /// 在指定位置绘制十字
        /// </summary>
        /// <param name="point">十字中心</param>
        /// <param name="algorithmEngineType">算法引擎（null 时取全局默认 AlgorithmEngineTypeDefaults）</param>
        public BitmapImage DrawCrossLine(Point point, AlgorithmEngineTypeEnum? algorithmEngineType = null) =>
            (algorithmEngineType ?? AlgorithmEngineConstantHelper.Current) == AlgorithmEngineTypeEnum.AlgoCSharp
                ? BitmapImageAlgoImageOperations.DrawCrossLine(@this, point)
                : BitmapImageHalconImageOperations.DrawCrossLine(@this, point);

        /// <summary>
        /// 按照 ROI 裁剪图片
        /// </summary>
        /// <param name="rect">Roi</param>
        /// <param name="algorithmEngineType">算法引擎（null 时取全局默认 AlgorithmEngineTypeDefaults）</param>
        public BitmapImage ToROI(Rect rect, AlgorithmEngineTypeEnum? algorithmEngineType = null) =>
            (algorithmEngineType ?? AlgorithmEngineConstantHelper.Current) == AlgorithmEngineTypeEnum.AlgoCSharp
                ? BitmapImageAlgoImageOperations.ToROI(@this, rect)
                : BitmapImageHalconImageOperations.ToROI(@this, rect);

        /// <summary>
        /// 获取图片均值与总体标准差
        /// </summary>
        /// <param name="algorithmEngineType">算法引擎（null 时取全局默认 AlgorithmEngineTypeDefaults）</param>
        public (double Average, double Deviation) GetIntensity(AlgorithmEngineTypeEnum? algorithmEngineType = null) =>
            (algorithmEngineType ?? AlgorithmEngineConstantHelper.Current) == AlgorithmEngineTypeEnum.AlgoCSharp
                ? BitmapImageAlgoImageOperations.GetIntensity(@this)
                : BitmapImageHalconImageOperations.GetIntensity(@this);

        // todo:Halcon sub_image 钳位语义；algo Subtract16S无法等价替换,只有「Int16 + 只要纯差分、不要显示偏置」时，才接近；无符号 16 位 / 需要 Add/Mult / 依赖钳到 0～65535 的旧行为，都不能等价
        /// <summary>
        /// 图片相减
        /// </summary>
        /// <param name="subImage">减图</param>
        /// <returns>相减结果</returns>
        public BitmapImage SubImage(BitmapImage subImage) => BitmapImageHalconImageOperations.SubImage(@this, subImage);

        /// <summary>
        /// 获取图片的水平方向投影
        /// </summary>
        /// <param name="algorithmEngineType">算法引擎（null 时取全局默认 AlgorithmEngineTypeDefaults）</param>
        /// <returns>水平方向投影</returns>
        public double[] GetHorizontalProjects(AlgorithmEngineTypeEnum? algorithmEngineType = null) =>
            (algorithmEngineType ?? AlgorithmEngineConstantHelper.Current) == AlgorithmEngineTypeEnum.AlgoCSharp
                ? BitmapImageAlgoImageOperations.GetHorizontalProjects(@this)
                : BitmapImageHalconImageOperations.GetHorizontalProjects(@this);

        /// <summary>
        /// 获取图片在指定矩形区域内的最小灰度值与最大灰度值及其对应的位置
        /// </summary>
        /// <param name="rect">感兴趣区域</param>
        /// <returns>最大灰度值及其坐标，最小灰度值及其坐标</returns>
        public (double MaxGrayValue, Point[] maxGrayPoints, double MinGrayValue, Point[] minGrayPoints) GetMaxMinGrayValue(Rect rect) =>
            BitmapImageHalconImageOperations.GetMaxMinGrayValue(@this, rect);
    }
}
