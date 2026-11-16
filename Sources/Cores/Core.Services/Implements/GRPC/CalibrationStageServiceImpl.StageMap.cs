using Core.Models.Helper;
using Core.Models.Models.Common.StageMap;
using Cuga.Data.DataStruct.Stage;
using Semix.CoreLib;

namespace Core.Services.Implements.GRPC;

public sealed partial class CalibrationStageServiceImpl
{
    public SxExecuteRet<bool> ToggleEnableStageMap(bool isEnable)
    {
        var sxExecuteRet = Invoke(() => Service?.ToggleEnableStageMap(new SxParamObj<bool>(isEnable)));
        if (sxExecuteRet.IsSuccess == false) return SxExecuteRetHelper.CreateError(sxExecuteRet.Msg, false);

        if (isEnable == false)
        {
            sxExecuteRet = Invoke(() => Service?.CloseStageMap());
            if (sxExecuteRet.IsSuccess == false) return SxExecuteRetHelper.CreateError(sxExecuteRet.Msg, false);
        }

        return sxExecuteRet.IsSuccess == false
            ? SxExecuteRetHelper.CreateError(sxExecuteRet.Msg, false)
            : SxExecuteRetHelper.CreateSuccess(true);
    }

    public SxExecuteRet<bool> SetStageMap(StageMapErrorDTO stageMapError)
    {
        var sxExecuteRet = Invoke(() => Service?.SetStageMap(new SxParamObj<CgErrorMap>(stageMapError.AdaptTo())));

        return sxExecuteRet.IsSuccess == false
            ? SxExecuteRetHelper.CreateError(sxExecuteRet.Msg, false)
            : SxExecuteRetHelper.CreateSuccess(true);
    }
}