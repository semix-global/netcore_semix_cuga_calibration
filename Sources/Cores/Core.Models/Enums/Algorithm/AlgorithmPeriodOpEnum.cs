using System.ComponentModel;

namespace Core.Models.Enums.Algorithm;

/// <summary>
/// AlgoCSharp 投影算子（BF period_op / DF period_y_op|period_x_op）
/// </summary>
public enum AlgorithmPeriodOpEnum
{
    [Description("Mean")]
    Mean,

    [Description("Std")]
    Std,

    [Description("Range")]
    Range
}