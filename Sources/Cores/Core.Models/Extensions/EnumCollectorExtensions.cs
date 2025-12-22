using CommunityToolkit.Diagnostics;
using Core.Models.Enums.Collector;
using Cuga.Data.DataStruct.Optics;

namespace Core.Models.Extensions;

public static class EnumCollectorExtensions
{
    public static CollectorPolarizationModeEnum ToCollectorPolarizationModeEnum(this CgNDFTypeEnum @this) => @this switch
    {
#if NETFRAMEWORK
        CgNDFTypeEnum.None => CollectorPolarizationModeEnum.None,
#endif
        CgNDFTypeEnum.P => CollectorPolarizationModeEnum.P,
        CgNDFTypeEnum.S => CollectorPolarizationModeEnum.S,
        _ => ThrowHelper.ThrowArgumentOutOfRangeException<CollectorPolarizationModeEnum>(nameof(@this))
    };

    public static CgNDFTypeEnum ToCgNDFTypeEnum(this CollectorPolarizationModeEnum @this) => @this switch
    {
#if NETFRAMEWORK
        CollectorPolarizationModeEnum.None => CgNDFTypeEnum.None,
#endif
        CollectorPolarizationModeEnum.P => CgNDFTypeEnum.P,
        CollectorPolarizationModeEnum.S => CgNDFTypeEnum.S,
        _ => ThrowHelper.ThrowArgumentOutOfRangeException<CgNDFTypeEnum>(nameof(@this))
    };
}