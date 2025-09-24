using HalconDotNet;
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
    /// 获取halcon图片
    /// </summary>
    /// <returns>halcon图片</returns>
    SxExecuteRet<HImage> GetBrightFieldImage();

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
}