using Core.Models.Enums.ADS;
using Core.Models.Enums.Stage;
using Cuga.Data.DataStruct.ADS;
using Cuga.Data.DataStruct.DTO.Swath;

#if NET
using Cuga.Data.DataStruct.Basic;
using Semix.GRPC.DTO;
using Semix.GRPC.DTO.Basic;
#else
using Semix.WcfTransfer.DTO;
using Semix.WcfTransfer.DTO.Basic;
using Cuga.Data.DataStruct.Stage;
using CommunityToolkit.Diagnostics;

#endif

namespace Core.Models.Extensions;

public static class EnumStageExtension
{
    #region CalChipMode

#if NET
    public static CgCalChipEnum ToCgCalChipEnum(this CalChipSiteModelEnum calChipSiteModelEnum) => calChipSiteModelEnum switch
    {
        CalChipSiteModelEnum.ChuckModel => CgCalChipEnum.None,
        CalChipSiteModelEnum.UndefinedModel => CgCalChipEnum.SHAPE,
        CalChipSiteModelEnum.DswModel => CgCalChipEnum.DSW,
        CalChipSiteModelEnum.ShinyWaferModel => CgCalChipEnum.SHINY,
        CalChipSiteModelEnum.HazeModel => CgCalChipEnum.HAZE,
        _ => throw new ArgumentOutOfRangeException(nameof(calChipSiteModelEnum), calChipSiteModelEnum, null)
    };

    public static CalChipSiteModelEnum ToCalChipSiteModelEnum(this CgCalChipEnum cgCalChipEnum) => cgCalChipEnum switch
    {
        CgCalChipEnum.None => CalChipSiteModelEnum.ChuckModel,
        CgCalChipEnum.SHAPE => CalChipSiteModelEnum.UndefinedModel,
        CgCalChipEnum.DSW => CalChipSiteModelEnum.DswModel,
        CgCalChipEnum.SHINY => CalChipSiteModelEnum.ShinyWaferModel,
        CgCalChipEnum.HAZE => CalChipSiteModelEnum.HazeModel,
        _ => throw new ArgumentOutOfRangeException(nameof(cgCalChipEnum), cgCalChipEnum, null)
    };

#else
    public static CgCalChipType ToCgCalChipType(this CalChipSiteModelEnum calChipSiteModelEnum) => calChipSiteModelEnum switch
    {
        CalChipSiteModelEnum.ChuckModel => CgCalChipType.None,
        CalChipSiteModelEnum.UndefinedModel => CgCalChipType.Shape,
        CalChipSiteModelEnum.DswModel => CgCalChipType.DSW,
        CalChipSiteModelEnum.ShinyWaferModel => CgCalChipType.Shiny,
        CalChipSiteModelEnum.HazeModel => CgCalChipType.Haze,
        _ => ThrowHelper.ThrowArgumentOutOfRangeException<CgCalChipType>(nameof(calChipSiteModelEnum))
    };

    public static CalChipSiteModelEnum ToCalChipModelEnum(this CgCalChipType cgCalChipType) => cgCalChipType switch
    {
        CgCalChipType.None => CalChipSiteModelEnum.ChuckModel,
        CgCalChipType.Shape => CalChipSiteModelEnum.UndefinedModel,
        CgCalChipType.DSW => CalChipSiteModelEnum.DswModel,
        CgCalChipType.Shiny => CalChipSiteModelEnum.ShinyWaferModel,
        CgCalChipType.Haze => CalChipSiteModelEnum.HazeModel,
        _ => ThrowHelper.ThrowArgumentOutOfRangeException<CalChipSiteModelEnum>(nameof(cgCalChipType))
    };

    public static CalChipSiteModelEnum ToCalChipModelEnum(this int cgCalChipModel) => cgCalChipModel switch
    {
        0 => CalChipSiteModelEnum.ChuckModel,
        1 => CalChipSiteModelEnum.UndefinedModel,
        2 => CalChipSiteModelEnum.DswModel,
        3 => CalChipSiteModelEnum.ShinyWaferModel,
        4 => CalChipSiteModelEnum.HazeModel,
        _ => throw new ArgumentOutOfRangeException(nameof(cgCalChipModel), cgCalChipModel, null)
    };

#endif

    #endregion CalChipMode

    #region DirectionType

    public static ESxMotionDirection ToESxMotionDirection(this StageDirectionTypeEnum stageDirectionTypeEnum) => stageDirectionTypeEnum switch
    {
        StageDirectionTypeEnum.UpLeft => ESxMotionDirection.LEFT_UP,
        StageDirectionTypeEnum.Up => ESxMotionDirection.Up,
        StageDirectionTypeEnum.UpRight => ESxMotionDirection.RIGHT_UP,
        StageDirectionTypeEnum.Left => ESxMotionDirection.LEFT,
        StageDirectionTypeEnum.Right => ESxMotionDirection.RIGHT,
        StageDirectionTypeEnum.DownLeft => ESxMotionDirection.LEFT_Down,
        StageDirectionTypeEnum.Down => ESxMotionDirection.Down,
        StageDirectionTypeEnum.DownRight => ESxMotionDirection.RIGHT_DOWN,
        _ => throw new ArgumentOutOfRangeException(nameof(stageDirectionTypeEnum), stageDirectionTypeEnum, null)
    };

    public static StageDirectionTypeEnum ToStageDirectionTypeEnum(this ESxMotionDirection eSxMotionDirection) => eSxMotionDirection switch
    {
        ESxMotionDirection.LEFT_UP => StageDirectionTypeEnum.UpLeft,
        ESxMotionDirection.Up => StageDirectionTypeEnum.Up,
        ESxMotionDirection.RIGHT_UP => StageDirectionTypeEnum.UpRight,
        ESxMotionDirection.LEFT => StageDirectionTypeEnum.Left,
        ESxMotionDirection.RIGHT => StageDirectionTypeEnum.Right,
        ESxMotionDirection.LEFT_Down => StageDirectionTypeEnum.DownLeft,
        ESxMotionDirection.Down => StageDirectionTypeEnum.Down,
        ESxMotionDirection.RIGHT_DOWN => StageDirectionTypeEnum.DownRight,
        _ => throw new ArgumentOutOfRangeException(nameof(eSxMotionDirection), eSxMotionDirection, null)
    };

#if NET
    public static CgMotionDir ToCgMotionDir(this StageDirectionTypeEnum stageDirectionTypeEnum) => stageDirectionTypeEnum switch
    {
        StageDirectionTypeEnum.UpLeft => CgMotionDir.LEFT_UP,
        StageDirectionTypeEnum.Up => CgMotionDir.Up,
        StageDirectionTypeEnum.UpRight => CgMotionDir.RIGHT_Up,
        StageDirectionTypeEnum.Left => CgMotionDir.LEFT,
        StageDirectionTypeEnum.Right => CgMotionDir.RIGHT,
        StageDirectionTypeEnum.DownLeft => CgMotionDir.LEFT_Down,
        StageDirectionTypeEnum.Down => CgMotionDir.Down,
        StageDirectionTypeEnum.DownRight => CgMotionDir.RIGHT_Down,
        _ => throw new ArgumentOutOfRangeException(nameof(stageDirectionTypeEnum), stageDirectionTypeEnum, null)
    };

    public static StageDirectionTypeEnum ToStageDirectionTypeEnum(this CgMotionDir cgMotionDir) => cgMotionDir switch
    {
        CgMotionDir.LEFT_UP => StageDirectionTypeEnum.UpLeft,
        CgMotionDir.Up => StageDirectionTypeEnum.Up,
        CgMotionDir.RIGHT_Up => StageDirectionTypeEnum.UpRight,
        CgMotionDir.LEFT => StageDirectionTypeEnum.Left,
        CgMotionDir.RIGHT => StageDirectionTypeEnum.Right,
        CgMotionDir.LEFT_Down => StageDirectionTypeEnum.DownLeft,
        CgMotionDir.Down => StageDirectionTypeEnum.Down,
        CgMotionDir.RIGHT_Down => StageDirectionTypeEnum.DownRight,
        _ => throw new ArgumentOutOfRangeException(nameof(cgMotionDir), cgMotionDir, null)
    };

#endif

    #endregion DirectionType

    #region Speed

    public static CgSpeedLevelType ToCgSpeedLevelType(this SxSpeedEnum sxSpeedEnum) => sxSpeedEnum switch
    {
        SxSpeedEnum.Low => CgSpeedLevelType.Low,
        SxSpeedEnum.Mid => CgSpeedLevelType.Mid,
        SxSpeedEnum.High => CgSpeedLevelType.High,
        _ => throw new ArgumentOutOfRangeException(nameof(sxSpeedEnum), sxSpeedEnum, null)
    };

    public static CgSpeedLevelType ToCgSpeedLevelType(this StageSpeedEnum stageSpeedEnum) => stageSpeedEnum switch
    {
        StageSpeedEnum.Low => CgSpeedLevelType.Low,
        StageSpeedEnum.Middle => CgSpeedLevelType.Mid,
        StageSpeedEnum.High => CgSpeedLevelType.High,
        _ => throw new ArgumentOutOfRangeException(nameof(stageSpeedEnum), stageSpeedEnum, null)
    };

    public static StageSpeedEnum ToStageSpeedEnum(this CgSpeedLevelType cgSpeedLevelType) => cgSpeedLevelType switch
    {
        CgSpeedLevelType.Low => StageSpeedEnum.Low,
        CgSpeedLevelType.Mid => StageSpeedEnum.Middle,
        CgSpeedLevelType.High => StageSpeedEnum.High,
        _ => throw new ArgumentOutOfRangeException(nameof(cgSpeedLevelType), cgSpeedLevelType, null)
    };

    public static SxSpeedEnum ToSxSpeedEnum(this StageSpeedEnum stageSpeedEnum) => stageSpeedEnum switch
    {
        StageSpeedEnum.Low => SxSpeedEnum.Low,
        StageSpeedEnum.Middle => SxSpeedEnum.Mid,
        StageSpeedEnum.High => SxSpeedEnum.High,
        _ => throw new ArgumentOutOfRangeException(nameof(stageSpeedEnum), stageSpeedEnum, null)
    };

    public static StageSpeedEnum ToStageSpeedEnum(this SxSpeedEnum sxSpeedEnum) => sxSpeedEnum switch
    {
        SxSpeedEnum.Low => StageSpeedEnum.Low,
        SxSpeedEnum.Mid => StageSpeedEnum.Middle,
        SxSpeedEnum.High => StageSpeedEnum.High,
        _ => throw new ArgumentOutOfRangeException(nameof(sxSpeedEnum), sxSpeedEnum, null)
    };

    public static ESxLevelEnum ToESxLevelEnum(this StageSpeedEnum stageSpeedEnum) => stageSpeedEnum switch
    {
        StageSpeedEnum.Low => ESxLevelEnum.Low,
        StageSpeedEnum.Middle => ESxLevelEnum.Mid,
        StageSpeedEnum.High => ESxLevelEnum.High,
        _ => throw new ArgumentOutOfRangeException(nameof(stageSpeedEnum), stageSpeedEnum, null)
    };

    public static StageSpeedEnum ToStageSpeedEnum(this ESxLevelEnum eSxLevelEnum) => eSxLevelEnum switch
    {
        ESxLevelEnum.Low => StageSpeedEnum.Low,
        ESxLevelEnum.Mid => StageSpeedEnum.Middle,
        ESxLevelEnum.High => StageSpeedEnum.High,
        _ => throw new ArgumentOutOfRangeException(nameof(eSxLevelEnum), eSxLevelEnum, null)
    };

    #endregion Speed

    #region Register

    public static CalibrationRegEnum ToCalibrationRegEnum(this AdsTracebufferRegEnum adsTracebufferRegEnum) => adsTracebufferRegEnum switch
    {
        AdsTracebufferRegEnum.Z_ECS0 => CalibrationRegEnum.Z_ECS0,
        AdsTracebufferRegEnum.Z_ECS1 => CalibrationRegEnum.Z_ECS1,
        AdsTracebufferRegEnum.Z_ECS2 => CalibrationRegEnum.Z_ECS2,
        AdsTracebufferRegEnum.XY_X0 => CalibrationRegEnum.XY_X0,
        AdsTracebufferRegEnum.XY_X1 => CalibrationRegEnum.XY_X1,
        AdsTracebufferRegEnum.XY_Y0 => CalibrationRegEnum.XY_Y0,
        AdsTracebufferRegEnum.XY_Y1 => CalibrationRegEnum.XY_Y1,
        AdsTracebufferRegEnum.Height => CalibrationRegEnum.Height,
        AdsTracebufferRegEnum.Roll => CalibrationRegEnum.ROLL,
        AdsTracebufferRegEnum.Pitch => CalibrationRegEnum.PITCH,
        AdsTracebufferRegEnum.ACS_X_Speed => CalibrationRegEnum.ACS_X_Speed,
        AdsTracebufferRegEnum.ACS_Y_Speed => CalibrationRegEnum.ACS_Y_Speed,
        AdsTracebufferRegEnum.PropOutput0 => CalibrationRegEnum.PropOutput0,
        AdsTracebufferRegEnum.PropOutput1 => CalibrationRegEnum.PropOutput1,
        AdsTracebufferRegEnum.PropOutput2 => CalibrationRegEnum.PropOutput2,

        _ => throw new ArgumentOutOfRangeException(nameof(adsTracebufferRegEnum), adsTracebufferRegEnum, null)
    };

    #endregion Register
}