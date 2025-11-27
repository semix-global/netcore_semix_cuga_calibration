using CommunityToolkit.Mvvm.ComponentModel;
using Core.Models.Enums.Optics;
using Core.Models.Helper;
using Net.Utilities.Models.Geometries;

namespace Core.Models.Models.Laser.Rtfc;

public sealed partial class RtfcCache : CalibrationCacheBase
{
    [ObservableProperty]
    private OpticsMagTypeEnum _opticsMagTypeEnum = OpticsMagTypeEnum.High;

    [ObservableProperty]
    private OpticsIncidentModeEnum _opticsIncidentModeEnum = CalibrationConstantsHelper.MainOpticsIncidentModeEnum;

    /// <summary>
    /// 入射角（°）
    /// </summary>
    [ObservableProperty]
    private double _obliqueAngle = 53;

    [ObservableProperty]
    private Point _ideaDarkFieldMachinePosition;

    [ObservableProperty]
    private double _findFocusMin;

    [ObservableProperty]
    private double _findFocusMax;

    [ObservableProperty]
    private double _findFocusInterval;

    [ObservableProperty]
    private double _offsetThreshold;
}