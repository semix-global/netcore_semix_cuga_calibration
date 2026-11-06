using algocv_sharp;
using Net.Utilities.Models.Geometries;

namespace Net.Utilities.Algorithms.AlgoCSharp;

/// <summary>
/// 12bit PMT raw 图工厂（algo api 底座）。
/// <para>与 Halcon 实现的 RAWImageFactory/RAWImageFactoryV2 按引擎分底座：V2 服务 HAlgorithm 链路（Halcon 原生产 HImage），
/// V3 服务 AlgoCSharp 链路及 DarkFieldImageDTO 显示图（algo 原生产 Image/BitmapImage）。</para>
/// <para>方向协议：raw 直读 = 竖图（原始形态）；<see cref="CreateImage(string, bool)"/> 产物 = 物理正向图（世界口径宽高）。
/// log 域产物与 V2.CreateImage 逐像素相等；线性域用 algolib 现成的 ImageProc.LogToLinear（float32 取整），
/// 与 V2 的双精度截断 LUT 存在 ±1 灰度精度差（非语义差）。</para>
/// </summary>
public static class RAWImageFactoryV3
{
    /// <summary>
    /// 获取 raw 文件尺寸（纯协议头解析，不解码像素，等价 V2.GetSize）。
    /// <para>ParseRawImageInfo 返回竖图口径（宽高为原始形态），V2 为世界口径，此处翻转对齐。</para>
    /// </summary>
    /// <param name="filePath">raw image file path</param>
    /// <returns>(尺寸, body 字节起始索引, body 字节长度)</returns>
    public static (SizeI Size, long BodyBytesStartIndex, long BodyBytesLength) GetSize(string filePath)
    {
        var info = ImageProc.ParseRawImageInfo(File.ReadAllBytes(filePath));

        return (new SizeI((int)info.height, (int)info.width), info.body_bytes_start_index, info.body_bytes_length);
    }

    /// <summary>
    /// 竖图出口（零重排）：raw 直读原形态（竖图，log 域 12bit），供需要竖图的引擎路径使用；
    /// 与 <see cref="CreateImage(string, bool)"/> 互为转置。
    /// </summary>
    /// <param name="filePath">raw image file path</param>
    /// <returns>竖图（用 using 管理释放）</returns>
    public static Image CreateProtocolImage(string filePath) => new(filePath);

    /// <summary>
    /// 创建物理正向图（世界口径宽高，语义对齐 V2.CreateImage）：raw 直读为竖图，经转置得到物理正向图。
    /// <para>线性化用 algolib 现成的 <see cref="ImageProc.LogToLinear(Image, ImageDataType)"/>（2026-10-23 裁定：
    /// 优先用现成方法，不自造轮子），其 float32 计算取整与 V2 的双精度截断 LUT 存在 ±1 灰度精度差。</para>
    /// </summary>
    /// <param name="filePath">raw image file path</param>
    /// <param name="isToLinear">是否转换到线性域（log 域 12bit → 线性域 16bit）</param>
    /// <returns>物理正向图（用 using 管理释放）</returns>
    public static Image CreateImage(string filePath, bool isToLinear)
    {
        using var protocolImage = CreateProtocolImage(filePath);
        var forwardImage = ImageProc.Transpose(protocolImage);

        if (isToLinear == false) return forwardImage;

        using (forwardImage)
        {
            return ImageProc.LogToLinear(forwardImage);
        }
    }
}
