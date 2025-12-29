using Core.Models.Enums.Stage;
using Cuga.Data.DataStruct.DTO.Swath;
using CommunityToolkit.Diagnostics;

#if NET
using Cuga.Data.DataStruct.Basic;
using Semix.GRPC.DTO.Basic;
using Semix.GRPC.DTO;
#else
using Semix.WcfTransfer.DTO;
using Semix.WcfTransfer.DTO.Basic;
using Cuga.Data.DataStruct.Stage;

#endif

namespace Core.Models.Extensions;

public static class EnumStageExtension
{
    #region CalChipMode

    extension(CalChipSiteModelEnum @this)
    {
#if NET
        public CgCalChipEnum ToCgCalChipEnum() => @this switch
        {
            CalChipSiteModelEnum.ChuckModel => CgCalChipEnum.None,
            CalChipSiteModelEnum.UndefinedModel => CgCalChipEnum.SHAPE,
            CalChipSiteModelEnum.DswModel => CgCalChipEnum.DSW,
            CalChipSiteModelEnum.ShinyWaferModel => CgCalChipEnum.SHINY,
            CalChipSiteModelEnum.HazeModel => CgCalChipEnum.HAZE,
           _ => ThrowHelper.ThrowArgumentOutOfRangeException<CgCalChipEnum>(nameof(@this))
        };
#else
        public CgCalChipType ToCgCalChipType() => @this switch
        {
            CalChipSiteModelEnum.ChuckModel => CgCalChipType.None,
            CalChipSiteModelEnum.UndefinedModel => CgCalChipType.Shape,
            CalChipSiteModelEnum.DswModel => CgCalChipType.DSW,
            CalChipSiteModelEnum.ShinyWaferModel => CgCalChipType.Shiny,
            CalChipSiteModelEnum.HazeModel => CgCalChipType.Haze,
            _ => ThrowHelper.ThrowArgumentOutOfRangeException<CgCalChipType>(nameof(@this))
        };
#endif
    }

#if NETFRAMEWORK

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

#endif

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

#if NET
        public CgMotionDir ToCgMotionDir() => @this switch
        {
            StageDirectionTypeEnum.UpLeft => CgMotionDir.LEFT_UP,
            StageDirectionTypeEnum.Up => CgMotionDir.Up,
            StageDirectionTypeEnum.UpRight => CgMotionDir.RIGHT_Up,
            StageDirectionTypeEnum.Left => CgMotionDir.LEFT,
            StageDirectionTypeEnum.Right => CgMotionDir.RIGHT,
            StageDirectionTypeEnum.DownLeft => CgMotionDir.LEFT_Down,
            StageDirectionTypeEnum.Down => CgMotionDir.Down,
            StageDirectionTypeEnum.DownRight => CgMotionDir.RIGHT_Down,
            _ => ThrowHelper.ThrowArgumentOutOfRangeException<CgMotionDir>(nameof(@this))
        };

#endif
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

#if NET
    extension(CgMotionDir @this)
    {
        public StageDirectionTypeEnum ToStageDirectionTypeEnum() => @this switch
        {
            CgMotionDir.LEFT_UP => StageDirectionTypeEnum.UpLeft,
            CgMotionDir.Up => StageDirectionTypeEnum.Up,
            CgMotionDir.RIGHT_Up => StageDirectionTypeEnum.UpRight,
            CgMotionDir.LEFT => StageDirectionTypeEnum.Left,
            CgMotionDir.RIGHT => StageDirectionTypeEnum.Right,
            CgMotionDir.LEFT_Down => StageDirectionTypeEnum.DownLeft,
            CgMotionDir.Down => StageDirectionTypeEnum.Down,
            CgMotionDir.RIGHT_Down => StageDirectionTypeEnum.DownRight,
            _ => ThrowHelper.ThrowArgumentOutOfRangeException<StageDirectionTypeEnum>(nameof(@this))
        };
    }

#endif

    #endregion DirectionType

    #region Speed

    extension(CgSpeedLevelType)
    {
        public static CgSpeedLevelType Default =>
#if NET
            (CgSpeedLevelType)(ushort.MaxValue);
#else
            CgSpeedLevelType.None;
#endif
    }

    extension(CgSpeedLevelType @this)
    {
        public StageSpeedEnum ToStageSpeedEnum() => @this switch
        {
            CgSpeedLevelType.Low => StageSpeedEnum.Low,
            CgSpeedLevelType.Mid => StageSpeedEnum.Middle,
            CgSpeedLevelType.High => StageSpeedEnum.High,
            _ => ThrowHelper.ThrowArgumentOutOfRangeException<StageSpeedEnum>(nameof(@this))
        };
    }

    extension(ESxLevelEnum @this)
    {
        public StageSpeedEnum ToStageSpeedEnum() => @this switch
        {
            ESxLevelEnum.Low => StageSpeedEnum.Low,
            ESxLevelEnum.Mid => StageSpeedEnum.Middle,
            ESxLevelEnum.High => StageSpeedEnum.High,
            _ => ThrowHelper.ThrowArgumentOutOfRangeException<StageSpeedEnum>(nameof(@this))
        };
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

        public StageSpeedEnum ToStageSpeedEnum() => @this switch
        {
            SxSpeedEnum.Low => StageSpeedEnum.Low,
            SxSpeedEnum.Mid => StageSpeedEnum.Middle,
            SxSpeedEnum.High => StageSpeedEnum.High,
            _ => ThrowHelper.ThrowArgumentOutOfRangeException<StageSpeedEnum>(nameof(@this))
        };

        public ESxLevelEnum ToESxLevelEnum() => @this switch
        {
            SxSpeedEnum.Low => ESxLevelEnum.Low,
            SxSpeedEnum.Mid => ESxLevelEnum.Mid,
            SxSpeedEnum.High => ESxLevelEnum.High,
            _ => ThrowHelper.ThrowArgumentOutOfRangeException<ESxLevelEnum>(nameof(@this))
        };
    }

    extension(StageSpeedEnum @this)
    {
        public CgSpeedLevelType ToCgSpeedLevelType() => @this switch
        {
            StageSpeedEnum.Low => CgSpeedLevelType.Low,
            StageSpeedEnum.Middle => CgSpeedLevelType.Mid,
            StageSpeedEnum.High => CgSpeedLevelType.High,
            _ => ThrowHelper.ThrowArgumentOutOfRangeException<CgSpeedLevelType>(nameof(@this))
        };

        public SxSpeedEnum ToSxSpeedEnum() => @this switch
        {
            StageSpeedEnum.Low => SxSpeedEnum.Low,
            StageSpeedEnum.Middle => SxSpeedEnum.Mid,
            StageSpeedEnum.High => SxSpeedEnum.High,
            _ => ThrowHelper.ThrowArgumentOutOfRangeException<SxSpeedEnum>(nameof(@this))
        };

        public ESxLevelEnum ToESxLevelEnum() => @this switch
        {
            StageSpeedEnum.Low => ESxLevelEnum.Low,
            StageSpeedEnum.Middle => ESxLevelEnum.Mid,
            StageSpeedEnum.High => ESxLevelEnum.High,
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
}