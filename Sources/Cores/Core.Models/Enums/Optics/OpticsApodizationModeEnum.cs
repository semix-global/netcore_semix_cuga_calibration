using System.ComponentModel;

namespace Core.Models.Enums.Optics;

/// <summary>
/// 光学切趾
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