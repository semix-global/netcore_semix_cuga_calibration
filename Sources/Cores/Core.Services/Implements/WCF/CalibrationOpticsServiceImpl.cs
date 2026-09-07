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
using Net.Utilities.Models;
using Semix.CoreLib;
using System.Runtime.CompilerServices;

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

            var hzAndRealSpeedSxExecuteRet = Invoke(() => Service?.GetHzAndRealSpeed(c2MProductivityInfo.NIOI, c2MProductivityInfo.Mag, c2MProductivityInfo.Speed));
            if (hzAndRealSpeedSxExecuteRet.IsSuccess == false) return SxExecuteRetHelper.CreateError<IReadOnlyList<ProductivityInformation>>(speedInfoSxExecuteRet.ErrorMsg, []);

            productivityInformationList.Add(ProductivityInformation.Default.Clone().AdaptIn(
                c2MProductivityInfo,
                speedInfoSxExecuteRet.Anything.YPixelSize,
                speedInfoSxExecuteRet.Anything.YPixel,
                speedInfoSxExecuteRet.Anything.PxStartPoint,
                speedInfoSxExecuteRet.Anything.PxEndPoint,
                speedInfoSxExecuteRet.Anything.Hz,
                hzAndRealSpeedSxExecuteRet.Anything.realSpeed));
        }

        Guard.IsTrue(productivityInformationList.DistinctBy(t => t).Count() == productivityInformationList.Count, "Productivity Information is not unique");

        return SxExecuteRetHelper.CreateSuccess<IReadOnlyList<ProductivityInformation>>([.. productivityInformationList.OrderBy(t => t)]);
    }

    public SxExecuteRet<double> GetDOEMotorAbsoluteValue(OpticsIlluminationModeEnum opticsIlluminationModeEnum) => GetMotorAbsoluteValue(opticsIlluminationModeEnum switch
    {
        OpticsIlluminationModeEnum.OI => CgCommonType.OI_DOE,
        OpticsIlluminationModeEnum.NI => CgCommonType.NI_DOE,
        _ => ThrowHelper.ThrowArgumentOutOfRangeException<CgCommonType>(nameof(opticsIlluminationModeEnum))
    });

    public SxExecuteRet<bool> SetDOEMotorAbsoluteValue(OpticsIlluminationModeEnum opticsIlluminationModeEnum, double value) => SetMotorAbsoluteValue(opticsIlluminationModeEnum switch
    {
        OpticsIlluminationModeEnum.OI => CgCommonType.OI_DOE,
        OpticsIlluminationModeEnum.NI => CgCommonType.NI_DOE,
        _ => ThrowHelper.ThrowArgumentOutOfRangeException<CgCommonType>(nameof(opticsIlluminationModeEnum))
    }, value);

    public SxExecuteRet<double> GetRelayMotorAbsoluteValue(OpticsIlluminationModeEnum opticsIlluminationModeEnum) => GetMotorAbsoluteValue(opticsIlluminationModeEnum switch
    {
        OpticsIlluminationModeEnum.OI => CgCommonType.OI_Relay,
        OpticsIlluminationModeEnum.NI => CgCommonType.NI_Relay,
        _ => ThrowHelper.ThrowArgumentOutOfRangeException<CgCommonType>(nameof(opticsIlluminationModeEnum))
    });

    public SxExecuteRet<bool> SetRelayMotorAbsoluteValue(OpticsIlluminationModeEnum opticsIlluminationModeEnum, double value) => SetMotorAbsoluteValue(opticsIlluminationModeEnum switch
    {
        OpticsIlluminationModeEnum.OI => CgCommonType.OI_Relay,
        OpticsIlluminationModeEnum.NI => CgCommonType.NI_Relay,
        _ => ThrowHelper.ThrowArgumentOutOfRangeException<CgCommonType>(nameof(opticsIlluminationModeEnum))
    }, value);

    public SxExecuteRet<double> GetINCMotorAbsoluteValue(OpticsIlluminationModeEnum opticsIlluminationModeEnum) => GetMotorAbsoluteValue(opticsIlluminationModeEnum switch
    {
        OpticsIlluminationModeEnum.OI => CgCommonType.OI_INC,
        OpticsIlluminationModeEnum.NI => CgCommonType.NI_INC,
        _ => ThrowHelper.ThrowArgumentOutOfRangeException<CgCommonType>(nameof(opticsIlluminationModeEnum))
    });

    public SxExecuteRet<bool> SetINCMotorAbsoluteValue(OpticsIlluminationModeEnum opticsIlluminationModeEnum, double value) => SetMotorAbsoluteValue(opticsIlluminationModeEnum switch
    {
        OpticsIlluminationModeEnum.OI => CgCommonType.OI_INC,
        OpticsIlluminationModeEnum.NI => CgCommonType.NI_INC,
        _ => ThrowHelper.ThrowArgumentOutOfRangeException<CgCommonType>(nameof(opticsIlluminationModeEnum))
    }, value);

    public SxExecuteRet<(double L1, double L3)> GetSCMotorAbsoluteValue(OpticsIlluminationModeEnum opticsIlluminationModeEnum)
    {
        var l1SxExecuteRet = GetMotorAbsoluteValue(opticsIlluminationModeEnum switch
        {
            OpticsIlluminationModeEnum.OI => CgCommonType.OISC_L,
            OpticsIlluminationModeEnum.NI => CgCommonType.NISC_L,
            _ => ThrowHelper.ThrowArgumentOutOfRangeException<CgCommonType>(nameof(opticsIlluminationModeEnum))
        });
        if (l1SxExecuteRet.IsSuccess == false) return SxExecuteRetHelper.CreateError<(double L1, double L3)>(l1SxExecuteRet.ErrorMsg, (0d, 0d));

        var l3SxExecuteRet = GetMotorAbsoluteValue(opticsIlluminationModeEnum switch
        {
            OpticsIlluminationModeEnum.OI => CgCommonType.OISC_R,
            OpticsIlluminationModeEnum.NI => CgCommonType.NISC_R,
            _ => ThrowHelper.ThrowArgumentOutOfRangeException<CgCommonType>(nameof(opticsIlluminationModeEnum))
        });
        if (l3SxExecuteRet.IsSuccess == false) return SxExecuteRetHelper.CreateError<(double L1, double L3)>(l3SxExecuteRet.ErrorMsg, (0d, 0d));

        return SxExecuteRetHelper.CreateSuccess((l1SxExecuteRet.Anything, l3SxExecuteRet.Anything));
    }

    public SxExecuteRet<double> GetPMTInterval()
    {
        var sxExecuteRet = Invoke(() => Service?.GetSpotSize());
        if (sxExecuteRet.IsSuccess == false) return SxExecuteRetHelper.CreateError(sxExecuteRet.ErrorMsg, 0d);

        return SxExecuteRetHelper.CreateSuccess(sxExecuteRet.Anything);
    }

    public SxExecuteRet<bool> SetSCMotorAbsoluteValue(OpticsIlluminationModeEnum opticsIlluminationModeEnum, (double L1, double L3) value)
    {
        var l1SxExecuteRet = SetMotorAbsoluteValue(opticsIlluminationModeEnum switch
        {
            OpticsIlluminationModeEnum.OI => CgCommonType.OISC_L,
            OpticsIlluminationModeEnum.NI => CgCommonType.NISC_L,
            _ => ThrowHelper.ThrowArgumentOutOfRangeException<CgCommonType>(nameof(opticsIlluminationModeEnum))
        }, value.L1);
        if (l1SxExecuteRet.IsSuccess == false) return SxExecuteRetHelper.CreateError(l1SxExecuteRet.ErrorMsg, false);

        var l3SxExecuteRet = SetMotorAbsoluteValue(opticsIlluminationModeEnum switch
        {
            OpticsIlluminationModeEnum.OI => CgCommonType.OISC_R,
            OpticsIlluminationModeEnum.NI => CgCommonType.NISC_R,
            _ => ThrowHelper.ThrowArgumentOutOfRangeException<CgCommonType>(nameof(opticsIlluminationModeEnum))
        }, value.L3);
        if (l3SxExecuteRet.IsSuccess == false) return SxExecuteRetHelper.CreateError(l3SxExecuteRet.ErrorMsg, false);

        return SxExecuteRetHelper.CreateSuccess(true);
    }

    public SxExecuteRet<double> GetCollectorPolarizationMotorAbsoluteValue(int channelId)
    {
        var sxExecuteRet = Invoke(() => Service?.ReadNDFRotary(channelId.ToCgNDFChEnum()));
        if (sxExecuteRet.IsSuccess == false) return SxExecuteRetHelper.CreateError(sxExecuteRet.ErrorMsg, 0d);

        return SxExecuteRetHelper.CreateSuccess(sxExecuteRet.Anything);
    }

    public SxExecuteRet<bool> SetCollectorPolarizationMotorAbsoluteValue(int channelId, double value)
    {
        var sxExecuteRet = Invoke(() => Service?.SetNDFRotary(channelId.ToCgNDFChEnum(), value));

        return sxExecuteRet.IsSuccess == false
            ? SxExecuteRetHelper.CreateError(sxExecuteRet.Msg, false)
            : SxExecuteRetHelper.CreateSuccess(true);
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
        return SxExecuteRetHelper.CreateSuccess(OpticsApodizationModeEnum.None);
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

    public SxExecuteRet<OpticsCollectorPolarizationModeEnum> GetCollectorPolarizationMode()
    {
        var sxExecuteRetCh1 = Invoke(() => Service?.ReadNDFType(CgNDFCHEnum.CH1_NDF));
        if (sxExecuteRetCh1.IsSuccess == false) return SxExecuteRetHelper.CreateError<OpticsCollectorPolarizationModeEnum>(sxExecuteRetCh1.ErrorMsg, default);
        var sxExecuteRetCh2 = Invoke(() => Service?.ReadNDFType(CgNDFCHEnum.CH2_NDF));
        if (sxExecuteRetCh2.IsSuccess == false) return SxExecuteRetHelper.CreateError<OpticsCollectorPolarizationModeEnum>(sxExecuteRetCh2.ErrorMsg, default);
        var sxExecuteRetCh3 = Invoke(() => Service?.ReadNDFType(CgNDFCHEnum.CH3_NDF));
        if (sxExecuteRetCh3.IsSuccess == false) return SxExecuteRetHelper.CreateError<OpticsCollectorPolarizationModeEnum>(sxExecuteRetCh3.ErrorMsg, default);

        var ch1 = sxExecuteRetCh1.Anything.ToCollectorPolarizationModeEnum();
        var ch2 = sxExecuteRetCh2.Anything.ToCollectorPolarizationModeEnum();
        var ch3 = sxExecuteRetCh3.Anything.ToCollectorPolarizationModeEnum();

        return SxExecuteRetHelper.CreateSuccess(new[] { ch1, ch2, ch3 }.Min());
    }

    public SxExecuteRet<bool> SetCollectorPolarizationMode(OpticsCollectorPolarizationModeEnum opticsCollectorPolarizationModeEnum)
    {
        var sxExecuteRet = Invoke(() => Service?.SetNDF(CgNDFCHEnum.ALL, opticsCollectorPolarizationModeEnum.ToCgNDFTypeEnum()));

        return sxExecuteRet.IsSuccess == false
            ? SxExecuteRetHelper.CreateError(sxExecuteRet.Msg, false)
            : SxExecuteRetHelper.CreateSuccess(true);
    }

    public SxExecuteRet<OpticsCollectorPolarizationModeEnum> GetCollectorPolarizationMode(int channelId)
    {
        var sxExecuteRet = Invoke(() => Service?.ReadNDFType(channelId.ToCgNDFChEnum()));
        if (sxExecuteRet.IsSuccess == false) return SxExecuteRetHelper.CreateError<OpticsCollectorPolarizationModeEnum>(sxExecuteRet.ErrorMsg, default);

        return SxExecuteRetHelper.CreateSuccess(sxExecuteRet.Anything.ToCollectorPolarizationModeEnum());
    }

    public SxExecuteRet<bool> SetCollectorPolarizationMode(int channelId, OpticsCollectorPolarizationModeEnum opticsCollectorPolarizationModeEnum)
    {
        var sxExecuteRet = Invoke(() => Service?.SetNDF(channelId.ToCgNDFChEnum(), opticsCollectorPolarizationModeEnum.ToCgNDFTypeEnum()));

        return sxExecuteRet.IsSuccess == false
            ? SxExecuteRetHelper.CreateError(sxExecuteRet.Msg, false)
            : SxExecuteRetHelper.CreateSuccess(true);
    }

    public SxExecuteRet<double> GetMotorAbsoluteValue(CgCommonType cgCommonType, [CallerMemberName] string name = Constants.EmptyString)
    {
        var sxExecuteRetOpticCommonReadPos = Invoke(() => Service?.OpticCommonReadPos(cgCommonType));
        if (sxExecuteRetOpticCommonReadPos.IsSuccess == false) return SxExecuteRetHelper.CreateError(sxExecuteRetOpticCommonReadPos.ErrorMsg, 0d);

        return SxExecuteRetHelper.CreateSuccess(sxExecuteRetOpticCommonReadPos.Anything);
    }

    public SxExecuteRet<bool> SetMotorAbsoluteValue(CgCommonType cgCommonType, double value, [CallerMemberName] string name = Constants.EmptyString)
    {
        var sxExecuteRetGetOpticRange = Invoke(() => Service?.GetOpticRange(cgCommonType));
        if (sxExecuteRetGetOpticRange.IsSuccess == false) return SxExecuteRetHelper.CreateError(sxExecuteRetGetOpticRange.ErrorMsg, false);

        Guard.IsBetween(value, sxExecuteRetGetOpticRange.Anything.min, sxExecuteRetGetOpticRange.Anything.max, name);

        var sxExecuteRetOpticCommonMove = Invoke(() => Service?.OpticCommonMove(cgCommonType, value));
        if (sxExecuteRetOpticCommonMove.IsSuccess == false) return SxExecuteRetHelper.CreateError(sxExecuteRetOpticCommonMove.ErrorMsg, false);

        return SxExecuteRetHelper.CreateSuccess(true);
    }

    public SxExecuteRet<(double StartPos, double EndPos, double Accuracy)> GetMotorRouteRange(CgCommonType cgCommonType, [CallerMemberName] string name = Constants.EmptyString)
    {
        var sxExecuteRet = Invoke(() => Service?.GetOpticRange(cgCommonType));
        if (sxExecuteRet.IsSuccess == false) return SxExecuteRetHelper.CreateError(sxExecuteRet.ErrorMsg, (0d, 0d, 0d));

        return SxExecuteRetHelper.CreateSuccess((sxExecuteRet.Anything.min, sxExecuteRet.Anything.max, 0.1));
    }

    public SxExecuteRet<bool> ToggleZoosClinder(OpticsIlluminationModeEnum opticsIlluminationModeEnum, bool enable)
    {
        var sxExecuteRet = Invoke(() => Service?.ClinderEXC(opticsIlluminationModeEnum switch
        {
            OpticsIlluminationModeEnum.OI => CgClinderType.OI_Zoos,
            OpticsIlluminationModeEnum.NI => CgClinderType.NI_Zoos,
            _ => ThrowHelper.ThrowArgumentOutOfRangeException<CgClinderType>(nameof(opticsIlluminationModeEnum))
        }, enable));

        return sxExecuteRet.IsSuccess == false
            ? SxExecuteRetHelper.CreateError(sxExecuteRet.Msg, false)
            : SxExecuteRetHelper.CreateSuccess(true);
    }

    public SxExecuteRet<(double StartPos, double EndPos, double Accuracy)> GetROOSMotorRouteRange()
    {
        var sxExecuteRet = Invoke(() => Service?.GetOpticRange(CgCommonType.Roos));
        if (sxExecuteRet.IsSuccess == false) return SxExecuteRetHelper.CreateError(sxExecuteRet.ErrorMsg, (0d, 0d, 0d));

        return SxExecuteRetHelper.CreateSuccess((sxExecuteRet.Anything.min, sxExecuteRet.Anything.max, 0.1));
    }

    public SxExecuteRet<double> GetROOSMotorAbsoluteValue()
    {
        var sxExecuteRet = Invoke(() => Service?.OpticCommonReadPos(CgCommonType.Roos));
        if (sxExecuteRet.IsSuccess == false) return SxExecuteRetHelper.CreateError<double>(sxExecuteRet.ErrorMsg, 0);

        return SxExecuteRetHelper.CreateSuccess(sxExecuteRet.Anything);
    }

    public SxExecuteRet<bool> SetROOSMotorAbsoluteValue(double value)
    {
        var sxExecuteRet = Invoke(() => Service?.OpticCommonMove(CgCommonType.Roos, value));
        if (sxExecuteRet.IsSuccess == false) return SxExecuteRetHelper.CreateError(sxExecuteRet.ErrorMsg, false);

        return SxExecuteRetHelper.CreateSuccess(true);
    }
}