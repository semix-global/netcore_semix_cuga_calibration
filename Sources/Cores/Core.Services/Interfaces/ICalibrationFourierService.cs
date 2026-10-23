using Core.Models.Models.Common.Pattern;
using Net.Utilities.Graphics.Primitives.Medias.Imaging;
using Net.Utilities.Models.Geometries;
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
    /// 通道回零
    /// </summary>
    /// <param name="channelId">1(channel 1), 2(channel 2), 3(channel 3)</param>
    /// <param name="isGoToZero">是否去零点</param>
    SxExecuteRet<bool> Home(int channelId, bool isGoToZero = true);

    /// <summary>
    /// 设置通道1或2挡杆位置
    /// </summary>
    /// <param name="channelId">1(channel 1), 2(channel 2)</param>
    /// <param name="rodPositions">挡杆位置，下标为杆号</param>
    SxExecuteRet<bool> SetRods(int channelId, double[] rodPositions);

    /// <summary>
    /// 获取傅里叶相机的图片
    /// </summary>
    /// <param name="productivityInformation">生产率信息</param>
    /// <param name="laserLightInformation">激光光强</param>
    /// <param name="dfPosition">暗场位置</param>
    /// <param name="scanLength">扫描长度</param>
    /// <param name="isCustomWaveform">是否自定义波形</param>
    /// <param name="channelId">1(channel 1), 2(channel 2), 3(channel 3)</param>
    /// <returns>傅里叶相机的图片</returns>
    SxExecuteRet<BitmapImage> GetImage(
        ProductivityInformation productivityInformation,
        LaserLightInformation laserLightInformation,
        Point dfPosition,
        double scanLength,
        int channelId,
        bool isCustomWaveform);

    /// <summary>
    /// 获取傅里叶电机的配置
    /// </summary>
    /// <returns>傅里叶电机的配置</returns>
    SxExecuteRet<FourierMotorConfiguration> GetMotorConfiguration();

    /// <summary>
    /// 设置通道3的X转盘
    /// </summary>
    /// <param name="angleMotorAbsoluteValue">角度电机绝对值</param>
    /// <param name="motorAbsoluteValue">位置电机绝对值</param>
    /// <returns>是否成功</returns>
    SxExecuteRet<bool> SetChannel3XTurntable(double angleMotorAbsoluteValue, double motorAbsoluteValue);

    /// <summary>
    /// 设置通道3的X推杆位置
    /// </summary>
    /// <param name="motorAbsoluteValue">位置电机绝对值</param>
    /// <returns>是否成功</returns>
    SxExecuteRet<bool> SetChannel3XPush(double motorAbsoluteValue);

    /// <summary>
    /// 设置通道3的Y转盘
    /// </summary>
    /// <param name="angleMotorAbsoluteValue">角度电机绝对值</param>
    /// <param name="motorAbsoluteValue">位置电机绝对值</param>
    /// <returns>是否成功</returns>
    SxExecuteRet<bool> SetChannel3YTurntable(double angleMotorAbsoluteValue, double motorAbsoluteValue);

    /// <summary>
    /// 傅里叶电机配置
    /// </summary>
    /// <param name="SideRodTotalCount">侧通道挡杆数量</param>
    /// <param name="SideMinMotorAbsoluteValue">侧通道挡杆最小位置电机绝对值</param>
    /// <param name="SideMaxMotorAbsoluteValue">侧通道挡杆最大位置电机绝对值</param>
    /// <param name="CenterXMinMotorAbsoluteValue">中心通道X转盘最小位置电机绝对值</param>
    /// <param name="CenterXMaxMotorAbsoluteValue">中心通道X转盘最大位置电机绝对值</param>
    /// <param name="CenterPushMinMotorAbsoluteValue">中心通道X推杆最小位置电机绝对值</param>
    /// <param name="CenterPushMaxMotorAbsoluteValue">中心通道X推杆最大位置电机绝对值</param>
    /// <param name="CenterYMinMotorAbsoluteValue">中心通道Y转盘最小位置电机绝对值</param>
    /// <param name="CenterYMaxMotorAbsoluteValue">中心通道Y转盘最大位置电机绝对值</param>
    public readonly record struct FourierMotorConfiguration(
        int SideRodTotalCount,
        double SideMinMotorAbsoluteValue,
        double SideMaxMotorAbsoluteValue,
        double CenterXMinMotorAbsoluteValue,
        double CenterXMaxMotorAbsoluteValue,
        double CenterPushMinMotorAbsoluteValue,
        double CenterPushMaxMotorAbsoluteValue,
        double CenterYMinMotorAbsoluteValue,
        double CenterYMaxMotorAbsoluteValue);
}