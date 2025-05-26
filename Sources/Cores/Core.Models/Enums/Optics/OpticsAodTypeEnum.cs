using System.ComponentModel;

namespace Core.Models.Enums.Optics;

public enum OpticsAodTypeEnum
{
    /// <summary>
    /// prescan AOD
    /// </summary>
    [Description("Prescan")]
    Prescan,

    /// <summary>
    /// chirp AOD
    /// </summary>
    [Description("Chirp")]
    Chirp
}