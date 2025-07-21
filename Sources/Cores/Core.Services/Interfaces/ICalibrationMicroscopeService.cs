using Core.Models.Models.Pattern;
using Cuga.Data.DataStruct.Microscope;
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
    /// 倍镜类型转换
    /// </summary>
    /// <param name="microscopeMagnificationInfo">倍镜信息对象</param>
    /// <returns></returns>
    SxExecuteRet<CgMicroscopeLens> MicroscopeMagnificationInfoToCgMicroscopeLens(MicroscopeMagnificationInfo microscopeMagnificationInfo);

    /// <summary>
    /// 倍镜类型转换
    /// </summary>
    /// <param name="cgMicroscopeLens">显微镜镜头枚举对象</param>
    /// <returns></returns>
    SxExecuteRet<MicroscopeMagnificationInfo> CgMicroscopeLensToMicroscopeMagnificationInfo(CgMicroscopeLens cgMicroscopeLens);


    /// <summary>
    /// 获取显微镜倍率
    /// </summary>
    /// <returns>显微镜倍率</returns>
    SxExecuteRet<MicroscopeMagnificationInfo> GetMagnification();

    /// <summary>
    /// 切换显微镜倍率
    /// </summary>
    /// <param name="microscopeMagnificationInfo">倍率</param>
    /// <returns>是否成功</returns>
    SxExecuteRet<bool> SwitchMagnification(MicroscopeMagnificationInfo microscopeMagnificationInfo);

    /// <summary>
    /// 切换显微镜倍率不切自动聚焦模式
    /// </summary>
    /// <param name="microscopeMagnificationInfo">倍率</param>
    /// <returns>是否成功</returns>
    SxExecuteRet<bool> SwitchMagnificationNotAutoFocus(MicroscopeMagnificationInfo microscopeMagnificationInfo);

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

    /// <summary>
    /// 获取cuga配置的倍镜列表
    /// </summary>
    /// <returns></returns>
    SxExecuteRet<List<CgMicroscopeInfo>> GetLensList();
}