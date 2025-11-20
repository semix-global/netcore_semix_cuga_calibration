using CommunityToolkit.Diagnostics;
using Core.Models.Helper;
using Core.Models.Models.Common.Pattern;
using Core.Services.Interfaces;
using Cuga.Data.DataStruct.Microscope.Enums;
using Cuga.Interface.Calibration;
using Cuga.Interface.Diagnosis;
using Cuga.Interface.Facade;
using Net.Utilities.Attributes;
using Net.Utilities.Enums;
using Semix.CoreLib;

namespace Core.Services.Implements.GRPC;

[IOCAppService(ServiceType = typeof(ICalibrationMicroscopeService), IOCLifetimeEnum = IOCLifeTimeEnum.Singleton, IOCEnvironmentEnum = IOCEnvironmentEnum.Production | IOCEnvironmentEnum.Staging)]
public sealed class CalibrationMicroscopeServiceImpl : BaseService<ICgCalibMicroscopeService, ICgDiagReviewService, ICgFacadeReviewService>, ICalibrationMicroscopeService
{
    private IReadOnlyList<MicroscopeLensInformation>? _microscopeLensInformationList;

    public SxExecuteRet<bool> Connect()
    {
        if (IsConnected) return SxExecuteRetHelper.CreateSuccess(true);

        return Invoke(() =>
        {
            var createService = CreateService();
            IsConnected = createService.IsSuccess;

            return createService;
        });
    }

    public SxExecuteRet<IReadOnlyList<MicroscopeLensInformation>> GetMicroscopeLensInformations()
    {
        if (_microscopeLensInformationList is not null) return SxExecuteRetHelper.CreateSuccess(_microscopeLensInformationList);

        var sxExecuteRet = Invoke(() => Service2?.GetMicroscopeModels());
        if (sxExecuteRet.IsSuccess == false) return SxExecuteRetHelper.CreateError<IReadOnlyList<MicroscopeLensInformation>>(sxExecuteRet.ErrorMsg, []);
        if (sxExecuteRet.Anything.Count == 0) return SxExecuteRetHelper.CreateError<IReadOnlyList<MicroscopeLensInformation>>("Microscope Lens Information is empty", []);

        _microscopeLensInformationList =
        [
            ..sxExecuteRet.Anything
                .Single(t => t.IsUsed)
                .Info
                .Select(t => MicroscopeLensInformation.Default.Clone().AdaptIn(t))
        ];

        Guard.IsTrue(_microscopeLensInformationList.Select(t => t.LensCode).Distinct().Count() == _microscopeLensInformationList.Count, "Microscope Lens Information Lens Code is not unique");

        return SxExecuteRetHelper.CreateSuccess(_microscopeLensInformationList);
    }

    public SxExecuteRet<MicroscopeLensInformation> CgMicroscopeLensToMicroscopeLensInfo(CgMicroscopeLens cgMicroscopeLens)
    {
        var sxExecuteRet = GetMicroscopeLensInformations();
        if (sxExecuteRet.IsSuccess == false) return SxExecuteRetHelper.CreateError(sxExecuteRet.Msg, MicroscopeLensInformation.Default);

        var result = sxExecuteRet.Anything
            .SingleOrDefault(t => t.AdaptTo().LensCode == cgMicroscopeLens);

        return result is null
            ? SxExecuteRetHelper.CreateError("Microscope Lens Information is not single", MicroscopeLensInformation.Default)
            : SxExecuteRetHelper.CreateSuccess(result);
    }

    public SxExecuteRet<MicroscopeLensInformation> GetCurrentMicroscopeLensInformation()
    {
        var sxExecuteRet = Invoke(() => Service?.GetMagnification());

        return sxExecuteRet.IsSuccess == false
            ? SxExecuteRetHelper.CreateError(sxExecuteRet.Msg, MicroscopeLensInformation.Default)
            : CgMicroscopeLensToMicroscopeLensInfo(sxExecuteRet.Anything);
    }

    public SxExecuteRet<bool> SwitchMicroscopeLensInformationNotAutoFocus(MicroscopeLensInformation microscopeLensInformation)
    {
        // 确定是否开启自动聚焦
        var sxExecuteRet = Invoke(() => Service?.SwitchMagnification(new SxParamObj<CgMicroscopeLens>(microscopeLensInformation.AdaptTo().LensCode)));

        return sxExecuteRet.IsSuccess == false
            ? SxExecuteRetHelper.CreateError(sxExecuteRet.Msg, false)
            : SxExecuteRetHelper.CreateSuccess(true);
    }

    public SxExecuteRet<bool> SetVoltage(double voltage)
    {
        var sxExecuteRet = Invoke(() => Service?.SetVoltage(new SxParamObj<double>(voltage)));

        return sxExecuteRet.IsSuccess == false
            ? SxExecuteRetHelper.CreateError(sxExecuteRet.Msg, false)
            : SxExecuteRetHelper.CreateSuccess(true);
    }

    public SxExecuteRet<double> GetVoltage()
    {
        var sxExecuteRet = Invoke(() => Service2?.ReadVolt());

        return sxExecuteRet.IsSuccess == false
            ? SxExecuteRetHelper.CreateError(sxExecuteRet.Msg, 0d)
            : SxExecuteRetHelper.CreateSuccess(Convert.ToDouble(sxExecuteRet.Anything));
    }

    public SxExecuteRet<(double min, double max)> GetVoltageRange()
    {
        var sxExecuteRet = Invoke(() => Service?.ReadVoltLimit());

        return sxExecuteRet.IsSuccess == false
            ? SxExecuteRetHelper.CreateError(sxExecuteRet.Msg, default((double min, double max)))
            : SxExecuteRetHelper.CreateSuccess((Convert.ToDouble(sxExecuteRet.Anything.Lower), Convert.ToDouble(sxExecuteRet.Anything.Upper)));
    }
}