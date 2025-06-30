using CommunityToolkit.Mvvm.ComponentModel;
using Core.Models.Enums.Optics;
using Local.NoSQL.DB.Providers.Bases;
using Net.Utilities.Mapper.Interfaces;
using Net.Utilities.Models.Geometries;

namespace Core.Models.Models.Laser.PrescanChirpAodAlignment;

public sealed partial class LaserPrescanChirpAodAlignmentDto : CalibrationDtoBase, ICloneable<LaserPrescanChirpAodAlignmentDto>
{
    [ObservableProperty]
    private OpticsMagTypeEnum _opticsMagTypeEnum;

    [ObservableProperty]
    private Point _findPosition;

    [ObservableProperty]
    private double _prescanCoefficient;

    [ObservableProperty]
    private double _gain;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(ItemPoints))]
    private LaserPrescanChirpAodAlignmentItemDto[] _items = [];

    public Point[] ItemPoints => [.. Items.Select(t => new Point(t.PrescanCenterFrequency, t.DarkFieldImageProjectionYsMaxPixel))];

    [ObservableProperty]
    private double _slope;

    [ObservableProperty]
    private double _intercept;

    [ObservableProperty]
    private double _rSquared;

    [ObservableProperty]
    private Point[] _itemFitPoints = [];

    [ObservableProperty]
    private Point[] _prescanSignals = [];

    [ObservableProperty]
    private Point[] _prescanFouriers = [];

    [ObservableProperty]
    private string _prescanFilePath = string.Empty;

    #region Mapper

    public LaserPrescanChirpAodAlignmentDto Clone() => new()
    {
        OpticsMagTypeEnum = OpticsMagTypeEnum,
        FindPosition = FindPosition,
        PrescanCoefficient = PrescanCoefficient,
        Gain = Gain,
        Items = [.. Items.Select(x => x.Clone())],
        Slope = Slope,
        Intercept = Intercept,
        RSquared = RSquared,
        ItemFitPoints = [.. ItemFitPoints],
        PrescanSignals = [.. PrescanSignals],
        PrescanFouriers = [.. PrescanFouriers],
        PrescanFilePath = PrescanFilePath,
        IsCalibrated = IsCalibrated,
        IsVerified = IsVerified,
        IsRequiredSelfCheck = IsRequiredSelfCheck,
        Id = Id,
        Expiration = Expiration
    };

    #endregion Mapper

    public void Clear()
    {
        Items = [];
        Slope = 0;
        Intercept = 0;
        RSquared = 0;
        ItemFitPoints = [];
        PrescanSignals = [];
        PrescanFouriers = [];
        PrescanFilePath = string.Empty;
    }
}

public sealed partial class LaserPrescanChirpAodAlignmentItemDto : ObservableCacheBase, ICloneable<LaserPrescanChirpAodAlignmentItemDto>
{
    [ObservableProperty]
    private double _prescanCenterFrequency;

    [ObservableProperty]
    private OpticsMagTypeEnum _opticsMagTypeEnum;

    [ObservableProperty]
    private Point[] _prescanSignals = [];

    [ObservableProperty]
    private Point[] _prescanFouriers = [];

    [ObservableProperty]
    private string _prescanFilePath = string.Empty;

    [ObservableProperty]
    private string _channel1ImageFilePath = string.Empty;

    [ObservableProperty]
    private double[] _channel1DarkFieldImageProjectionYs = [];

    [ObservableProperty]
    private string _channel2ImageFilePath = string.Empty;

    [ObservableProperty]
    private double[] _channel2DarkFieldImageProjectionYs = [];

    [ObservableProperty]
    private string _channel3ImageFilePath = string.Empty;

    [ObservableProperty]
    private double[] _channel3DarkFieldImageProjectionYs = [];

    [ObservableProperty]
    private int _darkFieldImageProjectionYsMaxPixel;

    [ObservableProperty]
    private double _darkFieldImageProjectionYsMaxValue;

    #region Mapper

    public LaserPrescanChirpAodAlignmentItemDto Clone() => new()
    {
        PrescanCenterFrequency = PrescanCenterFrequency,
        OpticsMagTypeEnum = OpticsMagTypeEnum,
        PrescanSignals = [.. PrescanSignals],
        PrescanFouriers = [.. PrescanFouriers],
        PrescanFilePath = PrescanFilePath,
        Channel1ImageFilePath = Channel1ImageFilePath,
        Channel1DarkFieldImageProjectionYs = [.. Channel1DarkFieldImageProjectionYs],
        Channel2ImageFilePath = Channel2ImageFilePath,
        Channel2DarkFieldImageProjectionYs = [.. Channel2DarkFieldImageProjectionYs],
        Channel3ImageFilePath = Channel3ImageFilePath,
        Channel3DarkFieldImageProjectionYs = [.. Channel3DarkFieldImageProjectionYs],
        DarkFieldImageProjectionYsMaxPixel = DarkFieldImageProjectionYsMaxPixel,
        DarkFieldImageProjectionYsMaxValue = DarkFieldImageProjectionYsMaxValue,
        Id = Id,
        Expiration = Expiration
    };

    #endregion Mapper
}