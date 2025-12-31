using CommunityToolkit.Diagnostics;
using Core.Models.Enums.Optics;
using Core.Models.Extensions;
using Core.Models.Helper;
using Core.Models.Models.Common.Pattern;
using Core.Services.Interfaces;
using Cuga.Data.DataStruct.Basic;
using Cuga.Data.DataStruct.Optics;
using Cuga.Engine.Interface;
using Net.Utilities.Attributes;
using Net.Utilities.Enums;
using Semix.CoreLib;

namespace Core.Services.Implements.WCF;

[IOCAppService(ServiceType = typeof(ICalibrationOpticsService), IOCLifetimeEnum = IOCLifeTimeEnum.Singleton, IOCEnvironmentEnum = IOCEnvironmentEnum.Production | IOCEnvironmentEnum.Staging)]
public sealed class CalibrationOpticsServiceImpl : BaseService<ICgCalibrationService>, ICalibrationOpticsService
{
    public SxExecuteRet<bool> Connect()
    {
        if (IsConnected) return SxExecuteRetHelper.CreateSuccess(true);

        return Invoke(() =>
        {
            var ep = new SxWcfEndPoint("127.0.0.1", 80, CgInernalAddr.CalAddr);
            var createService = CreateService(ep);
            IsConnected = createService.IsSuccess;
            return createService;
        }, false);
    }

    public SxExecuteRet<IReadOnlyList<ProductivityInformation>> GetProductivityInformations()
    {
        var sxExecuteRet = Invoke(() => Service?.GetProductivityInfos());

        if (sxExecuteRet.IsSuccess == false) return SxExecuteRetHelper.CreateError<IReadOnlyList<ProductivityInformation>>(sxExecuteRet.ErrorMsg, []);

        var productivityInformationList = new List<ProductivityInformation>();

        foreach (var c2MProductivityInfo in sxExecuteRet.Anything.Where(t => t.IsUsed))
        {
            var speedInfoSxExecuteRet = Invoke(() => Service?.GetSpeedInfo(c2MProductivityInfo.Mag, c2MProductivityInfo.NIOI));
            if (speedInfoSxExecuteRet.IsSuccess == false) return SxExecuteRetHelper.CreateError<IReadOnlyList<ProductivityInformation>>(speedInfoSxExecuteRet.ErrorMsg, []);

            var pmtDataLineHeightSxExecuteRet = Invoke(() => Service?.GetPmtDataLineHeight(c2MProductivityInfo.Mag, c2MProductivityInfo.NIOI));
            if (pmtDataLineHeightSxExecuteRet.IsSuccess == false) return SxExecuteRetHelper.CreateError<IReadOnlyList<ProductivityInformation>>(speedInfoSxExecuteRet.ErrorMsg, []);

            productivityInformationList.Add(ProductivityInformation.Default.Clone().AdaptIn(c2MProductivityInfo, speedInfoSxExecuteRet.Anything, pmtDataLineHeightSxExecuteRet.Anything));
        }

        Guard.IsTrue(productivityInformationList.DistinctBy(t => t).Count() == productivityInformationList.Count, "Productivity Information is not unique");

        return SxExecuteRetHelper.CreateSuccess<IReadOnlyList<ProductivityInformation>>([.. productivityInformationList.OrderBy(t => t)]);
    }

    public SxExecuteRet<double> GetRelayMotorAbsoluteValue(OpticsIlluminationModeEnum opticsIlluminationModeEnum)
    {
        var sxExecuteRet = Invoke(() => Service?.OpticCommonReadPos(opticsIlluminationModeEnum switch
        {
            OpticsIlluminationModeEnum.OI => CgCommonType.OI_Relay,
            OpticsIlluminationModeEnum.NI => CgCommonType.NI_Relay,
            _ => ThrowHelper.ThrowArgumentOutOfRangeException<CgCommonType>(nameof(opticsIlluminationModeEnum))
        }));
        if (sxExecuteRet.IsSuccess == false) return SxExecuteRetHelper.CreateError<double>(sxExecuteRet.ErrorMsg, 0);

        return SxExecuteRetHelper.CreateSuccess(sxExecuteRet.Anything);
    }

    public SxExecuteRet<bool> ToggleODFilter(bool isEnable)
    {
        var sxExecuteRet = Invoke(() => Service?.SetOD(isEnable ? CgODEnum.OD2_0 : CgODEnum.None));

        return sxExecuteRet.IsSuccess == false
            ? SxExecuteRetHelper.CreateError(sxExecuteRet.Msg, false)
            : SxExecuteRetHelper.CreateSuccess(true);
    }

    public SxExecuteRet<OpticsApodizationModeEnum> GetApodizationMode()
    {
        return SxExecuteRetHelper.CreateSuccess(OpticsApodizationModeEnum.Cosine);
    }

    public SxExecuteRet<bool> SetApodizationMode(OpticsApodizationModeEnum opticsApodizationModeEnum)
    {
        return SxExecuteRetHelper.CreateSuccess(true);
    }

    public SxExecuteRet<bool> SetRelayMotorAbsoluteValue(OpticsIlluminationModeEnum opticsIlluminationModeEnum, double value)
    {
        var sxExecuteRet = Invoke(() => Service?.OpticCommonMove(opticsIlluminationModeEnum switch
        {
            OpticsIlluminationModeEnum.OI => CgCommonType.OI_Relay,
            OpticsIlluminationModeEnum.NI => CgCommonType.NI_Relay,
            _ => ThrowHelper.ThrowArgumentOutOfRangeException<CgCommonType>(nameof(opticsIlluminationModeEnum))
        }, value));

        if (sxExecuteRet.IsSuccess == false) return SxExecuteRetHelper.CreateError(sxExecuteRet.ErrorMsg, false);

        return SxExecuteRetHelper.CreateSuccess(true);
    }

    public SxExecuteRet<OpticsPolarizationModeEnum> GetPolarizationMode()
    {
        var sxExecuteRet = Invoke(() => Service?.ReadPolarizationType());
        if (sxExecuteRet.IsSuccess == false) return SxExecuteRetHelper.CreateError<OpticsPolarizationModeEnum>(sxExecuteRet.ErrorMsg, default);

        return SxExecuteRetHelper.CreateSuccess(sxExecuteRet.Anything.ToOpticsPolarizationModeEnum());
    }

    public SxExecuteRet<bool> SetPolarizationMode(OpticsPolarizationModeEnum opticsPolarizationModeEnum)
    {
        var sxExecuteRet = Invoke(() => Service?.SetPolarization(opticsPolarizationModeEnum.ToCgPolarizationTypeEnum()));

        return sxExecuteRet.IsSuccess == false
            ? SxExecuteRetHelper.CreateError(sxExecuteRet.Msg, false)
            : SxExecuteRetHelper.CreateSuccess(true);
    }
}