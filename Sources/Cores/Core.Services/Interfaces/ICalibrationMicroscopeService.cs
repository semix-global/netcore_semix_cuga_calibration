using Core.Models.Models.Common.Pattern;
using Cuga.Data.DataStruct.Microscope.Enums;
using Semix.CoreLib;

namespace Core.Services.Interfaces;

public interface ICalibrationMicroscopeService
{
    /// <summary>
    /// 连接
    /// </summary>
    /// <returns>是否成功</returns>
    SxExecuteRet<bool> Connect();

    /// <summary>
    /// 获取cuga配置的显微镜镜头列表
    /// </summary>
    /// <returns>cuga配置的显微镜镜头列表</returns>
    SxExecuteRet<IReadOnlyList<MicroscopeLensInformation>> GetMicroscopeLensInformations();

    /// <summary>
    /// 倍镜类型转换
    /// </summary>
    /// <param name="cgMicroscopeLens">显微镜镜头枚举对象</param>
    /// <returns></returns>
    SxExecuteRet<MicroscopeLensInformation> CgMicroscopeLensToMicroscopeLensInfo(CgMicroscopeLens cgMicroscopeLens);

    /// <summary>
    /// 获取显微镜倍率
    /// </summary>
    /// <returns>显微镜倍率</returns>
    SxExecuteRet<MicroscopeLensInformation> GetCurrentMicroscopeLensInformation();

    /// <summary>
    /// 切换显微镜倍率不切自动聚焦模式
    /// </summary>
    /// <param name="microscopeLensInformation">倍率</param>
    /// <returns>是否成功</returns>
    SxExecuteRet<bool> SwitchMicroscopeLensInformationNotAutoFocus(MicroscopeLensInformation microscopeLensInformation);

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
    /// 下发AF参数
    /// </summary>
    /// <param name="microscopeLensInformation">倍率</param>
    /// <param name="ecs">ecs</param>
    /// <param name="voltage">电压</param>
    /// <returns>是否成功</returns>
    SxExecuteRet<bool> SetAFParams(MicroscopeLensInformation microscopeLensInformation, double ecs, double voltage);
}