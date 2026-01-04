using CommunityToolkit.Diagnostics;
using Core.Models.Enums.Optics;
using Cuga.Data.DataStruct.Optics;

#if NET
using Semix.GRPC.DTO.Basic;
using Semix.GRPC.DTO;
#else
using Semix.WcfTransfer.DTO;
using Semix.WcfTransfer.DTO.Basic;
using Cuga.Data.DataStruct.PMT;

#endif

namespace Core.Models.Extensions;

public static class EnumOpticsExtension
{
    #region AodWorking

    extension(OpticsAODWorkingModeEnum @this)
    {
        public int ToOpticsAodWorkingMode() => @this switch
        {
            OpticsAODWorkingModeEnum.Close => 0,
            OpticsAODWorkingModeEnum.Scan => 1,
            OpticsAODWorkingModeEnum.Through => 2,
            _ => ThrowHelper.ThrowArgumentOutOfRangeException<int>(nameof(@this))
        };
    }

    #endregion AodWorking

    #region MagType

    extension(CgMagTypeEnum)
    {
        public static CgMagTypeEnum ErrorCgMagTypeEnum =>
#if NET
            CgMagTypeEnum.Null;
#else
            CgMagTypeEnum.None;
#endif
    }

    extension(CgMagTypeEnum @this)
    {
        public OpticsMagTypeEnum ToOpticsMagTypeEnum() => @this switch
        {
            CgMagTypeEnum.Low => OpticsMagTypeEnum.Low,
            CgMagTypeEnum.Mid => OpticsMagTypeEnum.Middle,
            CgMagTypeEnum.High => OpticsMagTypeEnum.High,
            _ => ThrowHelper.ThrowArgumentOutOfRangeException<OpticsMagTypeEnum>(nameof(@this))
        };

        public SxMAGEnum ToSxMagEnum() => @this switch
        {
            CgMagTypeEnum.Low => SxMAGEnum.Low,
            CgMagTypeEnum.Mid => SxMAGEnum.Mid,
            CgMagTypeEnum.High => SxMAGEnum.High,
            _ => ThrowHelper.ThrowArgumentOutOfRangeException<SxMAGEnum>(nameof(@this))
        };
    }

    extension(OpticsMagTypeEnum @this)
    {
        public CgMagTypeEnum ToCgMagTypeEnum() => @this switch
        {
            OpticsMagTypeEnum.Low => CgMagTypeEnum.Low,
            OpticsMagTypeEnum.Middle => CgMagTypeEnum.Mid,
            OpticsMagTypeEnum.High => CgMagTypeEnum.High,
            _ => ThrowHelper.ThrowArgumentOutOfRangeException<CgMagTypeEnum>(nameof(@this))
        };

        public SxMAGEnum ToSxMagEnum() => @this switch
        {
            OpticsMagTypeEnum.Low => SxMAGEnum.Low,
            OpticsMagTypeEnum.Middle => SxMAGEnum.Mid,
            OpticsMagTypeEnum.High => SxMAGEnum.High,
            _ => ThrowHelper.ThrowArgumentOutOfRangeException<SxMAGEnum>(nameof(@this))
        };

        public ESxLevelEnum ToESxLevelEnum() => @this switch
        {
            OpticsMagTypeEnum.Low => ESxLevelEnum.Low,
            OpticsMagTypeEnum.Middle => ESxLevelEnum.Mid,
            OpticsMagTypeEnum.High => ESxLevelEnum.High,
            _ => ThrowHelper.ThrowArgumentOutOfRangeException<ESxLevelEnum>(nameof(@this))
        };
    }

    extension(SxMAGEnum @this)
    {
        public CgMagTypeEnum ToCgMagTypeEnum() => @this switch
        {
            SxMAGEnum.Low => CgMagTypeEnum.Low,
            SxMAGEnum.Mid => CgMagTypeEnum.Mid,
            SxMAGEnum.High => CgMagTypeEnum.High,
            _ => ThrowHelper.ThrowArgumentOutOfRangeException<CgMagTypeEnum>(nameof(@this))
        };

        public OpticsMagTypeEnum ToOpticsMagTypeEnum() => @this switch
        {
            SxMAGEnum.Low => OpticsMagTypeEnum.Low,
            SxMAGEnum.Mid => OpticsMagTypeEnum.Middle,
            SxMAGEnum.High => OpticsMagTypeEnum.High,
            _ => ThrowHelper.ThrowArgumentOutOfRangeException<OpticsMagTypeEnum>(nameof(@this))
        };

        public ESxLevelEnum ToESxLevelEnum() => @this switch
        {
            SxMAGEnum.Low => ESxLevelEnum.Low,
            SxMAGEnum.Mid => ESxLevelEnum.Mid,
            SxMAGEnum.High => ESxLevelEnum.High,
            _ => ThrowHelper.ThrowArgumentOutOfRangeException<ESxLevelEnum>(nameof(@this))
        };
    }

    #endregion MagType

    #region Polarization

    extension(OpticsPolarizationModeEnum @this)
    {
        public CgPolarizationTypeEnum ToCgPolarizationTypeEnum() => @this switch
        {
            OpticsPolarizationModeEnum.P => CgPolarizationTypeEnum.P,
            OpticsPolarizationModeEnum.S => CgPolarizationTypeEnum.S,
            OpticsPolarizationModeEnum.C => CgPolarizationTypeEnum.C,
            _ => ThrowHelper.ThrowArgumentOutOfRangeException<CgPolarizationTypeEnum>(nameof(@this))
        };
    }

    extension(CgPolarizationTypeEnum @this)
    {
        public OpticsPolarizationModeEnum ToOpticsPolarizationModeEnum() => @this switch
        {
            CgPolarizationTypeEnum.P => OpticsPolarizationModeEnum.P,
            CgPolarizationTypeEnum.S => OpticsPolarizationModeEnum.S,
            CgPolarizationTypeEnum.C => OpticsPolarizationModeEnum.C,
            _ => ThrowHelper.ThrowArgumentOutOfRangeException<OpticsPolarizationModeEnum>(nameof(@this))
        };
    }

    #endregion Polarization

    #region OpticsIlluminationMode

    extension(CgNIOIType)
    {
        public static CgNIOIType ErrorCgNIOIType => (CgNIOIType)(int.MaxValue);
    }

    extension(CgNIOIType @this)
    {
        public OpticsIlluminationModeEnum ToOpticsIlluminationModeEnum() => @this switch
        {
            CgNIOIType.OI => OpticsIlluminationModeEnum.OI,
            CgNIOIType.NI => OpticsIlluminationModeEnum.NI,
            _ => ThrowHelper.ThrowArgumentOutOfRangeException<OpticsIlluminationModeEnum>(nameof(@this))
        };
    }

    extension(OpticsIlluminationModeEnum @this)
    {
        public CgNIOIType ToCgNIOITypeEnum() => @this switch
        {
            OpticsIlluminationModeEnum.OI => CgNIOIType.OI,
            OpticsIlluminationModeEnum.NI => CgNIOIType.NI,
            _ => ThrowHelper.ThrowArgumentOutOfRangeException<CgNIOIType>(nameof(@this))
        };

        public SxNIOIEnum ToSxNIOIEnum() => @this switch
        {
            OpticsIlluminationModeEnum.OI => SxNIOIEnum.OI,
            OpticsIlluminationModeEnum.NI => SxNIOIEnum.NI,
            _ => ThrowHelper.ThrowArgumentOutOfRangeException<SxNIOIEnum>(nameof(@this))
        };
    }

    extension(SxNIOIEnum @this)
    {
        public OpticsIlluminationModeEnum ToOpticsIlluminationModeEnum() => @this switch
        {
            SxNIOIEnum.OI => OpticsIlluminationModeEnum.OI,
            SxNIOIEnum.NI => OpticsIlluminationModeEnum.NI,
            _ => ThrowHelper.ThrowArgumentOutOfRangeException<OpticsIlluminationModeEnum>(nameof(@this))
        };

        public CgNIOIType ToCgNIOIType() => @this switch
        {
            SxNIOIEnum.OI => CgNIOIType.OI,
            SxNIOIEnum.NI => CgNIOIType.NI,
            _ => ThrowHelper.ThrowArgumentOutOfRangeException<CgNIOIType>(nameof(@this))
        };
    }

    #endregion

#if NETFRAMEWORK

    #region OpticsAODTypeEnum

    extension(OpticsAODTypeEnum @this)
    {
        public CgWaveType ToCgWaveType() => @this switch
        {
            OpticsAODTypeEnum.Prescan => CgWaveType.Prescan,
            OpticsAODTypeEnum.Chirp => CgWaveType.Chirp,
            _ => ThrowHelper.ThrowArgumentOutOfRangeException<CgWaveType>(nameof(@this))
        };
    }

    extension(CgWaveType @this)
    {
        public OpticsAODTypeEnum ToOpticsAodTypeEnum() => @this switch
        {
            CgWaveType.Prescan => OpticsAODTypeEnum.Prescan,
            CgWaveType.Chirp => OpticsAODTypeEnum.Chirp,
            _ => ThrowHelper.ThrowArgumentOutOfRangeException<OpticsAODTypeEnum>(nameof(@this))
        };
    }

    #endregion OpticsAODTypeEnum

    #region OpticsAODElectrodeEnum

    extension(OpticsAODElectrodeEnum @this)
    {
        public CgAwgElectrodeEnum ToCgAwgElectrodeEnum() => @this switch
        {
            OpticsAODElectrodeEnum.Electrode1 => CgAwgElectrodeEnum.Electrode1,
            OpticsAODElectrodeEnum.Electrode2 => CgAwgElectrodeEnum.Electrode2,
            OpticsAODElectrodeEnum.Electrode3 => CgAwgElectrodeEnum.Electrode3,
            OpticsAODElectrodeEnum.Electrode4 => CgAwgElectrodeEnum.Electrode4,
            _ => ThrowHelper.ThrowArgumentOutOfRangeException<CgAwgElectrodeEnum>(nameof(@this))
        };
    }

    extension(CgAwgElectrodeEnum @this)
    {
        public OpticsAODElectrodeEnum ToOpticsAODElectrodeEnum() => @this switch
        {
            CgAwgElectrodeEnum.Electrode1 => OpticsAODElectrodeEnum.Electrode1,
            CgAwgElectrodeEnum.Electrode2 => OpticsAODElectrodeEnum.Electrode2,
            CgAwgElectrodeEnum.Electrode3 => OpticsAODElectrodeEnum.Electrode3,
            CgAwgElectrodeEnum.Electrode4 => OpticsAODElectrodeEnum.Electrode4,
            _ => ThrowHelper.ThrowArgumentOutOfRangeException<OpticsAODElectrodeEnum>(nameof(@this))
        };
    }

    #endregion OpticsAODElectrodeEnum

#endif
}