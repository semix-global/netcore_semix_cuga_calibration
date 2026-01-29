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
    /// GFO校准对象列表
    /// </summary>
    public CalibrationGlobalFocusOffset[] CalibrationGlobalFocusOffsets { get; set; } = Array.Empty<CalibrationGlobalFocusOffset>();
}

/// <summary>
/// 暗场CalChip DSW焦点位置校准对象
/// </summary>
[Serializable]
public sealed class CalibrationGlobalFocusOffset : CalibrationBase
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