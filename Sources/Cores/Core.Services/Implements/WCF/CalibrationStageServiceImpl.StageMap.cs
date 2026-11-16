using Core.Models.Helper;
using Core.Models.Models.Common.StageMap;
using Semix.CoreLib;

namespace Core.Services.Implements.WCF;

public sealed partial class CalibrationStageServiceImpl
{
    public SxExecuteRet<bool> ToggleEnableStageMap(bool isEnable)
    {
        var sxExecuteRet = Invoke(() => Service!.ErrorMapSwich(isEnable));
        if (sxExecuteRet.IsSuccess == false) return SxExecuteRetHelper.CreateError(sxExecuteRet.Msg, false);

        if (isEnable == false)
        {
            sxExecuteRet = Invoke(() => Service!.CloseErrorMap());
            if (sxExecuteRet.IsSuccess == false) return SxExecuteRetHelper.CreateError(sxExecuteRet.Msg, false);
        }

        return sxExecuteRet.IsSuccess == false
            ? SxExecuteRetHelper.CreateError(sxExecuteRet.Msg, false)
            : SxExecuteRetHelper.CreateSuccess(true);
    }

    public SxExecuteRet<bool> SetStageMap(StageMapErrorDTO stageMapError)
    {
        var sxExecuteRet = Invoke(() => Service!.ExecErrorMap(stageMapError.AdaptTo()));

        return sxExecuteRet.IsSuccess == false
            ? SxExecuteRetHelper.CreateError(sxExecuteRet.Msg, false)
            : SxExecuteRetHelper.CreateSuccess(true);
    }
}