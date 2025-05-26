using System.ComponentModel;

namespace Core.Models.Enums.Algorithm;

public enum AlgorithmWaferTypeEnum
{
    /// <summary>
    /// 8寸晶圆
    /// </summary>
    [Description("200mm")]
    D200,

    /// <summary>
    /// 12寸晶圆
    /// </summary>
    [Description("300mm")]
    D300
}