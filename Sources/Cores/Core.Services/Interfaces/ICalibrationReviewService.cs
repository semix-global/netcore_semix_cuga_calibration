using Net.Utilities.Graphics.Primitives.Medias.Imaging;
using Net.Utilities.Models.Geometries;
using Semix.CoreLib;

namespace Core.Services.Interfaces;

public interface ICalibrationReviewService
{
    /// <summary>
    /// 连接
    /// </summary>
    /// <returns>是否成功</returns>
    SxExecuteRet<bool> Connect();

    /// <summary>
    /// 获取明场相机的图片
    /// </summary>
    /// <returns>明场相机的图片</returns>
    SxExecuteRet<BitmapImage> GetBrightFieldImage();

    /// <summary>
    /// 获取bitmap memory byte array
    /// </summary>
    /// <returns>bitmap memory byte array</returns>
    SxExecuteRet<byte[]> GetBrightFieldImageMemoryByteArray();

    /// <summary>
    /// 相机尺寸
    /// </summary>
    /// <returns>是否成功</returns>
    SxExecuteRet<Size> GetBrightFieldImagePixelSize();

    /// <summary>
    /// 读取当前review相机配置的像元像素尺寸
    /// </summary>
    /// <returns>PixelSize</returns>
    SxExecuteRet<Size> GetDefaultPixelSize();
}