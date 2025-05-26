using Semix.CoreLib;

namespace Core.Services.Interfaces;

/// <summary>
/// 缓震平台服务
/// </summary>
public interface ICalibrationAdsService
{
    /// <summary>
    /// 连接
    /// </summary>
    /// <returns>是否成功</returns>
    SxExecuteRet<bool> Connect();

    /// <summary>
    /// X根据速度获取传感器: 速度前馈系数
    /// </summary>
    /// <param name="isPositive">是否是正向</param>
    /// <returns>前馈系数</returns>
    SxExecuteRet<(double X1, double X2)> GetSensorXSpeedFeedForwardValue(bool isPositive);

    /// <summary>
    /// X根据速度设置传感器: 速度前馈系数
    /// </summary>
    /// <param name="isPositive">是否是正向</param>
    /// <param name="value">速度前馈系数</param>
    /// <returns>是否成功</returns>
    SxExecuteRet<bool> SetSensorXSpeedFeedForwardValue(bool isPositive, (double X1, double X2) value);

    /// <summary>
    /// Y根据速度获取传感器: 速度前馈系数
    /// </summary>
    /// <param name="isPositive">是否是正向</param>
    /// <returns>前馈系数</returns>
    SxExecuteRet<(double Y1, double Y2, double Y3)> GetSensorYSpeedFeedForwardValue(bool isPositive);

    /// <summary>
    /// Y根据速度设置传感器: 速度前馈系数
    /// </summary>
    /// <param name="isPositive">是否是正向</param>
    /// <param name="value">速度前馈系数</param>
    /// <returns>是否成功</returns>
    SxExecuteRet<bool> SetSensorYSpeedFeedForwardValue(bool isPositive, (double Y1, double Y2, double Y3) value);

    /// <summary>
    /// 根据速度获取传感器: ADS 高度传感器 Z1 Z2 Z3
    /// </summary>
    /// <returns>ADS 高度传感器 Z1 Z2 Z3</returns>
    SxExecuteRet<(double Z1, double Z2, double Z3)> GetSensorSpeedZ1Z2Z3Value();

    /// <summary>
    /// 设置前馈压力传感器
    /// </summary>
    /// <param name="pressureValue1">压力1</param>
    /// <param name="pressureValue2">压力2</param>
    /// <param name="pressureValue3">压力3</param>
    /// <returns>是否成功</returns>
    SxExecuteRet<bool> SetSensorFeedForwardPressureValue(double pressureValue1, double pressureValue2, double pressureValue3);

    /// <summary>
    /// 获取传感器: TransBuffer 压力1、压力2、压力3的Buffer值
    /// </summary>
    /// <param name="timeSpan">多长时间Buffer</param>
    /// <returns>TransBuffer 压力1、压力2、压力3的值</returns>
    SxExecuteRet<List<(double PressureValue1, double PressureValue2, double PressureValue3)>> GetSensorAllPressureTraceBufferList(TimeSpan timeSpan);

    /// <summary>
    /// 获取传感器: TransBuffer 高度、横滚、俯仰的Buffer值
    /// </summary>
    /// <param name="timeSpan">多长时间Buffer</param>
    /// <returns>TransBuffer 高度、横滚、俯仰的值</returns>
    SxExecuteRet<List<(double Height, double Roll, double Pitch)>> GetSensorHeightRollPitchTraceBufferList(TimeSpan timeSpan);

    /// <summary>
    /// 根据速度获取传感器: ADS 高度传感器 Z1 Z2 Z3的Buffer值
    /// </summary>
    /// <param name="timeSpan">多长时间Buffer</param>
    /// <returns>TraceBuffer ADS 高度传感器 Z1 Z2 Z3</returns>
    SxExecuteRet<List<List<double>>> GetSensorSpeedZ1Z2Z3TraceBufferList(TimeSpan timeSpan);

    /// <summary>
    /// 根据速度获取传感器: ADS 高度传感器XyX0、XyX1、XyY0、XyY1的Buffer值
    /// </summary>
    /// <param name="timeSpan">多长时间Buffer</param>
    /// <returns>TraceBuffer ADS 高度传感器 XyX0、XyX1、XyY0、XyY1</returns>
    SxExecuteRet<List<List<double>>> GetSensorSpeedX0X1Y0Y1WithSpeedTraceBufferList(bool isAxisX, TimeSpan timeSpan);

    /// <summary>
    /// 设置ADS XY伺服使能
    /// </summary>
    /// <param name="isEnabled">true:On, false:Off</param>
    /// <returns></returns>
    SxExecuteRet<bool> SetAdsXyEnabled(bool isEnabled);
}