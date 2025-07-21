using CommunityToolkit.Mvvm.ComponentModel;
using Core.Models.Enums.Optics;
using Core.Models.Enums.Recipe.Wafer;
using Core.Models.Enums.Stage;
using Core.Models.Models.Pattern;
using Net.Utilities.Models.Geometries;

namespace Core.Models.Models.Laser.LineCentricity;

public sealed partial class LaserLineCentricityCache : CalibrationCacheBase
{
    [ObservableProperty]
    private MicroscopeMagnificationInfo _microscopeMagnificationInfo = new();

    [ObservableProperty]
    private WaferMaskTypeEnum _waferMaskTypeEnum = WaferMaskTypeEnum.GridConrner_100um;

    [ObservableProperty]
    private OpticsMagTypeEnum _opticsMagTypeEnum = OpticsMagTypeEnum.High;

    [ObservableProperty]
    private StageSpeedEnum _stageSpeedEnum = StageSpeedEnum.Low;

    [ObservableProperty]
    private double _chuckRadius = 150000;

    /// <summary>
    /// 选定特征的明场坐标
    /// </summary>
    [ObservableProperty]
    private Point _findPosition;

    /// <summary>
    /// 明场选定特征对应的stage机械坐标
    /// </summary>
    [ObservableProperty]
    private Point _findBrightMachinePosition;

    [ObservableProperty]
    private int _xWidthPixel = 800;

    [ObservableProperty]
    private double _pmtInterval = 320; // Pmt相机采集间隔320um

    [ObservableProperty]
    private string _brightTemplateFilePath = string.Empty;

    [ObservableProperty]
    private string _brightTemplateImageFilePath = string.Empty;

    [ObservableProperty]
    private string _templateFilePath = string.Empty;

    [ObservableProperty]
    private string _templateImageFilePath = string.Empty;

    [ObservableProperty]
    private Point _threshold;
}