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
using Cuga.Data.DataStruct.PMT;

#endif

namespace Core.Services.Implements.Mock;

[IOCAppService(ServiceType = typeof(ICalibrationOpticsService), IOCLifetimeEnum = IOCLifeTimeEnum.Singleton, IOCEnvironmentEnum = IOCEnvironmentEnum.Development)]
public sealed class CalibrationOpticsServiceMockImpl : ICalibrationOpticsService
{
    private double _currentRelayMotorValue;
    private OpticsApodizationModeEnum _currentOpticsApodizationModeEnum;
    private OpticsPolarizationModeEnum _currentOpticsPolarizationModeEnum;

    public SxExecuteRet<bool> Connect()
    {
        Thread.Sleep(100);

        return SxExecuteRetHelper.CreateSuccess(true);
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
                    YPixel = 508,
                    Hz = 408
#if NETFRAMEWORK
                    ,
                    Speed = new CgDictionary<CgSpeedLevelType, CgSpeedSetting>
                    {
                        { CgSpeedLevelType.High, new CgSpeedSetting { Vel = 445, XPixelSize = 1.091 } }
                    }
#endif
                },
                508),
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
                    YPixel = 508,
                    Hz = 408
#if NETFRAMEWORK
                    ,
                    Speed = new CgDictionary<CgSpeedLevelType, CgSpeedSetting>
                    {
                        { CgSpeedLevelType.Low, new CgSpeedSetting { Vel = 222.5, XPixelSize = 0.546 } }
                    }
#endif
                },
                508),
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
                    YPixel = 1008,
                    Hz = 290
#if NETFRAMEWORK
                    ,
                    Speed = new CgDictionary<CgSpeedLevelType, CgSpeedSetting>
                    {
                        { CgSpeedLevelType.High, new CgSpeedSetting { Vel = 175.9, XPixelSize = 0.61 } }
                    }
#endif
                },
                1008),
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
                    YPixel = 1008,
                    Hz = 290
#if NETFRAMEWORK
                    ,
                    Speed = new CgDictionary<CgSpeedLevelType, CgSpeedSetting>
                    {
                        { CgSpeedLevelType.Low, new CgSpeedSetting { Vel = 88.06, XPixelSize = 0.304 } }
                    }
#endif
                },
                1008),
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
                    YPixel = 1500,
                    Hz = 210
#if NETFRAMEWORK
                    ,
                    Speed = new CgDictionary<CgSpeedLevelType, CgSpeedSetting>
                    {
                        { CgSpeedLevelType.High, new CgSpeedSetting { Vel = 87.24, XPixelSize = 0.416 } }
                    }
#endif
                },
                1500),
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
                    YPixel = 1500,
                    Hz = 210
#if NETFRAMEWORK
                    ,
                    Speed = new CgDictionary<CgSpeedLevelType, CgSpeedSetting>
                    {
                        { CgSpeedLevelType.Low, new CgSpeedSetting { Vel = 43.6, XPixelSize = 0.208 } }
                    }
#endif
                },
                1500),
            ProductivityInformation.Default.Clone().AdaptIn(
                new C2MProductivityInfo
                {
                    Name = "S90",
#if NETFRAMEWORK
                    NIOI = SxNIOIEnum.NI,
#endif
                    Mag = SxMAGEnum.High,
                    Speed = SxSpeedEnum.Low,
                    IsUsed = true
                },
                new CgSwathSpeedInfo
                {
                    YPixelSize = 0.096,
                    YPixel = 1720,
                    Hz = 200
#if NETFRAMEWORK
                    ,
                    Speed = new CgDictionary<CgSpeedLevelType, CgSpeedSetting>
                    {
                        { CgSpeedLevelType.Low, new CgSpeedSetting { Vel = 26.88, XPixelSize = 0.135 } }
                    }
#endif
                },
                1720),
            ProductivityInformation.Default.Clone().AdaptIn(
                new C2MProductivityInfo
                {
                    Name = "S40",
#if NETFRAMEWORK
                    NIOI = SxNIOIEnum.NI,
#endif
                    Mag = SxMAGEnum.Mid,
                    Speed = SxSpeedEnum.Low,
                    IsUsed = true
                },
                new CgSwathSpeedInfo
                {
                    YPixelSize = 0.144,
                    YPixel = 1160,
                    Hz = 200
#if NETFRAMEWORK
                    ,
                    Speed = new CgDictionary<CgSpeedLevelType, CgSpeedSetting>
                    {
                        { CgSpeedLevelType.Low, new CgSpeedSetting { Vel = 40, XPixelSize = 0.2 } }
                    }
#endif
                },
                1160)
        };

        Guard.IsTrue(productivityInformations.DistinctBy(t => t).Count() == productivityInformations.Length, "Productivity Information is not unique");

        return SxExecuteRetHelper.CreateSuccess<IReadOnlyList<ProductivityInformation>>([.. productivityInformations.OrderBy(t => t)]);
    }
}