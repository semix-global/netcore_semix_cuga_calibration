using CommunityToolkit.Mvvm.ComponentModel;
using Core.Models.Enums.Microscope;
using Net.Utilities.Models;

namespace Core.Models.Models.Chuck.Gantry;

public sealed partial class ChuckGantryCache : CalibrationCacheBase
{
    [ObservableProperty]
    private MicroscopeMagnificationEnum _lowMicroscopeMagnificationEnum = MicroscopeMagnificationEnum.Magnification5X;

    [ObservableProperty]
    private MicroscopeMagnificationEnum _highMicroscopeMagnificationEnum = MicroscopeMagnificationEnum.Magnification50X;

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

    public Point LowToHighPoint1 => HighFindPosition1 - LowFindPosition1;

    public Point LowToHighPoint2 => HighFindPosition2 - LowFindPosition2;
}