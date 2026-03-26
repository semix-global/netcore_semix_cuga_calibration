using CommunityToolkit.Diagnostics;
using Core.Models.Enums.Optics;
using Core.Models.Helper;
using Core.Models.Models.Common.Pattern;
using Core.Services.Interfaces;
using Cuga.Data.DataStruct.DTO.Swath;
using Net.Utilities.Attributes;
using Net.Utilities.Enums;
using Semix.CoreLib;

#if NET
using Semix.GRPC.DTO;
#else
using Semix.WcfTransfer.DTO;
#endif

namespace Core.Services.Implements.Mock;

[IOCAppService(ServiceType = typeof(ICalibrationOpticsService), IOCLifetimeEnum = IOCLifeTimeEnum.Singleton, IOCEnvironmentEnum = IOCEnvironmentEnum.Development)]
public sealed class CalibrationOpticsServiceMockImpl : ICalibrationOpticsService
{
    private double _currentRelayMotorValue;
    private double _currentINCMotorValue;
    private double _currentSCL1MotorValue;
    private double _currentSCL3MotorValue;
    private OpticsApodizationModeEnum _currentOpticsApodizationModeEnum;
    private OpticsPolarizationModeEnum _currentOpticsPolarizationModeEnum;

    public SxExecuteRet<bool> Connect()
    {
        Thread.Sleep(100);

        return SxExecuteRetHelper.CreateSuccess(true);
    }

    public SxExecuteRet<IReadOnlyList<ProductivityInformation>> GetProductivityInformations()
    {
        Thread.Sleep(100);

        var productivityInformations = new[]
        {
            ProductivityInformation.Default.Clone().AdaptIn(
                new C2MProductivityInfo
                {
                    Name = "S5",
#if NETFRAMEWORK
                    NIOI = SxNIOIEnum.OI,
#endif
                    Mag = SxMAGEnum.Low,
                    Speed = SxSpeedEnum.High,
                    IsUsed = true
                },
                new CgSwathSpeedInfo
                {
                    YPixelSize = 0.327,
                    YPixel = 508
                },
                508,
                408,
                445000
#if NET
                , OpticsIlluminationModeEnum.OI
#endif
            ),
            ProductivityInformation.Default.Clone().AdaptIn(
                new C2MProductivityInfo
                {
                    Name = "S10",
#if NETFRAMEWORK
                    NIOI = SxNIOIEnum.OI,
#endif
                    Mag = SxMAGEnum.Low,
                    Speed = SxSpeedEnum.Low,
                    IsUsed = true
                },
                new CgSwathSpeedInfo
                {
                    YPixelSize = 0.327,
                    YPixel = 508
                },
                508,
                408,
                222500
#if NET
                , OpticsIlluminationModeEnum.OI
#endif
            ),
            ProductivityInformation.Default.Clone().AdaptIn(
                new C2MProductivityInfo
                {
                    Name = "S25",
#if NETFRAMEWORK
                    NIOI = SxNIOIEnum.OI,
#endif
                    Mag = SxMAGEnum.Mid,
                    Speed = SxSpeedEnum.High,
                    IsUsed = true
                },
                new CgSwathSpeedInfo
                {
                    YPixelSize = 0.1635,
                    YPixel = 1008
                },
                1008,
                290,
                175900
#if NET
                , OpticsIlluminationModeEnum.OI
#endif
            ),
            ProductivityInformation.Default.Clone().AdaptIn(
                new C2MProductivityInfo
                {
                    Name = "S40",
#if NETFRAMEWORK
                    NIOI = SxNIOIEnum.OI,
#endif
                    Mag = SxMAGEnum.Mid,
                    Speed = SxSpeedEnum.Low,
                    IsUsed = true
                },
                new CgSwathSpeedInfo
                {
                    YPixelSize = 0.1635,
                    YPixel = 1008
                },
                1008,
                290,
                88060
#if NET
                , OpticsIlluminationModeEnum.OI
#endif
            ),
            ProductivityInformation.Default.Clone().AdaptIn(
                new C2MProductivityInfo
                {
                    Name = "S55",
#if NETFRAMEWORK
                    NIOI = SxNIOIEnum.OI,
#endif
                    Mag = SxMAGEnum.High,
                    Speed = SxSpeedEnum.High,
                    IsUsed = true
                },
                new CgSwathSpeedInfo
                {
                    YPixelSize = 0.11286,
                    YPixel = 1500
                },
                1500,
                210,
                87240
#if NET
                , OpticsIlluminationModeEnum.OI
#endif
            ),
            ProductivityInformation.Default.Clone().AdaptIn(
                new C2MProductivityInfo
                {
                    Name = "S90",
#if NETFRAMEWORK
                    NIOI = SxNIOIEnum.OI,
#endif
                    Mag = SxMAGEnum.High,
                    Speed = SxSpeedEnum.Low,
                    IsUsed = true
                },
                new CgSwathSpeedInfo
                {
                    YPixelSize = 0.11286,
                    YPixel = 1500
                },
                1500,
                210,
                43600
#if NET
                , OpticsIlluminationModeEnum.OI
#endif
            ),
            ProductivityInformation.Default.Clone().AdaptIn(
                new C2MProductivityInfo
                {
                    Name = "S40",
#if NETFRAMEWORK
                    NIOI = SxNIOIEnum.NI,
#endif
                    Mag = SxMAGEnum.Mid,
                    Speed = SxSpeedEnum.Mid,
                    IsUsed = true
                },
                new CgSwathSpeedInfo
                {
                    YPixelSize = 0.144,
                    YPixel = 1160
                },
                1160,
                200,
                40000
#if NET
                , OpticsIlluminationModeEnum.NI
#endif
            ),
            ProductivityInformation.Default.Clone().AdaptIn(
                new C2MProductivityInfo
                {
                    Name = "S90",
#if NETFRAMEWORK
                    NIOI = SxNIOIEnum.NI,
#endif
                    Mag = SxMAGEnum.High,
                    Speed = SxSpeedEnum.Mid,
                    IsUsed = true
                },
                new CgSwathSpeedInfo
                {
                    YPixelSize = 0.096,
                    YPixel = 1720
                },
                1720,
                200,
                26880
#if NET
                , OpticsIlluminationModeEnum.NI
#endif
            )
        };
        Guard.IsTrue(productivityInformations.DistinctBy(t => t).Count() == productivityInformations.Length, "Productivity Information is not unique");

        return SxExecuteRetHelper.CreateSuccess<IReadOnlyList<ProductivityInformation>>([.. productivityInformations.OrderBy(t => t)]);
    }

    public SxExecuteRet<double> GetRelayMotorAbsoluteValue(OpticsIlluminationModeEnum opticsIlluminationModeEnum)
    {
        Thread.Sleep(100);

        return SxExecuteRetHelper.CreateSuccess(_currentRelayMotorValue);
    }

    public SxExecuteRet<bool> SetRelayMotorAbsoluteValue(OpticsIlluminationModeEnum opticsIlluminationModeEnum, double value)
    {
        Thread.Sleep(100);

        _currentRelayMotorValue = value;

        return SxExecuteRetHelper.CreateSuccess(true);
    }

    public SxExecuteRet<double> GetINCMotorAbsoluteValue(OpticsIlluminationModeEnum opticsIlluminationModeEnum)
    {
        Thread.Sleep(100);

        return SxExecuteRetHelper.CreateSuccess(_currentINCMotorValue);
    }

    public SxExecuteRet<bool> SetINCMotorAbsoluteValue(OpticsIlluminationModeEnum opticsIlluminationModeEnum, double value)
    {
        Thread.Sleep(100);

        _currentINCMotorValue = value;

        return SxExecuteRetHelper.CreateSuccess(true);
    }

    public SxExecuteRet<(double L1, double L3)> GetSCMotorAbsoluteValue(OpticsIlluminationModeEnum opticsIlluminationModeEnum)
    {
        Thread.Sleep(100);

        return SxExecuteRetHelper.CreateSuccess((_currentSCL1MotorValue, _currentSCL3MotorValue));
    }

    public SxExecuteRet<bool> SetSCMotorAbsoluteValue(OpticsIlluminationModeEnum opticsIlluminationModeEnum, (double L1, double L3) value)
    {
        Thread.Sleep(100);

        _currentSCL1MotorValue = value.L1;
        _currentSCL3MotorValue = value.L3;

        return SxExecuteRetHelper.CreateSuccess(true);
    }

    public SxExecuteRet<bool> ToggleODFilter(bool isEnable)
    {
        Thread.Sleep(100);

        return SxExecuteRetHelper.CreateSuccess(true);
    }

    public SxExecuteRet<OpticsApodizationModeEnum> GetApodizationMode()
    {
        Thread.Sleep(100);

        return SxExecuteRetHelper.CreateSuccess(_currentOpticsApodizationModeEnum);
    }

    public SxExecuteRet<bool> SetApodizationMode(OpticsApodizationModeEnum opticsApodizationModeEnum)
    {
        Thread.Sleep(100);

        _currentOpticsApodizationModeEnum = opticsApodizationModeEnum;

        return SxExecuteRetHelper.CreateSuccess(true);
    }

    public SxExecuteRet<OpticsPolarizationModeEnum> GetPolarizationMode()
    {
        Thread.Sleep(100);

        return SxExecuteRetHelper.CreateSuccess(_currentOpticsPolarizationModeEnum);
    }

    public SxExecuteRet<bool> SetPolarizationMode(OpticsPolarizationModeEnum opticsPolarizationModeEnum)
    {
        Thread.Sleep(100);

        _currentOpticsPolarizationModeEnum = opticsPolarizationModeEnum;

        return SxExecuteRetHelper.CreateSuccess(true);
    }

    public SxExecuteRet<(double StartPos, double EndPos, double Accuracy)> GetDOEMotorRouteRange(OpticsIlluminationModeEnum opticsIlluminationModeEnum)
    {
        return SxExecuteRetHelper.CreateSuccess((0d, 20d, 1d));
    }

    public SxExecuteRet<double> GetDOEMotorAbsoluteValue(OpticsIlluminationModeEnum opticsIlluminationModeEnum)
    {
        return SxExecuteRetHelper.CreateSuccess(5d);
    }

    public SxExecuteRet<bool> SetDOEMotorAbsoluteValue(OpticsIlluminationModeEnum opticsIlluminationModeEnum, double value)
    {
        return SxExecuteRetHelper.CreateSuccess(true);
    }

    public SxExecuteRet<bool> SetPolarization(OpticsPolarizationModeEnum type)
    {
        Thread.Sleep(100);

        return SxExecuteRetHelper.CreateSuccess(true);
    }

    public SxExecuteRet<bool> SetNDF(OpticsChannelModeEnum ch, OpticsNDFTypeEnum type)
    {
        Thread.Sleep(100);

        return SxExecuteRetHelper.CreateSuccess(true);
    }

    public SxExecuteRet<bool> SetNDFRotary(OpticsChannelModeEnum ch, double val)
    {
        Thread.Sleep(100);

        return SxExecuteRetHelper.CreateSuccess(true);
    }
}