using Core.Models.Enums.EFEM;

#if NET
using Semix.GRPC.DTO;
#else
using Semix.WcfTransfer.DTO;

#endif

namespace Core.Models.Extensions;

// ReSharper disable once InconsistentNaming
public static class EnumEFEMExtension
{
    #region Angle

    public static int ToEfemAngleEnum(this EFEMAngleEnum efemAngleEnum) => efemAngleEnum switch
    {
        EFEMAngleEnum.Down => 0,
        EFEMAngleEnum.Right => 90,
        EFEMAngleEnum.Up => 180,
        EFEMAngleEnum.Left => 270,
        _ => throw new ArgumentOutOfRangeException(nameof(efemAngleEnum), efemAngleEnum, null)
    };

    public static EFEMAngleEnum ToEfemAngleEnum(this int efemAngle) => efemAngle switch
    {
        0 => EFEMAngleEnum.Down,
        90 => EFEMAngleEnum.Right,
        180 => EFEMAngleEnum.Up,
        270 => EFEMAngleEnum.Left,
        _ => throw new ArgumentOutOfRangeException(nameof(efemAngle), efemAngle, null)
    };

    #endregion Angle

    #region Station

    public static ESxStation ToESxStation(this EFEMStationEnum efemStationEnum) => efemStationEnum switch
    {
        EFEMStationEnum.P1 => ESxStation.P1,
        EFEMStationEnum.P2 => ESxStation.P2,
        _ => throw new ArgumentOutOfRangeException(nameof(efemStationEnum), efemStationEnum, null)
    };

    public static EFEMStationEnum ToEfemStationEnum(this ESxStation sxStation) => sxStation switch
    {
        ESxStation.P1 => EFEMStationEnum.P1,
        ESxStation.P2 => EFEMStationEnum.P2,
        _ => throw new ArgumentOutOfRangeException(nameof(sxStation), sxStation, null)
    };

    #endregion Station
}