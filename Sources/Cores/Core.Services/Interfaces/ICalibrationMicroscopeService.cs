using Core.Models.Enums.Microscope;
using Cuga.Data.DataStruct.Microscope.Enums;
using Semix.CoreLib;

namespace Core.Services.Interfaces;

/// <summary>
/// 显微镜服务
/// </summary>
public interface ICalibrationMicroscopeService
{
    /// <summary>
    /// 连接
    /// </summary>
    /// <returns>是否成功</returns>
    SxExecuteRet<bool> Connect();

    /// <summary>
    /// 枚举互转
    /// </summary>
    /// <param name="microscopeMagnificationEnum">放大倍率</param>
    /// <returns>枚举类型互相转换</returns>
    SxExecuteRet<CgMicroscopeLens> MicroscopeMagnificationEnumToCgMicroscopeLens(MicroscopeMagnificationEnum microscopeMagnificationEnum);

    /// <summary>
    /// 枚举互转
    /// </summary>
    /// <param name="cgMicroscopeLens">枚举类型互相转换</param>
    /// <returns>放大倍率</returns>
    SxExecuteRet<MicroscopeMagnificationEnum> CgMicroscopeLensToMicroscopeMagnificationEnum(CgMicroscopeLens cgMicroscopeLens);

    /// <summary>
    /// 获取显微镜倍率
    /// </summary>
    /// <returns>显微镜倍率</returns>
    SxExecuteRet<MicroscopeMagnificationEnum> GetMagnification();

    /// <summary>
    /// 切换显微镜倍率
    /// </summary>
    /// <param name="microscopeMagnificationEnum">倍率</param>
    /// <returns>是否成功</returns>
    SxExecuteRet<bool> SwitchMagnification(MicroscopeMagnificationEnum microscopeMagnificationEnum);

    /// <summary>
    /// 切换显微镜倍率不切自动聚焦模式
    /// </summary>
    /// <param name="microscopeMagnificationEnum">倍率</param>
    /// <returns>是否成功</returns>
    SxExecuteRet<bool> SwitchMagnificationNotAutoFocus(MicroscopeMagnificationEnum microscopeMagnificationEnum);

    /// <summary>
    /// 设置显微镜电压
    /// </summary>
    /// <param name="voltage">电压</param>
    /// <returns>是否成功</returns>
    SxExecuteRet<bool> SetVoltage(double voltage);

    /// <summary>
    /// 读取显微镜电压
    /// </summary>
    /// <returns>电压值</returns>
    SxExecuteRet<double> GetVoltage();

    /// <summary>
    /// 读取显微镜电压上下限
    /// </summary>
    /// <returns>是否成功</returns>
    SxExecuteRet<(double min, double max)> GetVoltageRange();
}