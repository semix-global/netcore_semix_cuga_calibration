using Newtonsoft.Json;
using System;

namespace Core.Wcf.Models;

[Serializable]
public class CalibrationBase
{
    /// <summary>
    /// 是否校准Ok
    /// </summary>
    public bool IsCalibrated { get; set; }

    /// <summary>
    /// 是否验证Ok
    /// </summary>
    public bool IsVerified { get; set; }

    /// <summary>
    /// Cuga初始化是否需要自检此项校准结果是否Ok
    /// </summary>
    public bool IsRequiredCalibrate { get; set; } = false;

    /// <summary>
    /// 校准结果版本号
    /// </summary>
    public string Version { get; set; }

    /// <summary>
    /// 是否Ok
    /// </summary>
    [JsonIgnore]
    public bool IsOk => IsCalibrated && IsVerified;
}