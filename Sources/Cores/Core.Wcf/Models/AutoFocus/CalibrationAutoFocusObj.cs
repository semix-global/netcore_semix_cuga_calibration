using Cuga.Data.DataStruct.DTO.Swath;
using Cuga.Data.DataStruct.Optics;
using System;

namespace Core.Wcf.Models.AutoFocus;

/// <summary>
/// AutoFocus校准对象
/// </summary>
[Serializable]
public class CalibrationAutoFocusObj
{
    /// <summary>
    /// 暗场自动聚焦, FA FB补偿系数校准对象
    /// </summary>
    public CalibrationLaserAutoFocusFAFBCompensation CalibrationLaserAutoFocusFAFBCompensation { get; set; } = new();

    /// <summary>
    /// GFO校准对象列表
    /// </summary>
    public CalibrationAutoFocusGlobalFocusOffset[] CalibrationAutoFocusGlobalFocusOffsets { get; set; } = [];

    /// <summary>
    /// CalChipFocusOffset校准对象列表
    /// </summary>
    public CalibrationAutoFocusCalChipFocusOffset CalibrationAutoFocusCalChipFocusOffset { get; set; } = new();
}

/// <summary>
/// 暗场自动聚焦, FA FB补偿系数校准对象
/// target = FA / NA + KA * (1 - OffsetA / NA)
/// target = FB / NB + KB * (1 - OffsetB / NB)
/// </summary>
[Serializable]
public sealed class CalibrationLaserAutoFocusFAFBCompensation : CalibrationBase
{
    /// <summary>
    /// FA补偿系数, **需要下发AF硬件**
    /// </summary>
    public double KA { get; set; }

    /// <summary>
    /// FA补偿系数, **需要下发AF硬件**
    /// </summary>
    public double OffsetA { get; set; }

    /// <summary>
    /// FB补偿系数, **需要下发AF硬件**
    /// </summary>
    public double KB { get; set; }

    /// <summary>
    /// FB补偿系数, **需要下发AF硬件**
    /// </summary>
    public double OffsetB { get; set; }
}

/// <summary>
/// 暗场CalChip DSW焦点位置校准对象
/// </summary>
[Serializable]
public sealed class CalibrationAutoFocusGlobalFocusOffset : CalibrationBase
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
    /// 伺服电机 True:AF, False:Relay, **Cuga内部使用**
    /// </summary>
    public bool IsAFServo { get; set; }

    /// <summary>
    /// 焦点ECS, **Cuga内部使用**
    /// </summary>
    public double ECSValue { get; set; }

    /// <summary>
    /// 电机值, **Cuga内部使用**
    /// </summary>
    public double MotorValue { get; set; }
}

/// <summary>
/// 暗场Cal Chip焦点位置校准对象
/// </summary>
[Serializable]
public sealed class CalibrationAutoFocusCalChipFocusOffset : CalibrationBase
{
    /// <summary>
    /// Chuck暗场最佳Ecs
    /// </summary>
    public double ChuckEcsValue { get; set; }

    /// <summary>
    /// Dsw暗场最佳Ecs
    /// </summary>
    public double DswEcsValue { get; set; }

    /// <summary>
    /// Haze暗场最佳Ecs
    /// </summary>
    public double HazeEcsValue { get; set; }

    /// <summary>
    /// Undefined暗场最佳Ecs
    /// </summary>
    public double UndefineEcsValue { get; set; }

    /// <summary>
    /// ShinyWafer暗场最佳Ecs
    /// </summary>
    public double ShinyWaferEcsValue { get; set; }
}