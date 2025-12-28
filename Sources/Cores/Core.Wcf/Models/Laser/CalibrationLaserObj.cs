using Cuga.Data.DataStruct.DTO.Swath;
using Cuga.Data.DataStruct.Microscope.Enums;
using Cuga.Data.DataStruct.Optics;
using Cuga.Data.DataStruct.Stage;
using System;
using System.Collections.Generic;

#if NETFRAMEWORK
using Cuga.Data.DataStruct.PMT;

#endif

namespace Core.Wcf.Models.Laser;

/// <summary>
/// 激光校准对象
/// </summary>
[Serializable]
public sealed class CalibrationLaserObj
{
    /// <summary>
    /// 暗场自动聚焦, AB两路灯亮度校准对象
    /// </summary>
    public CalibrationLaserAutoFocus CalibrationLaserAutoFocus { get; set; } = new CalibrationLaserAutoFocus();

    /// <summary>
    /// 台面功率计校准对象
    /// </summary>
    public CalibrationLaserOpticalPower[] CalibrationLaserOpticalPowerList { get; set; } = Array.Empty<CalibrationLaserOpticalPower>();

    /// <summary>
    /// Attenuator校准对象
    /// </summary>
    public CalibrationAttenuatorObj[] CalibrationAttenuatorList { get; set; } = Array.Empty<CalibrationAttenuatorObj>();

    /// <summary>
    /// AOD延迟时间校准对象列表
    /// </summary>
    public CalibrationLaserAodDelayItem[] CalibrationLaserAodDelayItemList { get; set; } = Array.Empty<CalibrationLaserAodDelayItem>();

    /// <summary>
    /// 均匀性校准对象
    /// </summary>
    public CalibrationLaserIlluminationProfileItem[] CalibrationLaserIlluminationProfileItemList { get; set; } = Array.Empty<CalibrationLaserIlluminationProfileItem>();

    /// <summary>
    /// XTC
    /// </summary>
    public CalibrationLaserXTCCalibrationItem[] CalibrationLaserXtcCalibrationItemList { get; set; } = Array.Empty<CalibrationLaserXTCCalibrationItem>();

    /// <summary>
    /// AGC延迟时间校准对象列表
    /// </summary>
    public CalibrationLaserPmtAgcDelayItem[] CalibrationLaserPmtAgcDelayItemList { get; set; } = Array.Empty<CalibrationLaserPmtAgcDelayItem>();

    /// <summary>
    /// 暗场相机的Y像素尺寸校准对象列表
    /// </summary>
    public CalibrationLaserPixelSizeItem[] CalibrationLaserPixelSizeItemList { get; set; } = Array.Empty<CalibrationLaserPixelSizeItem>();

    /// <summary>
    /// XPixelSizer校准对象
    /// </summary>
    public CalibrationLaserXPixelSizeItem[] CalibrationLaserXPixelSizeList { get; set; } = Array.Empty<CalibrationLaserXPixelSizeItem>();

    /// <summary>
    /// 暗场相机的光斑中心校准对象列表
    /// </summary>
    public CalibrationLaserLineCentricityItem[] CalibrationLaserLineCentricityItemList { get; set; } = Array.Empty<CalibrationLaserLineCentricityItem>();

    /// <summary>
    /// 暗场相机的Swath扫描正反向误差校准对象列表
    /// </summary>
    public CalibrationLaserLineOrientationOffsetItem[] CalibrationLaserLineOrientationOffsetItemList { get; set; } = Array.Empty<CalibrationLaserLineOrientationOffsetItem>();

    /// <summary>
    /// 暗场AOD散光校准对象列表
    /// </summary>
    public CalibrationLaserXYAstigmatismItem[] CalibrationLaserXYAstigmatismItemList { get; set; } = Array.Empty<CalibrationLaserXYAstigmatismItem>();

    /// <summary>
    /// 暗场DOE角度校准对象
    /// </summary>
    public CalibrationLaserDOEAngle CalibrationLaserDoeAngle { get; set; } = new();

    /// <summary>
    /// CIB MMD 校准对象列表
    /// </summary>
    public CalibrationLaserCIBMMDItem[] CalibrationLaserCIBMMDItems { get; set; } = Array.Empty<CalibrationLaserCIBMMDItem>();

    /// <summary>
    /// CIB Light Matching 校准对象列表
    /// </summary>
    public CalibrationLaserCIBLightMatchingItem[] CalibrationLaserCIBLightMatchingItems { get; set; } = Array.Empty<CalibrationLaserCIBLightMatchingItem>();

    /// <summary>
    /// CIB Illumination Profile 校准对象列表
    /// </summary>
    public CalibrationLaserCIBIlluminationProfileItem[] CalibrationLaserCIBIlluminationProfileItems { get; set; } = Array.Empty<CalibrationLaserCIBIlluminationProfileItem>();

    /// <summary>
    /// Optics Relay 校准对象列表
    /// </summary>
    public CalibrationOpticsRelay[] CalibrationOpticsRelays { get; set; } = Array.Empty<CalibrationOpticsRelay>();

    /// <summary>
    /// Optics INC 校准对象列表
    /// </summary>
    public CalibrationOpticsINC[] CalibrationOpticsINCs { get; set; } = Array.Empty<CalibrationOpticsINC>();
}

/// <summary>
/// 暗场自动聚焦, AB两路灯亮度校准对象
/// </summary>
[Serializable]
public sealed class CalibrationLaserAutoFocus : CalibrationBase
{
    /// <summary>
    /// A路灯的电流值(绝对电流值), **需要下发AF硬件**
    /// </summary>
    public double CurrentA { get; set; }

    /// <summary>
    /// B路灯的电流值(绝对电流值), **需要下发AF硬件**
    /// </summary>
    public double CurrentB { get; set; }

    /// <summary>
    /// Nsc 增益归一化, **需要下发AF硬件** 【需要 * 1000下发】
    /// </summary>
    public double NscGain { get; set; }

    /// <summary>
    /// AF  电机位置和NSC换算关系,斜率系数
    /// </summary>
    public double EcsMotorPositionRelationSlope { get; set; }

    /// <summary>
    /// AF  电机位置和NSC换算关系,截距系数
    /// </summary>
    public double EcsMotorPositionRelationIntercept { get; set; }
}

/// <summary>
/// 台面功率计校准对象
/// </summary>
[Serializable]
public sealed class CalibrationLaserOpticalPower : CalibrationBase
{
    /// <summary>
    /// 入射方式
    /// </summary>
    public CgNIOIType CgNIOITypeEnum { get; set; }

    /// <summary>
    /// Mag类型
    /// </summary>
    public CgMagTypeEnum CgMagTypeEnum { get; set; }

    /// <summary>
    /// 测试的功率系数
    /// </summary>
    public double Coefficient { get; set; }

    /// <summary>
    /// 当前暗场Mag的测量的最大功率, **Cuga内部使用**
    /// </summary>
    public double MeasureMaxPower { get; set; }

    /// <summary>
    /// 当前暗场Mag的测量的最大功率机械坐标, **Cuga内部使用**
    /// </summary>
    public CgPoint MeasureMaxPowerPosition { get; set; }
}

/// <summary>
/// Attenuator校准对象
/// </summary>
[Serializable]
public sealed class CalibrationAttenuatorObj : CalibrationBase
{
    /// <summary>
    /// 入射方式
    /// </summary>
    public CgNIOIType CgNIOITypeEnum { get; set; }

    /// <summary>
    /// Mag类型
    /// </summary>
    public CgMagTypeEnum CgMagTypeEnum { get; set; }

    /// <summary>
    /// 最大的功率系数台面功率计的平均值P
    /// </summary>
    public double MaxCoefficientAverageMeasurePower { get; set; }

    /// <summary>
    /// 台面功率与系数C之间的曲线值(C,Pc)曲线
    /// </summary>
    public IReadOnlyList<CgPoint> CoefficientMeasurePowerPoints { get; set; }

    /// <summary>
    /// 台面功率与系数C之间的曲线值(C,Pc/P)曲线
    /// </summary>
    public IReadOnlyList<CgPoint> CoefficientMeasurePowerRatePoints { get; set; }

    /// <summary>
    /// 系数曲线通过三次多项式拟合的系数: 0次方
    /// </summary>
    public double P0 { get; set; }

    /// <summary>
    /// 系数曲线通过三次多项式拟合的系数: 1次方
    /// </summary>
    public double P1 { get; set; }

    /// <summary>
    /// 系数曲线通过三次多项式拟合的系数: 2次方
    /// </summary>
    public double P2 { get; set; }

    /// <summary>
    /// 系数曲线通过三次多项式拟合的系数: 3次方
    /// </summary>
    public double P3 { get; set; }

    /// <summary>
    /// 系数曲线通过三次多项式拟合的相关系数
    /// </summary>
    public double RSquared { get; set; }

    /// <summary>
    /// 系数曲线通过三次多项式拟合后的曲线值
    /// </summary>
    public IReadOnlyList<CgPoint> CoefficientFitMeasurePowerRatePoints { get; set; }

    /// <summary>
    /// 校准间隔时间s
    /// </summary>
    public double WaitTime { get; set; }
}

/// <summary>
/// AOD延迟时间校准对象
/// </summary>
public sealed class CalibrationLaserAodDelayItem : CalibrationBase
{
    /// <summary>
    /// 入射方式
    /// </summary>
    public CgNIOIType CgNIOITypeEnum { get; set; }

    /// <summary>
    /// 暗场Mag类型
    /// </summary>
    public CgMagTypeEnum CgMagTypeEnum { get; set; }

    /// <summary>
    /// 当前暗场Mag的Prescan AOD 延迟时间(绝对延迟时间), **需要下发Laser硬件**
    /// </summary>
    public double PrescanAodDelayTime { get; set; }

    /// <summary>
    /// 当前暗场Mag的Chirp AOD 延迟时间(绝对延迟时间), **需要下发Laser硬件**
    /// </summary>
    public double ChirpAodDelayTime { get; set; }
}

/// <summary>
/// 均匀性校准对象
/// </summary>
[Serializable]
public sealed class CalibrationLaserIlluminationProfileItem : CalibrationBase
{
    /// <summary>
    /// 波形功率系数(1表示100%, 0表示0%)
    /// </summary>
    public double Coefficient { get; set; }

    /// <summary>
    /// Mag类型
    /// </summary>
    public CgMagTypeEnum OpticsMagTypeEnum { get; set; }

    /// <summary>
    /// 当前暗场Mag和功率系数下的结果prescan文件路径, **需要下发Laser硬件**
    /// </summary>
    public CalibrationPrescanAODWaveformResult[] CalibrationPrescanAODWaveformResults { get; set; }

    /// <summary>
    /// 当前暗场Mag和功率系数下的P偏振功率, **Cuga内部使用**
    /// </summary>
    public double PolarizationPPower { get; set; }

    /// <summary>
    /// 当前暗场Mag和功率系数下的S偏振功率, **Cuga内部使用**
    /// </summary>
    public double PolarizationSPower { get; set; }

    /// <summary>
    /// 当前暗场Mag和功率系数下的C偏振功率, **Cuga内部使用**
    /// </summary>
    public double PolarizationCPower { get; set; }
}

/// <summary>
/// LaserXTCCalibration
/// </summary>
[Serializable]
public sealed class CalibrationLaserXTCCalibrationItem : CalibrationBase
{
    /// <summary>
    /// Mag类型
    /// </summary>
    public CgMagTypeEnum CgMagTypeEnum { get; set; }

    /// <summary>
    /// 暗场相机ID
    /// </summary>
    public int PmtId { get; set; }

    /// <summary>
    /// 当前暗场Mag和PmtId下的通道1延迟时间, **需要下发Laser硬件**
    /// </summary>
    public int CH1Delay { get; set; }

    /// <summary>
    /// 当前暗场Mag和PmtId下的通道2延迟时间, **需要下发Laser硬件**
    /// </summary>
    public int CH2Delay { get; set; }

    /// <summary>
    /// 当前暗场Mag和PmtId下的通道3延迟时间, **需要下发Laser硬件**
    /// </summary>
    public int CH3Delay { get; set; }
}

/// <summary>
/// Pmt Agc Delay
/// </summary>
[Serializable]
public sealed class CalibrationLaserPmtAgcDelayItem : CalibrationBase
{
    /// <summary>
    /// Mag类型
    /// </summary>
    public CgMagTypeEnum CgMagTypeEnum { get; set; }

    /// <summary>
    /// 暗场相机ID
    /// </summary>
    public int PmtId { get; set; }

    /// <summary>
    /// 当前暗场Mag和PmtId下的通道1 AGC延迟时间, **需要下发Laser硬件**
    /// </summary>
    public double Channel1AgcDelay { get; set; }

    /// <summary>
    /// 当前暗场Mag和PmtId下的通道2 AGC延迟时间, **需要下发Laser硬件**
    /// </summary>
    public double Channel2AgcDelay { get; set; }

    /// <summary>
    /// 当前暗场Mag和PmtId下的通道3 AGC延迟时间, **需要下发Laser硬件**
    /// </summary>
    public double Channel3AgcDelay { get; set; }
}

/// <summary>
/// 暗场相机的Y像素尺寸校准对象
/// </summary>
[Serializable]
public sealed class CalibrationLaserPixelSizeItem : CalibrationBase
{
    /// <summary>
    /// 入射方式
    /// </summary>
    public CgNIOIType CgNIOITypeEnum { get; set; }

    /// <summary>
    /// 暗场Mag类型
    /// </summary>
    public CgMagTypeEnum CgMagTypeEnum { get; set; }

    /// <summary>
    /// 暗场相机ID
    /// </summary>
    public int PmtId { get; set; }

    /// <summary>
    /// 当前暗场Mag和PmtId下的Y方向1像素转尺寸, 单位um/pixel, **Cuga内部使用**
    /// </summary>
    public double YPixelSize { get; set; }
}

/// <summary>
/// 暗场相机的X像素尺寸校准对象
/// </summary>
[Serializable]
public sealed class CalibrationLaserXPixelSizeItem : CalibrationBase
{
    /// <summary>
    /// 入射方式
    /// </summary>
    public CgNIOIType CgNIOITypeEnum { get; set; }

    /// <summary>
    /// Mag类型
    /// </summary>
    public CgMagTypeEnum CgMagTypeEnum { get; set; }

    /// <summary>
    /// 速度
    /// </summary>
    public CgSpeedLevelType Speed { get; set; }

    /// <summary>
    /// 当前暗场Mag和速度下的X方向1像素转尺寸, 单位um/pixel, **Cuga内部使用**
    /// </summary>
    public double XPixelSize { get; set; }
}

/// <summary>
/// 中心与明场中心的offset校准
/// </summary>
[Serializable]
public sealed class CalibrationLaserLineCentricityItem : CalibrationBase
{
    /// <summary>
    /// 入射方式
    /// </summary>
    public CgNIOIType CgNIOITypeEnum { get; set; }

    /// <summary>
    /// 此显微镜镜头下做的校准
    /// </summary>
    public CgMicroscopeLens CgMicroscopeLens { get; set; }

    /// <summary>
    /// Mag类型
    /// </summary>
    public CgMagTypeEnum CgMagTypeEnum { get; set; }

    /// <summary>
    /// 速度
    /// </summary>
    public CgSpeedLevelType Speed { get; set; }

    /// <summary>
    /// 暗场相机ID
    /// </summary>
    public int PmtId { get; set; }

    /// <summary>
    /// 当前暗场Mag和速度PmtId下的基于<see cref="CgMicroscopeLens"/>倍镜下, 正向暗场中心坐标, **Cuga内部使用, 8号光斑需要下发到AF硬件**
    /// </summary>
    public CgPoint DarkMachineCenterPosition { get; set; }
}

/// <summary>
/// swath路径正反向扫描offset校准(机械坐标差值)
/// </summary>
[Serializable]
public sealed class CalibrationLaserLineOrientationOffsetItem : CalibrationBase
{
    /// <summary>
    /// 此显微镜镜头下做的校准
    /// </summary>
    public CgMicroscopeLens CgMicroscopeLens { get; set; }

    /// <summary>
    /// Mag类型
    /// </summary>
    public CgMagTypeEnum CgMagTypeEnum { get; set; }

    /// <summary>
    /// 速度
    /// </summary>
    public CgSpeedLevelType Speed { get; set; }

    /// <summary>
    /// 暗场相机ID
    /// </summary>
    public int PmtId { get; set; }

    /// <summary>
    /// 当前暗场Mag和速度PmtId下的基于<see cref="CgMicroscopeLens"/>倍镜下, 正反向误差值
    /// </summary>
    public CgPoint Offset { get; set; }
}

/// <summary>
/// DOE角度校准
/// </summary>
[Serializable]
public sealed class CalibrationLaserDOEAngle : CalibrationBase
{
    public double DOEAngle { get; set; }
}

/// <summary>
/// 暗场散光校准
/// </summary>
[Serializable]
public sealed class CalibrationLaserXYAstigmatismItem : CalibrationBase
{
    /// <summary>
    /// Mag类型
    /// </summary>
    public CgMagTypeEnum CgMagTypeEnum { get; set; }

    public CalibrationChirpAODWaveformResult[] ChirpAODWaveformResultList { get; set; }
}

/// <summary>
/// Prescan波形结果
/// </summary>
[Serializable]
public class CalibrationPrescanAODWaveformResult
{
#if NETFRAMEWORK
    /// <summary>
    /// 电极Id
    /// </summary>
    public CgAwgElectrodeEnum OpticsAODElectrodeEnum { get; set; }

#else
    /// <summary>
    /// 电极Id
    /// </summary>
    public int OpticsAODElectrodeEnum { get; set; }
#endif

    /// <summary>
    /// 波形文件路径
    /// </summary>
    public string FilePath { get; set; } = string.Empty;
}

/// <summary>
/// Chirp 波形结果
/// </summary>
[Serializable]
public sealed class CalibrationChirpAODWaveformResult
{
#if NETFRAMEWORK
    /// <summary>
    /// 电极Id
    /// </summary>
    public CgAwgElectrodeEnum OpticsAODElectrodeEnum { get; set; }

#else
    /// <summary>
    /// 电极Id
    /// </summary>
    public int OpticsAODElectrodeEnum { get; set; }
#endif

    public string FilePath { get; set; }
}

/// <summary>
/// CIB MMD 校准
/// </summary>
[Serializable]
public sealed class CalibrationLaserCIBMMDItem : CalibrationBase
{
    /// <summary>
    /// CIB PMT ID
    /// </summary>
    public int PMTId { get; set; }

    /// <summary>
    /// CIB Channel ID
    /// </summary>
    public int ChannelId { get; set; }

    /// <summary>
    /// LogGain * 128 [0, 4095], **需要下发CIB硬件**
    /// </summary>
    public IReadOnlyList<double> LogGainMul128U12Bits { get; set; }

    /// <summary>
    /// GainS16Bit [-2^15, 2^15-1], **需要下发CIB硬件**
    /// </summary>
    public IReadOnlyList<double> GainS16Bits { get; set; }

    /// <summary>
    /// LogGain 最大值, **需要下发CIB硬件**
    /// </summary>
    public double MaxLogGain { get; set; }
}

/// <summary>
/// CIB Light Matching 校准
/// </summary>
[Serializable]
public sealed class CalibrationLaserCIBLightMatchingItem : CalibrationBase
{
    /// <summary>
    /// 入射方式
    /// </summary>
    public CgNIOIType CgNIOITypeEnum { get; set; }

    /// <summary>
    /// Mag类型
    /// </summary>
    public CgMagTypeEnum CgMagTypeEnum { get; set; }

    /// <summary>
    /// 速度
    /// </summary>
    public CgSpeedLevelType Speed { get; set; }

    /// <summary>
    /// 光学 Apodization
    /// </summary>
    public int OpticsApodizationModeEnum { get; set; }

    /// <summary>
    /// 光学偏振
    /// </summary>
    public int OpticsPolarizationModeEnum { get; set; }

    /// <summary>
    /// 采集偏振
    /// </summary>
    public int CollectorPolarizationModeEnum { get; set; }

    /// <summary>
    /// 校准结果, **需要下发CIB硬件**
    /// </summary>
    public IReadOnlyList<Item> Items { get; set; }

    /// <summary>
    /// 每个CIB的校准结果
    /// </summary>
    public sealed class Item
    {
        /// <summary>
        /// CIB PMT ID
        /// </summary>
        public int PMTId { get; set; }

        /// <summary>
        /// CIB Channel ID
        /// </summary>
        public int ChannelId { get; set; }

        /// <summary>
        /// 数码增益
        /// </summary>
        public double DigitalGainPlusMultiplicativeFactors { get; set; }
    }
}

/// <summary>
/// CIB Illumination Profile 校准
/// </summary>
[Serializable]
public sealed class CalibrationLaserCIBIlluminationProfileItem : CalibrationBase
{
    /// <summary>
    /// 入射方式
    /// </summary>
    public CgNIOIType CgNIOITypeEnum { get; set; }

    /// <summary>
    /// Mag类型
    /// </summary>
    public CgMagTypeEnum CgMagTypeEnum { get; set; }

    /// <summary>
    /// 速度
    /// </summary>
    public CgSpeedLevelType Speed { get; set; }

    /// <summary>
    /// 光学 Apodization
    /// </summary>
    public int OpticsApodizationModeEnum { get; set; }

    /// <summary>
    /// 光学偏振
    /// </summary>
    public int OpticsPolarizationModeEnum { get; set; }

    /// <summary>
    /// 采集偏振
    /// </summary>
    public int CollectorPolarizationModeEnum { get; set; }

    /// <summary>
    /// 校准结果, **需要下发CIB硬件**
    /// </summary>
    public IReadOnlyList<Item> Items { get; set; }

    /// <summary>
    /// 每个CIB的校准结果
    /// </summary>
    public sealed class Item
    {
        /// <summary>
        /// CIB PMT ID
        /// </summary>
        public int PMTId { get; set; }

        /// <summary>
        /// CIB Channel ID
        /// </summary>
        public int ChannelId { get; set; }

        /// <summary>
        /// 均匀性校准结果
        /// </summary>
        public IReadOnlyList<double> IlluminationProfiles { get; set; }
    }
}

/// <summary>
/// Optics Relay 校准
/// </summary>
[Serializable]
public sealed class CalibrationOpticsRelay : CalibrationBase
{
    /// <summary>
    /// 入射方式
    /// </summary>
    public CgNIOIType CgNIOITypeEnum { get; set; }

    /// <summary>
    /// 斜率(ECS/mm), **Cuga内部使用**
    /// </summary>
    public double Slope { get; set; }

    /// <summary>
    /// Relay 工作范围 最小值, **Cuga内部使用**
    /// </summary>
    public double MinRelayMotorAbsoluteValue { get; set; }

    /// <summary>
    /// Relay 工作范围 最大值, **Cuga内部使用**
    /// </summary>
    public double MaxRelayMotorAbsoluteValue { get; set; }
}

/// <summary>
/// Optics INC 校准
/// </summary>
[Serializable]
public sealed class CalibrationOpticsINC : CalibrationBase
{
    /// <summary>
    /// 入射方式
    /// </summary>
    public CgNIOIType CgNIOITypeEnum { get; set; }

    /// <summary>
    /// Mag类型
    /// </summary>
    public CgMagTypeEnum CgMagTypeEnum { get; set; }

    /// <summary>
    /// 速度
    /// </summary>
    public CgSpeedLevelType Speed { get; set; }

    /// <summary>
    /// INC电机位置, **需要下发Optics Motor硬件**
    /// </summary>
    public double? INCMotorAbsoluteValue { get; set; }
}