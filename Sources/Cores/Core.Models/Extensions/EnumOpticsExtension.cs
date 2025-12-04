using CommunityToolkit.Diagnostics;
using Core.Models.Enums.Optics;
using Cuga.Data.DataStruct.Optics;

#if NET
using Semix.GRPC.DTO.Basic;
using Semix.GRPC.DTO;
using SxNIOIEnum = Cuga.Data.DataStruct.Optics.CgNIOIType;
#else
using Semix.WcfTransfer.DTO;
using Semix.WcfTransfer.DTO.Basic;
using Cuga.Data.DataStruct.PMT;

#endif

namespace Core.Models.Extensions;

public static class EnumOpticsExtension
{
    #region AodWorking

    public static int ToOpticsAodWorkingMode(this OpticsAODWorkingModeEnum mode) => mode switch
    {
        OpticsAODWorkingModeEnum.Close => 0,
        OpticsAODWorkingModeEnum.Scan => 1,
        OpticsAODWorkingModeEnum.Through => 2,
        _ => throw new ArgumentOutOfRangeException(nameof(mode), mode, null)
    };

    public static OpticsAODWorkingModeEnum ToOpticsAodWorkingModeEnum(this int mode) => mode switch
    {
        0 => OpticsAODWorkingModeEnum.Close,
        1 => OpticsAODWorkingModeEnum.Scan,
        2 => OpticsAODWorkingModeEnum.Through,
        _ => throw new ArgumentOutOfRangeException(nameof(mode), mode, null)
    };

    #endregion AodWorking

    #region MagType

    public static CgMagTypeEnum ToCgMagTypeEnum(this SxMAGEnum sxMagEnum) => sxMagEnum switch
    {
        SxMAGEnum.Low => CgMagTypeEnum.Low,
        SxMAGEnum.Mid => CgMagTypeEnum.Mid,
        SxMAGEnum.High => CgMagTypeEnum.High,
        (SxMAGEnum)(-1) => (CgMagTypeEnum)(-1),
        _ => ThrowHelper.ThrowArgumentOutOfRangeException<CgMagTypeEnum>(nameof(sxMagEnum))
    };

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

    public static SxMAGEnum ToSxMagEnum(this CgMagTypeEnum cgMagTypeEnum) => cgMagTypeEnum switch
    {
        CgMagTypeEnum.Low => SxMAGEnum.Low,
        CgMagTypeEnum.Mid => SxMAGEnum.Mid,
        CgMagTypeEnum.High => SxMAGEnum.High,
        _ => throw new ArgumentOutOfRangeException(nameof(cgMagTypeEnum), cgMagTypeEnum, null)
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

    public static CgPolarizationTypeEnum ToCgPolarizationTypeEnum(this OpticsPolarizationModeEnum opticsPolarizationModeEnum) => opticsPolarizationModeEnum switch
    {
        OpticsPolarizationModeEnum.C => CgPolarizationTypeEnum.C,
        OpticsPolarizationModeEnum.S => CgPolarizationTypeEnum.S,
        OpticsPolarizationModeEnum.P => CgPolarizationTypeEnum.P,
        _ => throw new ArgumentOutOfRangeException(nameof(opticsPolarizationModeEnum), opticsPolarizationModeEnum, null)
    };

    public static OpticsPolarizationModeEnum ToOpticsPolarizationModeEnum(this CgPolarizationTypeEnum cgPolarizationTypeEnum) => cgPolarizationTypeEnum switch
    {
        CgPolarizationTypeEnum.C => OpticsPolarizationModeEnum.C,
        CgPolarizationTypeEnum.S => OpticsPolarizationModeEnum.S,
        CgPolarizationTypeEnum.P => OpticsPolarizationModeEnum.P,
        _ => throw new ArgumentOutOfRangeException(nameof(cgPolarizationTypeEnum), cgPolarizationTypeEnum, null)
    };

    #endregion Polarization

    #region OpticsIlluminationMode

    public static OpticsIlluminationModeEnum ToOpticsIlluminationModeEnum(this CgNIOIType cgNIOIType) => cgNIOIType switch
    {
        CgNIOIType.OI => OpticsIlluminationModeEnum.OI,
        CgNIOIType.NI => OpticsIlluminationModeEnum.NI,
        _ => throw new ArgumentOutOfRangeException(nameof(cgNIOIType), cgNIOIType, null)
    };

    public static CgNIOIType ToCgNIOITypeEnum(this OpticsIlluminationModeEnum opticsIlluminationModeEnum) => opticsIlluminationModeEnum switch
    {
        OpticsIlluminationModeEnum.OI => CgNIOIType.OI,
        OpticsIlluminationModeEnum.NI => CgNIOIType.NI,
        _ => throw new ArgumentOutOfRangeException(nameof(opticsIlluminationModeEnum), opticsIlluminationModeEnum, null)
    };

    public static SxNIOIEnum ToSxNIOIEnum(this OpticsIlluminationModeEnum opticsIlluminationModeEnum) => opticsIlluminationModeEnum switch
    {
        OpticsIlluminationModeEnum.OI => SxNIOIEnum.OI,
        OpticsIlluminationModeEnum.NI => SxNIOIEnum.NI,
        _ => throw new ArgumentOutOfRangeException(nameof(opticsIlluminationModeEnum), opticsIlluminationModeEnum, null)
    };

    #endregion

#if NETFRAMEWORK

    #region OpticsAODTypeEnum

    public static CgWaveType ToCgWaveType(this OpticsAODTypeEnum @this) => @this switch
    {
        OpticsAODTypeEnum.Prescan => CgWaveType.Prescan,
        OpticsAODTypeEnum.Chirp => CgWaveType.Chirp,
        _ => ThrowHelper.ThrowArgumentOutOfRangeException<CgWaveType>(nameof(@this))
    };

    public static OpticsAODTypeEnum ToOpticsAodTypeEnum(this CgWaveType @this) => @this switch
    {
        CgWaveType.Prescan => OpticsAODTypeEnum.Prescan,
        CgWaveType.Chirp => OpticsAODTypeEnum.Chirp,
        _ => ThrowHelper.ThrowArgumentOutOfRangeException<OpticsAODTypeEnum>(nameof(@this))
    };

    #endregion OpticsAODTypeEnum

    #region OpticsAODElectrodeEnum

    public static CgAwgElectrodeEnum ToCgAwgElectrodeEnum(this OpticsAODElectrodeEnum @this) => @this switch
    {
        OpticsAODElectrodeEnum.Electrode1 => CgAwgElectrodeEnum.Electrode1,
        OpticsAODElectrodeEnum.Electrode2 => CgAwgElectrodeEnum.Electrode2,
        OpticsAODElectrodeEnum.Electrode3 => CgAwgElectrodeEnum.Electrode3,
        OpticsAODElectrodeEnum.Electrode4 => CgAwgElectrodeEnum.Electrode4,
        _ => ThrowHelper.ThrowArgumentOutOfRangeException<CgAwgElectrodeEnum>(nameof(@this))
    };

    public static OpticsAODElectrodeEnum ToOpticsAODElectrodeEnum(this CgAwgElectrodeEnum @this) => @this switch
    {
        CgAwgElectrodeEnum.Electrode1 => OpticsAODElectrodeEnum.Electrode1,
        CgAwgElectrodeEnum.Electrode2 => OpticsAODElectrodeEnum.Electrode2,
        CgAwgElectrodeEnum.Electrode3 => OpticsAODElectrodeEnum.Electrode3,
        CgAwgElectrodeEnum.Electrode4 => OpticsAODElectrodeEnum.Electrode4,
        _ => ThrowHelper.ThrowArgumentOutOfRangeException<OpticsAODElectrodeEnum>(nameof(@this))
    };

    #endregion OpticsAODElectrodeEnum

#endif
}