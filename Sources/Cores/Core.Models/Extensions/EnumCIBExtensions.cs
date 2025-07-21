using CommunityToolkit.Diagnostics;
using Core.Models.Enums.CIB;

namespace Core.Models.Extensions;

public static class EnumCIBExtensions
{
    /// <summary>
    /// 0.Profile_PMT 1.PMT_Volt 2.Profile_Log 3.PMT_Log 4.Sense_Volt
    /// </summary>
    public static int ToCIBProfile(this CIBProfileTypeEnum cibProfileTypeEnum) => cibProfileTypeEnum switch
    {
        CIBProfileTypeEnum.PMTVoltage => 1,
        CIBProfileTypeEnum.PMTLog => 3,
        _ => ThrowHelper.ThrowArgumentOutOfRangeException<int>(nameof(cibProfileTypeEnum))
    };

    /// <summary>
    /// 0.Profile_PMT 1.PMT_Volt 2.Profile_Log 3.PMT_Log 4.Sense_Volt
    /// </summary>
    public static CIBProfileTypeEnum ToCIBProfileEnum(this int cibProfileType) => cibProfileType switch
    {
        1 => CIBProfileTypeEnum.PMTVoltage,
        3 => CIBProfileTypeEnum.PMTLog,
        _ => ThrowHelper.ThrowArgumentOutOfRangeException<CIBProfileTypeEnum>(nameof(cibProfileType))
    };
}