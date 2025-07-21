using CommunityToolkit.Mvvm.ComponentModel;
using Core.Models.Enums.Recipe.Wafer;
using Core.Models.Models.Pattern;
using Net.Utilities.Models.Geometries;

namespace Core.Models.Models.Chuck.Gantry;

public sealed partial class ChuckGantryCache : CalibrationCacheBase
{
    [ObservableProperty]
    private MicroscopeMagnificationInfo _lowMicroscopeMagnificationInfo = new();

    [ObservableProperty]
    private MicroscopeMagnificationInfo _highMicroscopeMagnificationInfo = new();

    [ObservableProperty]
    private WaferMaskTypeEnum _waferMaskTypeEnum = WaferMaskTypeEnum.DieCorner_LeftBottom;

    [ObservableProperty]
    private double _verifyResultOffset;

    [ObservableProperty]
    private double _threshold;

    [ObservableProperty]
    private Point _lowFindPosition1 = new(0, 150);

    [ObservableProperty]
    private string _lowTemplateFilePath = string.Empty;

    [ObservableProperty]
    private string _lowTemplateImageFilePath = string.Empty;

    [ObservableProperty]
    private Point _lowFindPosition2 = new(0, -150);

    [ObservableProperty]
    private Point _highFindPosition1 = new(0, 150);

    [ObservableProperty]
    private string _highTemplateFilePath = string.Empty;

    [ObservableProperty]
    private string _highTemplateImageFilePath = string.Empty;

    [ObservableProperty]
    private Point _highFindPosition2 = new(0, -150);

    [ObservableProperty]
    private double _p5Angle;

    public Point LowToHighPoint1 => HighFindPosition1 - (Vector)LowFindPosition1;

    public Point LowToHighPoint2 => HighFindPosition2 - (Vector)LowFindPosition2;
}