using System.ComponentModel;

namespace Core.Models.Enums.Optics;

/// <summary>
/// 照明切趾模式
/// </summary>
public enum OpticsApodizationModeEnum
{
    [Description("Gaussian")]
    Gaussian,

    [Description("Super Gaussian")]
    SuperGaussian,

    [Description("Cosine")]
    Cosine
}