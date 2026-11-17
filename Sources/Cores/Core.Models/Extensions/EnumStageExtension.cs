using CommunityToolkit.Diagnostics;
using Core.Models.Enums.ADS;
using Core.Models.Enums.Stage;
using Cuga.Data.DataStruct.ADS;
using Cuga.Data.DataStruct.DTO.Swath;
using Cuga.Data.DataStruct.Stage;
using Semix.WcfTransfer.DTO;
using Semix.WcfTransfer.DTO.Basic;

namespace Core.Models.Extensions;

public static class EnumStageExtension
{
    #region CalChipMode

    extension(CalChipSiteModelEnum @this)
    {
        public CgCalChipType ToCgCalChipType() => @this switch
        {
            CalChipSiteModelEnum.ChuckModel => CgCalChipType.None,
            CalChipSiteModelEnum.UndefinedModel => CgCalChipType.Shape,
            CalChipSiteModelEnum.DswModel => CgCalChipType.DSW,
            CalChipSiteModelEnum.ShinyWaferModel => CgCalChipType.Shiny,
            CalChipSiteModelEnum.HazeModel => CgCalChipType.Haze,
            _ => ThrowHelper.ThrowArgumentOutOfRangeException<CgCalChipType>(nameof(@this))
        };
    }

    extension(int @this)
    {
        public CalChipSiteModelEnum ToCalChipModelEnum() => @this switch
        {
            0 => CalChipSiteModelEnum.ChuckModel,
            1 => CalChipSiteModelEnum.UndefinedModel,
            2 => CalChipSiteModelEnum.DswModel,
            3 => CalChipSiteModelEnum.ShinyWaferModel,
            4 => CalChipSiteModelEnum.HazeModel,
            _ => ThrowHelper.ThrowArgumentOutOfRangeException<CalChipSiteModelEnum>(nameof(@this))
        };
    }

    #endregion CalChipMode

    #region DirectionType

    extension(StageDirectionTypeEnum @this)
    {
        public ESxMotionDirection ToESxMotionDirection() => @this switch
        {
            StageDirectionTypeEnum.UpLeft => ESxMotionDirection.LEFT_UP,
            StageDirectionTypeEnum.Up => ESxMotionDirection.Up,
            StageDirectionTypeEnum.UpRight => ESxMotionDirection.RIGHT_UP,
            StageDirectionTypeEnum.Left => ESxMotionDirection.LEFT,
            StageDirectionTypeEnum.Right => ESxMotionDirection.RIGHT,
            StageDirectionTypeEnum.DownLeft => ESxMotionDirection.LEFT_Down,
            StageDirectionTypeEnum.Down => ESxMotionDirection.Down,
            StageDirectionTypeEnum.DownRight => ESxMotionDirection.RIGHT_DOWN,
            _ => ThrowHelper.ThrowArgumentOutOfRangeException<ESxMotionDirection>(nameof(@this))
        };
    }

    extension(ESxMotionDirection @this)
    {
        public StageDirectionTypeEnum ToStageDirectionTypeEnum() => @this switch
        {
            ESxMotionDirection.LEFT_UP => StageDirectionTypeEnum.UpLeft,
            ESxMotionDirection.Up => StageDirectionTypeEnum.Up,
            ESxMotionDirection.RIGHT_UP => StageDirectionTypeEnum.UpRight,
            ESxMotionDirection.LEFT => StageDirectionTypeEnum.Left,
            ESxMotionDirection.RIGHT => StageDirectionTypeEnum.Right,
            ESxMotionDirection.LEFT_Down => StageDirectionTypeEnum.DownLeft,
            ESxMotionDirection.Down => StageDirectionTypeEnum.Down,
            ESxMotionDirection.RIGHT_DOWN => StageDirectionTypeEnum.DownRight,
            _ => ThrowHelper.ThrowArgumentOutOfRangeException<StageDirectionTypeEnum>(nameof(@this))
        };
    }

    #endregion DirectionType

    #region Speed

    extension(CgSpeedLevelType)
    {
        public static CgSpeedLevelType ErrorCgSpeedLevelType => CgSpeedLevelType.None;
    }

    extension(SxSpeedEnum @this)
    {
        public CgSpeedLevelType ToCgSpeedLevelType() => @this switch
        {
            SxSpeedEnum.Low => CgSpeedLevelType.Low,
            SxSpeedEnum.Mid => CgSpeedLevelType.Mid,
            SxSpeedEnum.High => CgSpeedLevelType.High,
            _ => ThrowHelper.ThrowArgumentOutOfRangeException<CgSpeedLevelType>(nameof(@this))
        };

        public ESxLevelEnum ToESxLevelEnum() => @this switch
        {
            SxSpeedEnum.Low => ESxLevelEnum.Low,
            SxSpeedEnum.Mid => ESxLevelEnum.Mid,
            SxSpeedEnum.High => ESxLevelEnum.High,
            _ => ThrowHelper.ThrowArgumentOutOfRangeException<ESxLevelEnum>(nameof(@this))
        };
    }

    #endregion Speed

    #region Coordinate

    extension(StageCoordinateSystemEnum @this)
    {
        public SxCollectImgCoordinateSystem ToSxCollectImgCoordinateSystemEnum() => @this switch
        {
            StageCoordinateSystemEnum.Bright or StageCoordinateSystemEnum.Dark => SxCollectImgCoordinateSystem.DF,
            StageCoordinateSystemEnum.Machine => SxCollectImgCoordinateSystem.Stage,
            _ => ThrowHelper.ThrowArgumentOutOfRangeException<SxCollectImgCoordinateSystem>(nameof(@this))
        };
    }

    #endregion

    #region Register

    public static CgADSTraceBufferReg ToCgADSTraceBufferRegEnum(this AdsTracebufferRegEnum adsTracebufferRegEnum) => adsTracebufferRegEnum switch
    {
        AdsTracebufferRegEnum.Z_ECS0 => CgADSTraceBufferReg.Z_ECS0,
        AdsTracebufferRegEnum.Z_ECS1 => CgADSTraceBufferReg.Z_ECS1,
        AdsTracebufferRegEnum.Z_ECS2 => CgADSTraceBufferReg.Z_ECS2,
        AdsTracebufferRegEnum.XY_X0 => CgADSTraceBufferReg.XY_X0,
        AdsTracebufferRegEnum.XY_X1 => CgADSTraceBufferReg.XY_X1,
        AdsTracebufferRegEnum.XY_Y0 => CgADSTraceBufferReg.XY_Y0,
        AdsTracebufferRegEnum.XY_Y1 => CgADSTraceBufferReg.XY_Y1,
        AdsTracebufferRegEnum.Height => CgADSTraceBufferReg.Height,
        AdsTracebufferRegEnum.Roll => CgADSTraceBufferReg.ROLL,
        AdsTracebufferRegEnum.Pitch => CgADSTraceBufferReg.PITCH,
        AdsTracebufferRegEnum.ACS_X_Speed => CgADSTraceBufferReg.ACS_X_Speed,
        AdsTracebufferRegEnum.ACS_Y_Speed => CgADSTraceBufferReg.ACS_Y_Speed,
        AdsTracebufferRegEnum.PropOutput0 => CgADSTraceBufferReg.PropOutput0,
        AdsTracebufferRegEnum.PropOutput1 => CgADSTraceBufferReg.PropOutput1,
        AdsTracebufferRegEnum.PropOutput2 => CgADSTraceBufferReg.PropOutput2,

        _ => throw new ArgumentOutOfRangeException(nameof(adsTracebufferRegEnum), adsTracebufferRegEnum, null)
    };

    #endregion Register
}