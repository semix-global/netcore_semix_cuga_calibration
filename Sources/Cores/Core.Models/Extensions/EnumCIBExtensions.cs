using CommunityToolkit.Diagnostics;
using Core.Models.Enums.CIB;

namespace Core.Models.Extensions;

public static class EnumCIBExtensions
{
    /// <summary>
    /// 0.Profile_PMT 1.PMT_Volt 2.Profile_Log 3.PMT_Log 4.Sense_Volt
    /// </summary>
    public static int ToCIBProfile(this CIBProfileModeEnum cibProfileModeEnum) => cibProfileModeEnum switch
    {
        CIBProfileModeEnum.PMTVoltage => 1,
        CIBProfileModeEnum.PMTLog => 3,
        _ => ThrowHelper.ThrowArgumentOutOfRangeException<int>(nameof(cibProfileModeEnum))
    };

    /// <summary>
    /// 0.Profile_PMT 1.PMT_Volt 2.Profile_Log 3.PMT_Log 4.Sense_Volt
    /// </summary>
    public static CIBProfileModeEnum ToCIBProfileEnum(this int cibProfileType) => cibProfileType switch
    {
        1 => CIBProfileModeEnum.PMTVoltage,
        3 => CIBProfileModeEnum.PMTLog,
        _ => ThrowHelper.ThrowArgumentOutOfRangeException<CIBProfileModeEnum>(nameof(cibProfileType))
    };
}