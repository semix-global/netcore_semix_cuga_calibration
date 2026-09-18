using CommunityToolkit.Diagnostics;
using Core.Models.Enums.Optics;
using Cuga.Data.DataStruct.Optics;

#if NET
using Semix.GRPC.DTO;
using Semix.GRPC.DTO.Basic;
#else
using Cuga.Data.DataStruct.PMT;
using Semix.WcfTransfer.DTO;
using Semix.WcfTransfer.DTO.Basic;

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
        public SxMAGEnum ToSxMagEnum() => @this switch
        {
            CgMagTypeEnum.Low => SxMAGEnum.Low,
            CgMagTypeEnum.Mid => SxMAGEnum.Mid,
            CgMagTypeEnum.High => SxMAGEnum.High,
            _ => ThrowHelper.ThrowArgumentOutOfRangeException<SxMAGEnum>(nameof(@this))
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

    #region Collector Polarization

    extension(int @this)
    {
        public CgNDFCHEnum ToCgNDFChEnum() => @this switch
        {
            1 => CgNDFCHEnum.CH1_NDF,
            2 => CgNDFCHEnum.CH2_NDF,
            3 => CgNDFCHEnum.CH3_NDF,
            _ => ThrowHelper.ThrowArgumentOutOfRangeException<CgNDFCHEnum>(nameof(@this))
        };
    }

    extension(CgNDFTypeEnum @this)
    {
        public OpticsCollectorPolarizationModeEnum ToCollectorPolarizationModeEnum() => @this switch
        {
            CgNDFTypeEnum.N => OpticsCollectorPolarizationModeEnum.N,
            CgNDFTypeEnum.P => OpticsCollectorPolarizationModeEnum.P,
            CgNDFTypeEnum.S => OpticsCollectorPolarizationModeEnum.S,
#if NET48
            CgNDFTypeEnum.None => OpticsCollectorPolarizationModeEnum.N,
#endif
            _ => ThrowHelper.ThrowArgumentOutOfRangeException<OpticsCollectorPolarizationModeEnum>(nameof(@this))
        };
    }

    extension(OpticsCollectorPolarizationModeEnum @this)
    {
        public CgNDFTypeEnum ToCgNDFTypeEnum() => @this switch
        {
            OpticsCollectorPolarizationModeEnum.N => CgNDFTypeEnum.N,
            OpticsCollectorPolarizationModeEnum.P => CgNDFTypeEnum.P,
            OpticsCollectorPolarizationModeEnum.S => CgNDFTypeEnum.S,
#if NET48
            OpticsCollectorPolarizationModeEnum.None => CgNDFTypeEnum.N,
#endif
            _ => ThrowHelper.ThrowArgumentOutOfRangeException<CgNDFTypeEnum>(nameof(@this))
        };
    }

    #endregion Collector Polarization

    #region Y Ghost

    extension(CgClinderType @this)
    {
        public OpticsYGhostModeEnum ToYGhostModeEnum() => @this switch
        {
            CgClinderType.Magnet => OpticsYGhostModeEnum.Magnet,
            CgClinderType.OI_Zoos => OpticsYGhostModeEnum.OI_Zoos,
            CgClinderType.NI_Zoos => OpticsYGhostModeEnum.NI_Zoos,
            CgClinderType.CH2_Camera => OpticsYGhostModeEnum.CH2_Camera,
            CgClinderType.CH1_Camera => OpticsYGhostModeEnum.CH1_Camera,
            _ => ThrowHelper.ThrowArgumentOutOfRangeException<OpticsYGhostModeEnum>(nameof(@this))
        };
    }

    extension(OpticsYGhostModeEnum @this)
    {
        public CgClinderType ToCgClinderType() => @this switch
        {
            OpticsYGhostModeEnum.Magnet => CgClinderType.Magnet,
            OpticsYGhostModeEnum.OI_Zoos => CgClinderType.OI_Zoos,
            OpticsYGhostModeEnum.NI_Zoos => CgClinderType.NI_Zoos,
            OpticsYGhostModeEnum.CH2_Camera => CgClinderType.CH2_Camera,
            OpticsYGhostModeEnum.CH1_Camera => CgClinderType.CH1_Camera,
            _ => ThrowHelper.ThrowArgumentOutOfRangeException<CgClinderType>(nameof(@this))
        };
    }

    #endregion Y Ghost

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
            OpticsAODElectrodeEnum.Electrode5 => CgAwgElectrodeEnum.Electrode5,
            OpticsAODElectrodeEnum.Electrode6 => CgAwgElectrodeEnum.Electrode6,
            OpticsAODElectrodeEnum.Electrode7 => CgAwgElectrodeEnum.Electrode7,
            OpticsAODElectrodeEnum.Electrode8 => CgAwgElectrodeEnum.Electrode8,
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
            CgAwgElectrodeEnum.Electrode5 => OpticsAODElectrodeEnum.Electrode5,
            CgAwgElectrodeEnum.Electrode6 => OpticsAODElectrodeEnum.Electrode6,
            CgAwgElectrodeEnum.Electrode7 => OpticsAODElectrodeEnum.Electrode7,
            CgAwgElectrodeEnum.Electrode8 => OpticsAODElectrodeEnum.Electrode8,
            _ => ThrowHelper.ThrowArgumentOutOfRangeException<OpticsAODElectrodeEnum>(nameof(@this))
        };
    }

    #endregion OpticsAODElectrodeEnum

#endif
}