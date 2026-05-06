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
    public CalibrationLaserAutoFocus CalibrationLaserAutoFocus { get; set; } = new();

    /// <summary>
    /// 台面功率计校准对象
    /// </summary>
    public CalibrationLaserOpticalPower[] CalibrationLaserOpticalPowerList { get; set; } = [];

    /// <summary>
    /// Attenuator校准对象
    /// </summary>
    public CalibrationAttenuatorObj[] CalibrationAttenuatorList { get; set; } = [];

    /// <summary>
    /// AOD延迟时间校准对象列表
    /// </summary>
    public CalibrationLaserAodDelayItem[] CalibrationLaserAodDelayItemList { get; set; } = [];

    /// <summary>
    /// 暗场相机的Y像素尺寸校准对象列表
    /// </summary>
    public CalibrationLaserPixelSizeItem[] CalibrationLaserPixelSizeItemList { get; set; } = [];

    /// <summary>
    /// XPixelSizer校准对象
    /// </summary>
    public CalibrationLaserXPixelSizeItem[] CalibrationLaserXPixelSizeList { get; set; } = [];

    /// <summary>
    /// 暗场相机的光斑中心校准对象列表
    /// </summary>
    public CalibrationLaserLineCentricityItem[] CalibrationLaserLineCentricityItemList { get; set; } = [];

    /// <summary>
    /// 暗场相机的Swath扫描正反向误差校准对象列表
    /// </summary>
    public CalibrationCIBLineOrientationOffsetItem[] CalibrationLaserLineOrientationOffsetItemList { get; set; } = [];

    /// <summary>
    /// 暗场DOE角度校准对象
    /// </summary>
    public CalibrationLaserDOEAngle[] CalibrationLaserDoeAngleItems { get; set; } = [];

    /// <summary>
    /// CIB MMD 校准对象列表
    /// </summary>
    public CalibrationLaserCIBMMDItem[] CalibrationLaserCIBMMDItems { get; set; } = [];

    /// <summary>
    /// CIB Light Matching 校准对象列表
    /// </summary>
    public CalibrationLaserCIBLightMatchingItem[] CalibrationLaserCIBLightMatchingItems { get; set; } = [];

    /// <summary>
    /// CIB Illumination Profile 校准对象列表
    /// </summary>
    public CalibrationLaserCIBIlluminationProfileItem[] CalibrationLaserCIBIlluminationProfileItems { get; set; } = [];

    /// <summary>
    /// CIB XTC 校准对象列表
    /// </summary>
    public CalibrationLaserCIBXTCItem[] CalibrationLaserCIBXTCItems { get; set; } = [];

    /// <summary>
    /// CIB AGC Delay 校准对象列表
    /// </summary>
    public CalibrationLaserCIBAGCDelayItem[] CalibrationLaserCIBAGCDelayItems { get; set; } = [];

    /// <summary>
    /// AOD Uniformity 校准对象列表
    /// </summary>
    public CalibrationLaserAODUniformityItem[] CalibrationLaserAODUniformityItems { get; set; } = [];

    /// <summary>
    /// Optics Relay 校准对象列表
    /// </summary>
    public CalibrationOpticsRelay[] CalibrationOpticsRelays { get; set; } = [];

    /// <summary>
    /// Optics INC 校准对象列表
    /// </summary>
    public CalibrationOpticsINC[] CalibrationOpticsINCs { get; set; } = [];

    /// <summary>
    /// 采集偏振校准, CollectionPolarization对象数据
    /// </summary>
    public CalibrationCollectionPolarization CalibrationCollectionPolarization { get; set; } = new CalibrationCollectionPolarization();
}

/// <summary>
/// 暗场自动聚焦, AB两路灯亮度校准对象
/// </summary>
[Serializable]
public sealed class CalibrationLaserAutoFocus : CalibrationBase
{
    /// <summary>
    /// 中档: A路灯的电流值(绝对电流值), **需要下发AF硬件**
    /// </summary>
    public double MiddleCurrentA { get; set; }

    /// <summary>
    /// 中档: B路灯的电流值(绝对电流值), **需要下发AF硬件**
    /// </summary>
    public double MiddleCurrentB { get; set; }

    /// <summary>
    /// 低档系数
    /// </summary>
    public double LowCoefficient { get; set; } = 0.6d;

    /// <summary>
    /// 高档系数
    /// </summary>
    public double HighCoefficient { get; set; } = 1.5d;

    /// <summary>
    /// 低档: A路灯的电流值(绝对电流值), **需要下发AF硬件**
    /// </summary>
    public double LowCurrentA => LowCoefficient * MiddleCurrentA;

    /// <summary>
    /// 低档: B路灯的电流值(绝对电流值), **需要下发AF硬件**
    /// </summary>
    public double LowCurrentB => LowCoefficient * MiddleCurrentB;

    /// <summary>
    /// 高档: A路灯的电流值(绝对电流值), **需要下发AF硬件**
    /// </summary>
    public double HighCurrentA => HighCoefficient * MiddleCurrentA;

    /// <summary>
    /// 高档: B路灯的电流值(绝对电流值), **需要下发AF硬件**
    /// </summary>
    public double HighCurrentB => HighCoefficient * MiddleCurrentB;

    /// <summary>
    /// Nsc 增益归一化, **需要下发AF硬件** 【需要 * 1000下发】
    /// </summary>
    public double NscGain { get; set; }

    /// <summary>
    /// 斜率(ECS/mm), **Cuga内部使用**
    /// </summary>
    public double Slope { get; set; }

    /// <summary>
    /// AF 工作范围 最小值, **Cuga内部使用**
    /// </summary>
    public double MinAFMotorAbsoluteValue { get; set; }

    /// <summary>
    /// AF 工作范围 最大值, **Cuga内部使用**
    /// </summary>
    public double MaxAFMotorAbsoluteValue { get; set; }
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

    public string CgNIOIType => CgNIOITypeEnum.ToString();

    /// <summary>
    /// Mag类型
    /// </summary>
    public CgMagTypeEnum CgMagTypeEnum { get; set; }

    public string CgMagType => CgMagTypeEnum.ToString();


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

    public string CgNIOIType => CgNIOITypeEnum.ToString();

    /// <summary>
    /// Mag类型
    /// </summary>
    public CgMagTypeEnum CgMagTypeEnum { get; set; }

    public string CgMagType => CgMagTypeEnum.ToString();

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
    /// 饱和系数
    /// </summary>
    public double SaturationCoefficient { get; set; }

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

    public string CgNIOIType => CgNIOITypeEnum.ToString();

    /// <summary>
    /// 暗场Mag类型
    /// </summary>
    public CgMagTypeEnum CgMagTypeEnum { get; set; }

    public string CgMagType => CgMagTypeEnum.ToString();

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
/// 暗场相机的Y像素尺寸校准对象
/// </summary>
[Serializable]
public sealed class CalibrationLaserPixelSizeItem : CalibrationBase
{
    /// <summary>
    /// 入射方式
    /// </summary>
    public CgNIOIType CgNIOITypeEnum { get; set; }

    public string CgNIOIType => CgNIOITypeEnum.ToString();

    /// <summary>
    /// 暗场Mag类型
    /// </summary>
    public CgMagTypeEnum CgMagTypeEnum { get; set; }

    public string CgMagType => CgMagTypeEnum.ToString();

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

    public string CgNIOIType => CgNIOITypeEnum.ToString();

    /// <summary>
    /// Mag类型
    /// </summary>
    public CgMagTypeEnum CgMagTypeEnum { get; set; }

    public string CgMagType => CgMagTypeEnum.ToString();

    /// <summary>
    /// 速度
    /// </summary>
    public CgSpeedLevelType Speed { get; set; }

    public string SpeedString => Speed.ToString();

    /// <summary>
    /// 当前暗场Mag和速度下的X方向1像素转尺寸, 单位um/pixel, **Cuga内部使用**
    /// </summary>
    public double XPixelSize { get; set; }

    /// <summary>
    /// 当前暗场Mag和速度下的X方向1像素转尺寸, 这个是误差变量, 图像匹配算法识别后的像素最大值减去最小值，单位为pixel, **Cuga内部使用**
    /// </summary>
    public double XPixelSizeDelta { get; set; }
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

    public string CgNIOIType => CgNIOITypeEnum.ToString();

    /// <summary>
    /// 此显微镜镜头下做的校准
    /// </summary>
    public CgMicroscopeLens CgMicroscopeLens { get; set; }

    public string CgMicroscopeLensString => CgMicroscopeLens.ToString();

    /// <summary>
    /// Mag类型
    /// </summary>
    public CgMagTypeEnum CgMagTypeEnum { get; set; }

    public string CgMagType => CgMagTypeEnum.ToString();

    /// <summary>
    /// 速度
    /// </summary>
    public CgSpeedLevelType Speed { get; set; }

    public string SpeedString => Speed.ToString();

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
public sealed class CalibrationCIBLineOrientationOffsetItem : CalibrationBase
{
    /// <summary>
    /// 此显微镜镜头下做的校准
    /// </summary>
    public CgMicroscopeLens CgMicroscopeLens { get; set; }

    public string CgMicroscopeLensString => CgMicroscopeLens.ToString();

    /// <summary>
    /// 入射方式
    /// </summary>
    public CgNIOIType CgNIOITypeEnum { get; set; }

    public string CgNIOIType => CgNIOITypeEnum.ToString();

    /// <summary>
    /// Mag类型
    /// </summary>
    public CgMagTypeEnum CgMagTypeEnum { get; set; }

    public string CgMagType => CgMagTypeEnum.ToString();

    /// <summary>
    /// 速度
    /// </summary>
    public CgSpeedLevelType Speed { get; set; }

    public string SpeedString => Speed.ToString();

    /// <summary>
    /// 暗场相机ID
    /// </summary>
    public int PmtId { get; set; }

    /// <summary>
    /// 当前暗场Mag和速度PmtId下的基于<see cref="CgMicroscopeLens"/>倍镜下, 正反向误差值
    /// </summary>
    public double XOffset { get; set; }
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

    public string CgMagType => CgMagTypeEnum.ToString();

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
    /// LogGain * 128 [0, 2^12-1], **需要下发CIB硬件**, 且最大值, **需要下发CIB硬件**
    /// </summary>
    public IReadOnlyList<double> LogGainMul128U12Bits { get; set; }

    /// <summary>
    /// GainS16Bit [-2^15, 2^15-1], **需要下发CIB硬件**
    /// </summary>
    public IReadOnlyList<double> GainS16Bits { get; set; }
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

    public string CgNIOIType => CgNIOITypeEnum.ToString();

    /// <summary>
    /// Mag类型
    /// </summary>
    public CgMagTypeEnum CgMagTypeEnum { get; set; }

    public string CgMagType => CgMagTypeEnum.ToString();

    /// <summary>
    /// 速度
    /// </summary>
    public CgSpeedLevelType Speed { get; set; }

    public string SpeedString => Speed.ToString();

    /// <summary>
    /// 光学 Apodization
    /// </summary>
    public int OpticsApodizationModeEnum { get; set; }

    /// <summary>
    /// 光学偏振
    /// </summary>
    public CgPolarizationTypeEnum OpticsPolarizationModeEnum { get; set; }

    public string OpticsPolarizationMode => OpticsPolarizationModeEnum.ToString();

    /// <summary>
    /// 采集偏振
    /// </summary>
    public CgNDFTypeEnum CollectorPolarizationModeEnum { get; set; }

    public string CollectorPolarizationMode => CollectorPolarizationModeEnum.ToString();

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
        /// 数码增益, **需要下发CIB硬件**
        /// </summary>
        public double DigitalGainPlusMultiplicativeFactors { get; set; }
    }
}

/// <summary>
/// CIB XTC 校准
/// </summary>
[Serializable]
public sealed class CalibrationLaserCIBXTCItem : CalibrationBase
{
    /// <summary>
    /// 入射方式
    /// </summary>
    public CgNIOIType CgNIOITypeEnum { get; set; }

    public string CgNIOIType => CgNIOITypeEnum.ToString();

    /// <summary>
    /// Mag类型
    /// </summary>
    public CgMagTypeEnum CgMagTypeEnum { get; set; }

    public string CgMagType => CgMagTypeEnum.ToString();

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
        /// 延迟 PMTDelay SenseDelay, **需要下发CIB硬件**
        /// </summary>
        public double Delay { get; set; }
    }
}

/// <summary>
/// CIB AGC Delay 校准
/// </summary>
[Serializable]
public sealed class CalibrationLaserCIBAGCDelayItem : CalibrationBase
{
    /// <summary>
    /// 入射方式
    /// </summary>
    public CgNIOIType CgNIOITypeEnum { get; set; }

    public string CgNIOIType => CgNIOITypeEnum.ToString();

    /// <summary>
    /// Mag类型
    /// </summary>
    public CgMagTypeEnum CgMagTypeEnum { get; set; }

    public string CgMagType => CgMagTypeEnum.ToString();

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
        /// 延迟 DACDealy, **需要下发CIB硬件**
        /// </summary>
        public double Delay { get; set; }
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

    public string CgNIOIType => CgNIOITypeEnum.ToString();

    /// <summary>
    /// Mag类型
    /// </summary>
    public CgMagTypeEnum CgMagTypeEnum { get; set; }

    public string CgMagType => CgMagTypeEnum.ToString();

    /// <summary>
    /// 速度
    /// </summary>
    public CgSpeedLevelType Speed { get; set; }

    public string SpeedString => Speed.ToString();

    /// <summary>
    /// 光学 Apodization
    /// </summary>
    public int OpticsApodizationModeEnum { get; set; }

    /// <summary>
    /// 光学偏振
    /// </summary>
    public CgPolarizationTypeEnum OpticsPolarizationModeEnum { get; set; }

    public string OpticsPolarizationMode => OpticsPolarizationModeEnum.ToString();

    /// <summary>
    /// 采集偏振
    /// </summary>
    public CgNDFTypeEnum CollectorPolarizationModeEnum { get; set; }

    public string CollectorPolarizationMode => CollectorPolarizationModeEnum.ToString();

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
        /// 均匀性校准结果, **需要下发CIB硬件**
        /// </summary>
        public IReadOnlyList<double> IlluminationProfiles { get; set; }
    }
}

/// <summary>
/// AOD Uniformitiy 校准
/// </summary>
[Serializable]
public sealed class CalibrationLaserAODUniformityItem : CalibrationBase
{
    /// <summary>
    /// 入射方式
    /// </summary>
    public CgNIOIType CgNIOITypeEnum { get; set; }

    public string CgNIOIType => CgNIOITypeEnum.ToString();

    /// <summary>
    /// Mag类型
    /// </summary>
    public CgMagTypeEnum CgMagTypeEnum { get; set; }

    public string CgMagType => CgMagTypeEnum.ToString();

    /// <summary>
    /// 波形功率系数(1表示100%, 0表示0%)
    /// </summary>
    public double Coefficient { get; set; }

    /// <summary>
    /// Uniformity 偏振功率校准结果, **需要下发AOD硬件**
    /// </summary>
    public IReadOnlyList<KeyValuePair<CgPolarizationTypeEnum, double>> OpticsPolarizationModeEnumMeasurePowers { get; set; }

    /// <summary>
    /// Uniformitiy 校准结果使用的偏振
    /// </summary>
    public CgPolarizationTypeEnum OpticsPolarizationModeEnum { get; set; }

    public string OpticsPolarizationMode => OpticsPolarizationModeEnum.ToString();

    /// <summary>
    /// Uniformity 校准结果, **需要下发AOD硬件**
    /// </summary>
    public IReadOnlyList<double> Uniformities { get; set; }
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

    public string CgNIOIType => CgNIOITypeEnum.ToString();

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
/// Optics SC 校准
/// </summary>
[Serializable]
public sealed class CalibrationOpticsSC : CalibrationBase
{
    /// <summary>
    /// 入射方式
    /// </summary>
    public CgNIOIType CgNIOITypeEnum { get; set; }

    public string CgNIOIType => CgNIOITypeEnum.ToString();

    /// <summary>
    /// SC L1,电机位置, **需要下发Optics Motor硬件**
    /// </summary>
    public double SCMotorAbsoluteValueL1 { get; set; }

    /// <summary>
    /// SC L3,电机位置, **需要下发Optics Motor硬件**
    /// </summary>
    public double SCMotorAbsoluteValueL3 { get; set; }
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

    public string CgNIOIType => CgNIOITypeEnum.ToString();

    /// <summary>
    /// Mag类型
    /// </summary>
    public CgMagTypeEnum CgMagTypeEnum { get; set; }

    public string CgMagType => CgMagTypeEnum.ToString();

    /// <summary>
    /// 速度
    /// </summary>
    public CgSpeedLevelType Speed { get; set; }

    public string SpeedString => Speed.ToString();

    /// <summary>
    /// INC电机位置, **需要下发Optics Motor硬件**
    /// </summary>
    public double? INCMotorAbsoluteValue { get; set; }
}

/// <summary>
/// DOE角度校准
/// </summary>
[Serializable]
public sealed class CalibrationLaserDOEAngle : CalibrationBase
{
    /// <summary>
    /// 入射方式
    /// </summary>
    public CgNIOIType CgNIOITypeEnum { get; set; }

    public string CgNIOIType => CgNIOITypeEnum.ToString();

    public double DOEAngle { get; set; }
}

/// <summary>
/// 采集偏振校准, CollectionPolarization校准下发Cuga参数
/// </summary>
[Serializable]
public sealed class CalibrationCollectionPolarization : CalibrationBase
{
    /// <summary>
    /// CH1通道NDF电机的S偏振位置, **需要记录**
    /// </summary> 
    public double PolarizationPositionNDFSCH1 { get; set; }

    /// <summary>
    /// CH2通道NDF电机的S偏振位置, **需要记录**
    /// </summary> 
    public double PolarizationPositionNDFSCH2 { get; set; }

    /// <summary>
    /// CH3通道NDF电机的S偏振位置, **需要记录**
    /// </summary> 
    public double PolarizationPositionNDFSCH3 { get; set; }

    /// <summary>
    /// CH1通道NDF电机的P偏振位置, **需要记录**
    /// </summary> 
    public double PolarizationPositionNDFPCH1 { get; set; }

    /// <summary>
    /// CH2通道NDF电机的P偏振位置, **需要记录**
    /// </summary> 
    public double PolarizationPositionNDFPCH2 { get; set; }

    /// <summary>
    /// CH3通道NDF电机的P偏振位置, **需要记录**
    /// </summary> 
    public double PolarizationPositionNDFPCH3 { get; set; }
}