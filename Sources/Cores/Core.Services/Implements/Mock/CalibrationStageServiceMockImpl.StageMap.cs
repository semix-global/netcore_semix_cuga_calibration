using Core.Models.Helper;
using Core.Models.Models.Common.StageMap;
using Semix.CoreLib;

namespace Core.Services.Implements.Mock;

public sealed partial class CalibrationStageServiceMockImpl
{
    public SxExecuteRet<bool> ToggleEnableStageMap(bool isEnable) => SxExecuteRetHelper.CreateSuccess(true);

    public SxExecuteRet<bool> SetStageMap(StageMapErrorDTO stageMapError) => SxExecuteRetHelper.CreateSuccess(true);
}