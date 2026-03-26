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

            var hzAndRealSpeedSxExecuteRet = Invoke(() => Service?.GetHzAndRealSpeed(c2MProductivityInfo.NIOI, c2MProductivityInfo.Mag, c2MProductivityInfo.Speed));
            if (hzAndRealSpeedSxExecuteRet.IsSuccess == false) return SxExecuteRetHelper.CreateError<IReadOnlyList<ProductivityInformation>>(speedInfoSxExecuteRet.ErrorMsg, []);

            productivityInformationList.Add(ProductivityInformation.Default.Clone().AdaptIn(
                c2MProductivityInfo,
                speedInfoSxExecuteRet.Anything,
                pmtDataLineHeightSxExecuteRet.Anything,
                hzAndRealSpeedSxExecuteRet.Anything.hz,
                hzAndRealSpeedSxExecuteRet.Anything.realSpeed));
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

    public SxExecuteRet<double> GetINCMotorAbsoluteValue(OpticsIlluminationModeEnum opticsIlluminationModeEnum)
    {
        var sxExecuteRet = Invoke(() => Service?.OpticCommonReadPos(opticsIlluminationModeEnum switch
        {
            OpticsIlluminationModeEnum.OI => CgCommonType.OI_INC,
            OpticsIlluminationModeEnum.NI => CgCommonType.NI_INC,
            _ => ThrowHelper.ThrowArgumentOutOfRangeException<CgCommonType>(nameof(opticsIlluminationModeEnum))
        }));
        if (sxExecuteRet.IsSuccess == false) return SxExecuteRetHelper.CreateError<double>(sxExecuteRet.ErrorMsg, 0);

        return SxExecuteRetHelper.CreateSuccess(sxExecuteRet.Anything);
    }

    public SxExecuteRet<bool> SetINCMotorAbsoluteValue(OpticsIlluminationModeEnum opticsIlluminationModeEnum, double value)
    {
        var sxExecuteRet = Invoke(() => Service?.OpticCommonMove(opticsIlluminationModeEnum switch
        {
            OpticsIlluminationModeEnum.OI => CgCommonType.OI_INC,
            OpticsIlluminationModeEnum.NI => CgCommonType.NI_INC,
            _ => ThrowHelper.ThrowArgumentOutOfRangeException<CgCommonType>(nameof(opticsIlluminationModeEnum))
        }, value));

        if (sxExecuteRet.IsSuccess == false) return SxExecuteRetHelper.CreateError(sxExecuteRet.ErrorMsg, false);

        return SxExecuteRetHelper.CreateSuccess(true);
    }

    public SxExecuteRet<(double L1, double L3)> GetSCMotorAbsoluteValue(OpticsIlluminationModeEnum opticsIlluminationModeEnum)
    {
        var l1SxExecuteRet = Invoke(() => Service?.OpticCommonReadPos(opticsIlluminationModeEnum switch
        {
            OpticsIlluminationModeEnum.OI => CgCommonType.OISC_L,
            OpticsIlluminationModeEnum.NI => CgCommonType.NISC_L,
            _ => ThrowHelper.ThrowArgumentOutOfRangeException<CgCommonType>(nameof(opticsIlluminationModeEnum))
        }));
        if (l1SxExecuteRet.IsSuccess == false) return SxExecuteRetHelper.CreateError<(double L1, double L3)>(l1SxExecuteRet.ErrorMsg, (0d, 0d));

        var l3SxExecuteRet = Invoke(() => Service?.OpticCommonReadPos(opticsIlluminationModeEnum switch
        {
            OpticsIlluminationModeEnum.OI => CgCommonType.OISC_R,
            OpticsIlluminationModeEnum.NI => CgCommonType.NISC_R,
            _ => ThrowHelper.ThrowArgumentOutOfRangeException<CgCommonType>(nameof(opticsIlluminationModeEnum))
        }));
        if (l3SxExecuteRet.IsSuccess == false) return SxExecuteRetHelper.CreateError<(double L1, double L3)>(l3SxExecuteRet.ErrorMsg, (0d, 0d));

        return SxExecuteRetHelper.CreateSuccess((l1SxExecuteRet.Anything, l3SxExecuteRet.Anything));
    }

    public SxExecuteRet<bool> SetSCMotorAbsoluteValue(OpticsIlluminationModeEnum opticsIlluminationModeEnum, (double L1, double L3) value)
    {
        var l1SxExecuteRet = Invoke(() => Service?.OpticCommonMove(opticsIlluminationModeEnum switch
        {
            OpticsIlluminationModeEnum.OI => CgCommonType.OISC_L,
            OpticsIlluminationModeEnum.NI => CgCommonType.NISC_L,
            _ => ThrowHelper.ThrowArgumentOutOfRangeException<CgCommonType>(nameof(opticsIlluminationModeEnum))
        }, value.L1));

        if (l1SxExecuteRet.IsSuccess == false) return SxExecuteRetHelper.CreateError(l1SxExecuteRet.ErrorMsg, false);

        var l3SxExecuteRet = Invoke(() => Service?.OpticCommonMove(opticsIlluminationModeEnum switch
        {
            OpticsIlluminationModeEnum.OI => CgCommonType.OISC_R,
            OpticsIlluminationModeEnum.NI => CgCommonType.NISC_R,
            _ => ThrowHelper.ThrowArgumentOutOfRangeException<CgCommonType>(nameof(opticsIlluminationModeEnum))
        }, value.L3));

        if (l3SxExecuteRet.IsSuccess == false) return SxExecuteRetHelper.CreateError(l3SxExecuteRet.ErrorMsg, false);

        return SxExecuteRetHelper.CreateSuccess(true);
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

    public SxExecuteRet<(double StartPos, double EndPos, double Accuracy)> GetDOEMotorRouteRange(OpticsIlluminationModeEnum opticsIlluminationModeEnum)
    {
        var motorType = opticsIlluminationModeEnum switch
        {
            OpticsIlluminationModeEnum.OI => CgCommonType.OI_DOE,
            OpticsIlluminationModeEnum.NI => CgCommonType.NI_DOE,
            _ => ThrowHelper.ThrowArgumentOutOfRangeException<CgCommonType>(nameof(opticsIlluminationModeEnum))
        };
        var sxExecuteRet = Invoke(() => Service?.GetOpticRange(motorType));
        if (sxExecuteRet.IsSuccess == false) return SxExecuteRetHelper.CreateError(sxExecuteRet.ErrorMsg, (0d, 0d, 0d));

        return SxExecuteRetHelper.CreateSuccess((sxExecuteRet.Anything.min, sxExecuteRet.Anything.max, 0.001));
    }

    public SxExecuteRet<double> GetDOEMotorAbsoluteValue(OpticsIlluminationModeEnum opticsIlluminationModeEnum)
    {
        var motorType = opticsIlluminationModeEnum switch
        {
            OpticsIlluminationModeEnum.OI => CgCommonType.OI_DOE,
            OpticsIlluminationModeEnum.NI => CgCommonType.NI_DOE,
            _ => ThrowHelper.ThrowArgumentOutOfRangeException<CgCommonType>(nameof(opticsIlluminationModeEnum))
        };
        var sxExecuteRet = Invoke(() => Service?.OpticCommonReadPos(motorType));
        if (sxExecuteRet.IsSuccess == false) return SxExecuteRetHelper.CreateError<double>(sxExecuteRet.ErrorMsg, 0);

        return SxExecuteRetHelper.CreateSuccess(sxExecuteRet.Anything);
    }

    public SxExecuteRet<bool> SetDOEMotorAbsoluteValue(OpticsIlluminationModeEnum opticsIlluminationModeEnum, double value)
    {
        var motorType = opticsIlluminationModeEnum switch
        {
            OpticsIlluminationModeEnum.OI => CgCommonType.OI_DOE,
            OpticsIlluminationModeEnum.NI => CgCommonType.NI_DOE,
            _ => ThrowHelper.ThrowArgumentOutOfRangeException<CgCommonType>(nameof(opticsIlluminationModeEnum))
        };
        var sxExecuteRet = Invoke(() => Service?.OpticCommonMove(motorType, value));
        if (sxExecuteRet.IsSuccess == false) return SxExecuteRetHelper.CreateError(sxExecuteRet.ErrorMsg, false);

        return SxExecuteRetHelper.CreateSuccess(true);
    }

    public SxExecuteRet<bool> SetPolarization(OpticsPolarizationModeEnum type)
    {
        var sxExecuteRet = Invoke(() => Service?.SetPolarization(type.ToCgPolarizationTypeEnum()));

        return sxExecuteRet.IsSuccess == false
            ? SxExecuteRetHelper.CreateError(sxExecuteRet.Msg, false)
            : SxExecuteRetHelper.CreateSuccess(true);
    }

    public SxExecuteRet<bool> SetNDF(OpticsChannelModeEnum ch, OpticsNDFTypeEnum type)
    {
        var sxExecuteRet = Invoke(() => Service?.SetNDF(ch.ToCgChannelTypeEnum(), type.ToCgNDFTypeEnum()));

        return sxExecuteRet.IsSuccess == false
            ? SxExecuteRetHelper.CreateError(sxExecuteRet.Msg, false)
            : SxExecuteRetHelper.CreateSuccess(true);
    }

    public SxExecuteRet<bool> SetNDFRotary(OpticsChannelModeEnum ch, double val)
    {
        var sxExecuteRet = Invoke(() => Service?.SetNDFRotary(ch.ToCgChannelTypeEnum(), val));

        return sxExecuteRet.IsSuccess == false
            ? SxExecuteRetHelper.CreateError(sxExecuteRet.Msg, false)
            : SxExecuteRetHelper.CreateSuccess(true);
    }
}