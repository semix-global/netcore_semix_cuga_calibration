using CommunityToolkit.Mvvm.ComponentModel;
using Core.Models.Enums.Stage;
using Core.Models.Models.Common.Pattern;
using Core.Models.Models.Setting;
using Net.Utilities.Models.Geometries;
using System.Collections.ObjectModel;

namespace Core.Models.Models.Laser.DOEAngle;

public sealed partial class LaserDOEAngleCache : CalibrationCacheBase
{
    [ObservableProperty]
    private MicroscopeLensInformation _microscopeLensInformation = MicroscopeLensInformation.Default;

    [ObservableProperty]
    private CIBConfiguration _cIBConfiguration = new();

    [ObservableProperty]
    private CalChipSiteModelEnum _calChipSiteModelEnum = CalChipSiteModelEnum.DswModel;

    [ObservableProperty]
    private ObservableCollection<PmtConfigParam> _pmtConfigList = [];

    [ObservableProperty]
    private Point _findPosition = Point.Origin;

    [ObservableProperty]
    private double _originDOEAngle;

    /// <summary>
    ///  um
    /// </summary>
    [ObservableProperty]
    private double _pmtInterval = 320;

    /// <summary>
    /// 入射角（°）
    /// </summary>
    [ObservableProperty]
    private double _obliqueAngle = 53;

    /// <summary>
    /// um/ecs
    /// </summary>
    [ObservableProperty]
    private double _umPerEcs = 0.2;

    /// <summary>
    /// Af Offset(mm)/ecs
    /// </summary>
    [ObservableProperty]
    private double _ecsPerAfOffset = 23;

    [ObservableProperty]
    private double _threshold;

    [ObservableProperty]
    private int _retryCount = 5;
}