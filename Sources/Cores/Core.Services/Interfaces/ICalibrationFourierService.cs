using HalconDotNet;
using Semix.CoreLib;

namespace Core.Services.Interfaces;

public interface ICalibrationFourierService
{
    /// <summary>
    /// 连接
    /// </summary>
    /// <returns>是否成功</returns>
    SxExecuteRet<bool> Connect();

    /// <summary>
    /// 获取傅里叶相机的图片
    /// </summary>
    /// <param name="channelId">通道ID</param>
    /// <returns>傅里叶相机的图片</returns>
    SxExecuteRet<HImage> GetFourierImage(int channelId);
}