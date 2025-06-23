using Core.Models.Enums.Optics;
using Cuga.Data.DataStruct.Optics;

#if NET
using Semix.GRPC.DTO;
using Semix.GRPC.DTO.Basic;

#else
using Semix.WcfTransfer.DTO;
using Semix.WcfTransfer.DTO.Basic;

#endif

namespace Core.Models.Extensions;

public static class EnumOpticsExtension
{
    #region AodWorking

    public static int ToOpticsAodWorkingMode(this OpticsAodWorkingModeEnum mode) => mode switch
    {
        OpticsAodWorkingModeEnum.Close => 0,
        OpticsAodWorkingModeEnum.Scan => 1,
        OpticsAodWorkingModeEnum.Through => 2,
        _ => throw new ArgumentOutOfRangeException(nameof(mode), mode, null)
    };

    public static OpticsAodWorkingModeEnum ToOpticsAodWorkingModeEnum(this int mode) => mode switch
    {
        0 => OpticsAodWorkingModeEnum.Close,
        1 => OpticsAodWorkingModeEnum.Scan,
        2 => OpticsAodWorkingModeEnum.Through,
        _ => throw new ArgumentOutOfRangeException(nameof(mode), mode, null)
    };

    #endregion AodWorking

    #region MagType

    public static CgMagTypeEnum ToCgMagTypeEnum(this OpticsMagTypeEnum opticsMagTypeEnum) => opticsMagTypeEnum switch
    {
        OpticsMagTypeEnum.Low => CgMagTypeEnum.Low,
        OpticsMagTypeEnum.Middle => CgMagTypeEnum.Mid,
        OpticsMagTypeEnum.High => CgMagTypeEnum.High,
        _ => throw new ArgumentOutOfRangeException(nameof(opticsMagTypeEnum), opticsMagTypeEnum, null)
    };

    public static OpticsMagTypeEnum ToOpticsMagTypeEnum(this CgMagTypeEnum cgMagTypeEnum) => cgMagTypeEnum switch
    {
        CgMagTypeEnum.Low => OpticsMagTypeEnum.Low,
        CgMagTypeEnum.Mid => OpticsMagTypeEnum.Middle,
        CgMagTypeEnum.High => OpticsMagTypeEnum.High,
        _ => OpticsMagTypeEnum.Low // todo: 后续恢复异常处理
    };

    public static SxMAGEnum ToSxMagEnum(this OpticsMagTypeEnum opticsMagTypeEnum) => opticsMagTypeEnum switch
    {
        OpticsMagTypeEnum.Low => SxMAGEnum.Low,
        OpticsMagTypeEnum.Middle => SxMAGEnum.Mid,
        OpticsMagTypeEnum.High => SxMAGEnum.High,
        _ => throw new ArgumentOutOfRangeException(nameof(opticsMagTypeEnum), opticsMagTypeEnum, null)
    };

    public static OpticsMagTypeEnum ToOpticsMagTypeEnum(this SxMAGEnum sxMagEnum) => sxMagEnum switch
    {
        SxMAGEnum.Low => OpticsMagTypeEnum.Low,
        SxMAGEnum.Mid => OpticsMagTypeEnum.Middle,
        SxMAGEnum.High => OpticsMagTypeEnum.High,
        _ => throw new ArgumentOutOfRangeException(nameof(sxMagEnum), sxMagEnum, null)
    };

    public static ESxLevelEnum ToESxLevelEnum(this OpticsMagTypeEnum opticsMagTypeEnum) => opticsMagTypeEnum switch
    {
        OpticsMagTypeEnum.Low => ESxLevelEnum.Low,
        OpticsMagTypeEnum.Middle => ESxLevelEnum.Mid,
        OpticsMagTypeEnum.High => ESxLevelEnum.High,
        _ => throw new ArgumentOutOfRangeException(nameof(opticsMagTypeEnum), opticsMagTypeEnum, null)
    };

    public static OpticsMagTypeEnum ToOpticsMagTypeEnum(this ESxLevelEnum eSxLevelEnum) => eSxLevelEnum switch
    {
        ESxLevelEnum.Low => OpticsMagTypeEnum.Low,
        ESxLevelEnum.Mid => OpticsMagTypeEnum.Middle,
        ESxLevelEnum.High => OpticsMagTypeEnum.High,
        _ => throw new ArgumentOutOfRangeException(nameof(eSxLevelEnum), eSxLevelEnum, null)
    };

    #endregion MagType

    #region Polarization

    public static CgPolarizationTypeEnum ToCgPolarizationTypeEnum(this OpticsPolarizationTypeEnum opticsPolarizationTypeEnum) => opticsPolarizationTypeEnum switch
    {
        OpticsPolarizationTypeEnum.C => CgPolarizationTypeEnum.C,
        OpticsPolarizationTypeEnum.S => CgPolarizationTypeEnum.S,
        OpticsPolarizationTypeEnum.P => CgPolarizationTypeEnum.P,
        _ => throw new ArgumentOutOfRangeException(nameof(opticsPolarizationTypeEnum), opticsPolarizationTypeEnum, null)
    };

    public static OpticsPolarizationTypeEnum ToOpticsPolarizationTypeEnum(this CgPolarizationTypeEnum cgPolarizationTypeEnum) => cgPolarizationTypeEnum switch
    {
        CgPolarizationTypeEnum.C => OpticsPolarizationTypeEnum.C,
        CgPolarizationTypeEnum.S => OpticsPolarizationTypeEnum.S,
        CgPolarizationTypeEnum.P => OpticsPolarizationTypeEnum.P,
        _ => throw new ArgumentOutOfRangeException(nameof(cgPolarizationTypeEnum), cgPolarizationTypeEnum, null)
    };

    #endregion Polarization
}