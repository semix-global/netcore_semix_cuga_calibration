using Core.Models.Models.Common.StageMap;
using Semix.CoreLib;

namespace Core.Services.Interfaces;

public partial interface ICalibrationStageService
{
    /// <summary>
    /// 补偿矩阵开关
    /// </summary>
    /// <param name="isEnable">是否启用</param>
    /// <returns>启用是否成功</returns>
    SxExecuteRet<bool> ToggleEnableStageMap(bool isEnable);

    /// <summary>
    /// 使用 Cuga ACS ErrorMap 结构设置 StageMap。
    /// </summary>
    /// <param name="stageMapError">Cuga ACS ErrorMap</param>
    /// <returns>是否成功</returns>
    SxExecuteRet<bool> SetStageMap(StageMapErrorDTO stageMapError);
}