using CommunityToolkit.Mvvm.ComponentModel;
using Core.Models.Enums.Microscope;
using Core.Models.Enums.Optics;
using Core.Models.Enums.Stage;
using Net.Utilities.Models.Geometries;

namespace Core.Models.Models.Laser.PixelSize;

public sealed partial class LaserPixelSizeCache : CalibrationCacheBase
{
    [ObservableProperty]
    private MicroscopeMagnificationEnum _microscopeMagnificationEnum = MicroscopeMagnificationEnum.Magnification5X;

    [ObservableProperty]
    private OpticsMagTypeEnum _opticsMagTypeEnum;

    [ObservableProperty]
    private double _chuckRadius = 150000;

    [ObservableProperty]
    private Point _findPosition;

    [ObservableProperty]
    private int _xWidthPixel = 800;

    [ObservableProperty]
    private StageSpeedEnum _xStageSpeedEnum = StageSpeedEnum.Low;

    [ObservableProperty]
    private double _pmtInterval = 320; // Pmt相机采集间隔320um

    [ObservableProperty]
    private double _verifyResultYPixelSize;

    [ObservableProperty]
    private double _threshold;
}