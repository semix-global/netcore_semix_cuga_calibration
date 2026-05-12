using CommunityToolkit.Mvvm.ComponentModel;
using Core.Wcf.Models.Fourier;
using Cuga.Data.DataStruct.DTO.Recipe;
using Local.SQL.Cache.Providers.Bases;
using Net.Utilities.Mapper.Interfaces;
using Net.Utilities.Models.Geometries;

namespace Core.Models.Models.Fourier.CameraAlignment;

[CacheVersion("1.0.0")]
public sealed partial class PupilCameraAlignmentDTO : CalibrationDTOBase<PupilCameraAlignmentDTO>, IAdaptTo<CalibrationPupilCameraAlignment>
{
    [ObservableProperty]
    private Point _rectCh1Position = Point.Origin;

    [ObservableProperty]
    private int _ch1ImageWidth;

    [ObservableProperty]
    private int _ch1ImageHeight;

    [ObservableProperty]
    private Point _rectCh2Position = Point.Origin;

    [ObservableProperty]
    private int _ch2ImageWidth;

    [ObservableProperty]
    private int _ch2ImageHeight;

    [ObservableProperty]
    private Point _rectCh3Position = Point.Origin;

    [ObservableProperty]
    private int _ch3ImageWidth;

    [ObservableProperty]
    private int _ch3ImageHeight;

    #region Mapper

    public override PupilCameraAlignmentDTO Clone()
    {
        return new PupilCameraAlignmentDTO
        {
            RectCh1Position = RectCh1Position,
            Ch1ImageWidth = Ch1ImageWidth,
            Ch1ImageHeight = Ch1ImageHeight,
            RectCh2Position = RectCh2Position,
            Ch2ImageWidth = Ch2ImageWidth,
            Ch2ImageHeight = Ch2ImageHeight,
            RectCh3Position = RectCh3Position,
            Ch3ImageWidth = Ch3ImageWidth,
            Ch3ImageHeight = Ch3ImageHeight,
            IsCalibrated = IsCalibrated,
            IsVerified = IsVerified,
            IsRequiredSelfCheck = IsRequiredSelfCheck,
            Id = Id,
            Expiration = Expiration
        };
    }

    public CalibrationPupilCameraAlignment AdaptTo() => new()
    {
        RectCh1 = new RectD((int)Math.Round(RectCh1Position.X), (int)Math.Round(RectCh1Position.Y), Ch1ImageWidth, Ch1ImageHeight),
        RectCh2 = new RectD((int)Math.Round(RectCh2Position.X), (int)Math.Round(RectCh2Position.Y), Ch2ImageWidth, Ch2ImageHeight),
        RectCh3 = new RectD((int)Math.Round(RectCh3Position.X), (int)Math.Round(RectCh3Position.Y), Ch3ImageWidth, Ch3ImageHeight),
        IsCalibrated = IsCalibrated,
        IsVerified = IsVerified,
        IsRequiredCalibrate = IsRequiredSelfCheck
    };

    #endregion Mapper
}