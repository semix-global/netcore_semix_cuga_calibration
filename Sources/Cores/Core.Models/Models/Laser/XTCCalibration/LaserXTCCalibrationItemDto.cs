using CommunityToolkit.Mvvm.ComponentModel;
using Core.Models.Enums.Microscope;
using Core.Models.Enums.Optics;
using Core.Models.Extensions;
using Core.Wcf.Models.Laser;
using Net.Utilities.Mapper.Interfaces;
using Net.Utilities.Models;

namespace Core.Models.Models.Laser.XTCCalibration;

public sealed partial class LaserXTCCalibrationItemDto : CalibrationDtoBase, ICloneable<LaserXTCCalibrationItemDto>, IAdaptTo<CalibrationLaserXTCCalibrationItem>
{
    [ObservableProperty]
    private MicroscopeMagnificationEnum _microscopeMagnificationEnum;

    [ObservableProperty]
    private OpticsMagTypeEnum _opticsMagTypeEnum;

    [ObservableProperty]
    private int _pmtId;

    [ObservableProperty]
    private double _cH1Delay;

    [ObservableProperty]
    private double _cH2Delay;

    [ObservableProperty]
    private double _cH3Delay;

    [ObservableProperty]
    private double _cH1DelayOffset;

    [ObservableProperty]
    private double _cH2DelayOffset;

    [ObservableProperty]
    private double _gain;

    [ObservableProperty]
    private Point _findPosition;

    [ObservableProperty]
    private string _channel1ImageFilePath = string.Empty;

    [ObservableProperty]
    private string _channel2ImageFilePath = string.Empty;

    [ObservableProperty]
    private string _channel3ImageFilePath = string.Empty;

    [ObservableProperty]
    private double[] _channel1DarkFieldImageProjectionYs = [];

    [ObservableProperty]
    private double[] _channel2DarkFieldImageProjectionYs = [];

    [ObservableProperty]
    private double[] _channel3DarkFieldImageProjectionYs = [];

    [ObservableProperty]
    private string _prescanFilePath = string.Empty;

    #region Mapper

    public LaserXTCCalibrationItemDto Clone() => new()
    {
        MicroscopeMagnificationEnum = MicroscopeMagnificationEnum,
        OpticsMagTypeEnum = OpticsMagTypeEnum,
        PmtId = PmtId,
        CH1Delay = CH1Delay,
        CH2Delay = CH2Delay,
        CH3Delay = CH3Delay,
        FindPosition = FindPosition,
        IsCalibrated = IsCalibrated,
        Gain = Gain,
        IsVerified = IsVerified,
        IsRequiredSelfCheck = IsRequiredSelfCheck,
        Id = Id,
        Expiration = Expiration
    };

    public CalibrationLaserXTCCalibrationItem AdaptTo() => new()
    {
        CgMagTypeEnum = OpticsMagTypeEnum.ToCgMagTypeEnum(),
        PmtId = PmtId,
        CH1Delay = Convert.ToInt32(CH1Delay),
        CH2Delay = Convert.ToInt32(CH2Delay),
        CH3Delay = Convert.ToInt32(CH3Delay),
        IsCalibrated = IsCalibrated,
        IsVerified = IsVerified,
        IsRequiredSelfCheck = IsRequiredSelfCheck
    };

    #endregion Mapper
}