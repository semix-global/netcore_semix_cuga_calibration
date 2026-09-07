using Core.Models.Enums.Optics;
using Core.Models.Models.Common.Pattern;
using Semix.CoreLib;

namespace Core.Services.Interfaces;

public interface ICalibrationOpticsService
{
    /// <summary>
    /// 连接
    /// </summary>
    /// <returns>是否成功</returns>
    SxExecuteRet<bool> Connect();

    /// <summary>
    /// 获取cuga配置的产率列表
    /// </summary>
    /// <returns>cuga配置的产率列表</returns>
    SxExecuteRet<IReadOnlyList<ProductivityInformation>> GetProductivityInformations();

    /// <summary>
    /// 读取DOE电机绝对值
    /// <param name="opticsIlluminationModeEnum">照明方式</param>
    /// </summary>
    /// <returns>返回电机位置</returns>
    SxExecuteRet<double> GetDOEMotorAbsoluteValue(OpticsIlluminationModeEnum opticsIlluminationModeEnum);

    /// <summary>
    /// 下发DOE电机绝对位置
    /// </summary>
    /// <param name="opticsIlluminationModeEnum">照明方式</param>
    /// <param name="value">电机位置</param>
    /// <returns>返回是否下发成功</returns>
    SxExecuteRet<bool> SetDOEMotorAbsoluteValue(OpticsIlluminationModeEnum opticsIlluminationModeEnum, double value);

    /// <summary>
    /// 获取Relay电机位置
    /// </summary>
    /// <param name="opticsIlluminationModeEnum">照明光入射方式</param>
    /// <returns>Relay电机位置mm</returns>
    SxExecuteRet<double> GetRelayMotorAbsoluteValue(OpticsIlluminationModeEnum opticsIlluminationModeEnum);

    /// <summary>
    /// 设置Relay电机位置
    /// </summary>
    /// <param name="opticsIlluminationModeEnum">照明光入射方式</param>
    /// <param name="value">Relay电机位置mm</param>
    /// <returns>是否成功</returns>
    SxExecuteRet<bool> SetRelayMotorAbsoluteValue(OpticsIlluminationModeEnum opticsIlluminationModeEnum, double value);

    /// <summary>
    /// 获取INC电机位置
    /// </summary>
    /// <param name="opticsIlluminationModeEnum">照明光入射方式</param>
    /// <returns>INC电机位置mm</returns>
    SxExecuteRet<double> GetINCMotorAbsoluteValue(OpticsIlluminationModeEnum opticsIlluminationModeEnum);

    /// <summary>
    /// 设置INC电机位置
    /// </summary>
    /// <param name="opticsIlluminationModeEnum">照明光入射方式</param>
    /// <param name="value">INC电机位置mm</param>
    /// <returns>是否成功</returns>
    SxExecuteRet<bool> SetINCMotorAbsoluteValue(OpticsIlluminationModeEnum opticsIlluminationModeEnum, double value);

    /// <summary>
    /// 获取SC电机位置
    /// </summary>
    /// <param name="opticsIlluminationModeEnum">照明光入射方式</param>
    /// <returns>SC电机位置mm</returns>
    SxExecuteRet<(double L1, double L3)> GetSCMotorAbsoluteValue(OpticsIlluminationModeEnum opticsIlluminationModeEnum);

    /// <summary>
    /// 获取PMT Sensor高度
    /// </summary>
    /// <returns>PMT Sensor高度</returns>
    SxExecuteRet<double> GetPMTInterval();

    /// <summary>
    /// 设置SC电机位置
    /// </summary>
    /// <param name="opticsIlluminationModeEnum">照明光入射方式</param>
    /// <param name="value">SC电机位置mm</param>
    /// <returns>是否成功</returns>
    SxExecuteRet<bool> SetSCMotorAbsoluteValue(OpticsIlluminationModeEnum opticsIlluminationModeEnum, (double L1, double L3) value);

    /// <summary>
    /// 获取通道采集偏振电机位置
    /// </summary>
    /// <param name="channelId">通道ID</param>
    /// <returns>通道采集偏振电机位置mm</returns>
    SxExecuteRet<double> GetCollectorPolarizationMotorAbsoluteValue(int channelId);

    /// <summary>
    /// 设置通道采集偏振电机位置
    /// </summary>
    /// <param name="channelId">通道ID</param>
    /// <param name="value">电机值</param>
    /// <returns>是否成功</returns>
    SxExecuteRet<bool> SetCollectorPolarizationMotorAbsoluteValue(int channelId, double value);

    /// <summary>
    /// 切换照明OD滤光片
    /// </summary>
    /// <param name="isEnable">是否开启</param>
    /// <returns>是否成功</returns>
    SxExecuteRet<bool> ToggleODFilter(bool isEnable);

    /// <summary>
    /// 切换照明切趾
    /// </summary>
    /// <returns>偏振</returns>
    SxExecuteRet<OpticsApodizationModeEnum> GetApodizationMode();

    /// <summary>
    /// 切换照明切趾
    /// </summary>
    /// <param name="opticsApodizationModeEnum">偏振</param>
    /// <returns>是否成功</returns>
    SxExecuteRet<bool> SetApodizationMode(OpticsApodizationModeEnum opticsApodizationModeEnum);

    /// <summary>
    /// 切换照明偏振
    /// </summary>
    /// <returns>偏振</returns>
    SxExecuteRet<OpticsPolarizationModeEnum> GetPolarizationMode();

    /// <summary>
    /// 切换照明偏振
    /// </summary>
    /// <param name="opticsPolarizationModeEnum">偏振</param>
    /// <returns>是否成功</returns>
    SxExecuteRet<bool> SetPolarizationMode(OpticsPolarizationModeEnum opticsPolarizationModeEnum);

    /// <summary>
    /// 获取所有采集偏振
    /// </summary>
    /// <returns>偏振</returns>
    SxExecuteRet<OpticsCollectorPolarizationModeEnum> GetCollectorPolarizationMode();

    /// <summary>
    /// 切换所有采集偏振
    /// </summary>
    /// <param name="opticsCollectorPolarizationModeEnum">偏振</param>
    /// <returns>是否成功</returns>
    SxExecuteRet<bool> SetCollectorPolarizationMode(OpticsCollectorPolarizationModeEnum opticsCollectorPolarizationModeEnum);

    /// <summary>
    /// 获取通道采集偏振
    /// </summary>
    /// <param name="channelId">通道ID</param>
    /// <returns>偏振</returns>
    SxExecuteRet<OpticsCollectorPolarizationModeEnum> GetCollectorPolarizationMode(int channelId);

    /// <summary>
    /// 切换通道采集偏振
    /// </summary>
    /// <param name="channelId">通道ID</param>
    /// <param name="opticsCollectorPolarizationModeEnum">偏振</param>
    /// <returns>是否成功</returns>
    SxExecuteRet<bool> SetCollectorPolarizationMode(int channelId, OpticsCollectorPolarizationModeEnum opticsCollectorPolarizationModeEnum);

    /// <summary>
    /// 设置ZOOS遮挡与否，实现单光斑或者所有光斑采图
    /// </summary>
    /// <param name="opticsIlluminationModeEnum"></param>
    /// <param name="enable">True为ZOOS遮挡，单光斑采图，否则为所有光斑采图</param>
    /// <returns>是否成功</returns>
    SxExecuteRet<bool> ToggleZoosClinder(OpticsIlluminationModeEnum opticsIlluminationModeEnum, bool enable);

    /// <summary>
    /// 获取ROOS电机行程范围
    /// </summary>
    /// <returns>(ROOS起点位置mm，ROOS终点位置mm，ROOS控制精度mm）</returns>
    SxExecuteRet<(double StartPos, double EndPos, double Accuracy)> GetROOSMotorRouteRange();

    /// <summary>
    /// 获取ROOS电机位置
    /// </summary>
    /// <returns>ROOS电机位置mm</returns>
    SxExecuteRet<double> GetROOSMotorAbsoluteValue();

    /// <summary>
    /// 设置ROOS电机位置
    /// </summary>
    /// <param name="value">ROOS电机位置mm</param>
    /// <returns>是否成功</returns>
    SxExecuteRet<bool> SetROOSMotorAbsoluteValue(double value);
}