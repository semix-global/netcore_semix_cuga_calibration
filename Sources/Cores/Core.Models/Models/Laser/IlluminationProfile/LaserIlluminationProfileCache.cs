using CommunityToolkit.Mvvm.ComponentModel;
using Core.Models.Enums.Microscope;
using Core.Models.Enums.Optics;
using Core.Models.Enums.Stage;
using Net.Utilities.Models;
using Newtonsoft.Json;
using System.Collections.Concurrent;

namespace Core.Models.Models.Laser.IlluminationProfile;

public sealed partial class LaserIlluminationProfileCache : CalibrationCacheBase
{
    [ObservableProperty]
    private MicroscopeMagnificationEnum _microscopeMagnificationEnum = MicroscopeMagnificationEnum.Magnification5X;

    [ObservableProperty]
    private StageSpeedEnum _xSpeed = StageSpeedEnum.Low;

    [ObservableProperty]
    private int _pmtId = 8;

    [ObservableProperty]
    private int _channelId = 3;

    [ObservableProperty]
    private int _widthPixel = 800;

    [ObservableProperty]
    private double _waitTime = 15;

    [ObservableProperty]
    private double _darkImageListRangeThreshold = 1000;

    /// <summary>
    /// 校准的位置
    /// </summary>
    [ObservableProperty]
    private Point _findPosition;

    /// <summary>
    /// 校准前后去dsw的位置1采图
    /// </summary>
    [ObservableProperty]
    private Point _dswPosition1;

    /// <summary>
    /// 校准前后去dsw的位置2采图
    /// </summary>
    [ObservableProperty]
    private Point _dswPosition2;

    /// <summary>
    /// 校准前后去dsw的位置3采图
    /// </summary>
    [ObservableProperty]
    private Point _dswPosition3;

    /// <summary>
    /// dsw增益
    /// </summary>
    [ObservableProperty]
    private double _dswGain = -1;

    /// <summary>
    /// 校准阈值
    /// </summary>
    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(CalibrateThresholdDarkFieldImageListRateMin), nameof(CalibrateThresholdDarkFieldImageListRateMax))]
    private double _calibrateThreshold = 0.05;

    /// <summary>
    /// 校准比值最小值
    /// </summary>
    [JsonIgnore]
    public double CalibrateThresholdDarkFieldImageListRateMin => 1 - CalibrateThreshold;

    /// <summary>
    /// 校准比值最大值
    /// </summary>
    [JsonIgnore]
    public double CalibrateThresholdDarkFieldImageListRateMax => 1 + CalibrateThreshold;

    /// <summary>
    /// 验证阈值
    /// </summary>
    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(VerifyThresholdDarkFieldImageListRateMin), nameof(VerifyThresholdDarkFieldImageListRateMax))]
    private double _verifyThreshold = 0.05;

    /// <summary>
    /// 验证比值最小值
    /// </summary>
    [JsonIgnore]
    public double VerifyThresholdDarkFieldImageListRateMin => 1 - VerifyThreshold;

    /// <summary>
    /// 验证比值最大值
    /// </summary>
    [JsonIgnore]
    public double VerifyThresholdDarkFieldImageListRateMax => 1 + VerifyThreshold;

    /// <summary>
    /// MAG类型
    /// </summary>
    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(CurrentDarkFieldImageListToPrescanListCacheItem), nameof(CurrentCalibrationCacheItem))]
    private OpticsMagTypeEnum _opticsMagTypeEnum;

    /// <summary>
    /// 波形功率系数(1表示100%, 0表示0%)
    /// </summary>
    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(CurrentCalibrationCacheItem))]
    private double _coefficient = 1.0;

    [ObservableProperty]
    private ConcurrentDictionary<string, LaserIlluminationProfileDarkFieldImageListToPrescanListCacheItem> _darkFieldImageListToPrescanListCacheItemDic = [];

    [ObservableProperty]
    private ConcurrentDictionary<string, LaserIlluminationProfileCalibrationCacheItem> _calibrationCacheItemDic = [];

    [JsonIgnore]
    public LaserIlluminationProfileDarkFieldImageListToPrescanListCacheItem CurrentDarkFieldImageListToPrescanListCacheItem =>
        DarkFieldImageListToPrescanListCacheItemDic.GetOrAdd($"{OpticsMagTypeEnum}", new LaserIlluminationProfileDarkFieldImageListToPrescanListCacheItem());

    [JsonIgnore]
    public LaserIlluminationProfileCalibrationCacheItem CurrentCalibrationCacheItem =>
        CalibrationCacheItemDic.GetOrAdd($"{OpticsMagTypeEnum}-{Coefficient:f6}", new LaserIlluminationProfileCalibrationCacheItem());

    partial void OnOpticsMagTypeEnumChanged(OpticsMagTypeEnum value)
    {
        _ = value;
        CurrentDarkFieldImageListToPrescanListCacheItem.Reset();
        CurrentCalibrationCacheItem.Reset();
    }

    partial void OnCoefficientChanged(double value)
    {
        _ = value;
        CurrentCalibrationCacheItem.Reset();
    }

    public List<double> GetDarkFieldImageList(List<double> darkFieldImageList)
    {
        return CurrentDarkFieldImageListToPrescanListCacheItem is { IsOk: true, IsReviseDarkFieldImageToPrescan: true } ? [.. darkFieldImageList.AsEnumerable().Reverse()] : darkFieldImageList;
    }
}

public sealed partial class LaserIlluminationProfileCalibrationCacheItem : CalibrationCacheBase
{
    #region 参数

    /// <summary>
    /// 波形功率系数限制(1表示100%, 0表示0%)
    /// </summary>
    [ObservableProperty]
    private double _coefficientLimit = 0.2;

    /// <summary>
    /// 重复次数
    /// </summary>
    [ObservableProperty]
    private int _repeatCount = 100;

    /// <summary>
    /// 间隔
    /// </summary>
    [ObservableProperty]
    private double _repeatCoefficientInterval = 0.01;

    /// <summary>
    /// 去掉首位的点数
    /// </summary>
    [ObservableProperty]
    private int _judgeDarkFieldImageListRateSkipCout = 50;

    #endregion 参数

    [ObservableProperty]
    private List<LaserIlluminationProfileCalibrationPmtIdItem> _LaserIlluminationProfileCalibrationPmtList = [];

    #region 份数 与 最大功率值 对应关系

    /// <summary>
    /// 份数 与 最大功率值 对应关系是否已经成功
    /// </summary>
    [ObservableProperty]
    private bool _isOk;

    /// <summary>
    /// (功率, (索引代表份数, DarkFieldImageListAverage)[])
    /// </summary>
    [ObservableProperty]
    private List<(double Coefficient, double[] ServingToDarkFieldImageListAverages)> _coefficientToServingToDarkFieldImageListAveragesList = [];

    /// <summary>
    /// (索引代表份数, 最大功率值)
    /// </summary>
    [ObservableProperty]
    private List<double> _servingToMaxCoefficientList = [];

    #endregion 份数 与 最大功率值 对应关系

    public void Reset()
    {
        IsOk = false;
        CoefficientToServingToDarkFieldImageListAveragesList = [];
        ServingToMaxCoefficientList = [];
    }
}

public sealed partial class LaserIlluminationProfileCalibrationPmtIdItem : CalibrationCacheBase
{
    #region 参数

    /// <summary>
    /// Pmt
    /// </summary>
    [ObservableProperty]
    private int _pmtId = 8;

    [ObservableProperty]
    private int _channelId = 3;

    /// <summary>
    /// 重复次数
    /// </summary>
    [ObservableProperty]
    private Point _pmtIdPosition;

    /// <summary>
    /// 增益值
    /// </summary>
    [ObservableProperty]
    private double _gain;

    #endregion 参数
}

/// <summary>
/// DarkFieldImageList 与 PrescanList 对应关系
/// </summary>
public sealed partial class LaserIlluminationProfileDarkFieldImageListToPrescanListCacheItem : CalibrationCacheBase
{
    /// <summary>
    /// 对应关系是否已经成功
    /// </summary>
    [ObservableProperty]
    private bool _isOk;

    #region 多V对应关系

    /// <summary>
    /// 波形多V形起始索引
    /// </summary>
    [ObservableProperty]
    private int _waveFormVStartIndex = 200;

    /// <summary>
    /// 波形多V形结束索引
    /// </summary>
    [ObservableProperty]
    private int _waveFormVEndIndex = 5000;

    /// <summary>
    /// 波形多V形下降和上升步长范围
    /// </summary>
    [ObservableProperty]
    private int _waveFormVInterval = 500;

    /// <summary>
    /// DarkFieldImageList
    /// </summary>
    [ObservableProperty]
    private List<double> _waveFormVDarkFieldImageList = [];

    /// <summary>
    /// 平滑后的DarkFieldImageList
    /// </summary>
    [ObservableProperty]
    private List<double> _waveFormVSmoothDarkFieldImageList = [];

    /// <summary>
    /// prescanList
    /// </summary>
    [ObservableProperty]
    private List<double> _waveFormVPrescanList = [];

    /// <summary>
    /// DarkFieldImageListMin
    /// </summary>
    [ObservableProperty]
    private List<int> _waveFormVImageMinList = [];

    /// <summary>
    /// prescanListMin
    /// </summary>
    [ObservableProperty]
    private List<int> _waveFormVPrescanMinList = [];

    #endregion 多V对应关系

    #region 参数

    /// <summary>
    /// 校准的prescan文件路径
    /// </summary>
    [ObservableProperty]
    private string _prescanFilePath = string.Empty;

    /// <summary>
    /// 份数
    /// </summary>
    [ObservableProperty]
    private int _servings = 100;

    /// <summary>
    /// 大一点的窗口索引
    /// </summary>
    [ObservableProperty]
    private int _maxWindowStartIndex = 2000;

    /// <summary>
    /// 小一点的窗口索引
    /// </summary>
    [ObservableProperty]
    private int _minWindowStartIndex = 1500;

    /// <summary>
    /// 窗口到最小值的数量
    /// </summary>
    [ObservableProperty]
    private int _windowToMinAmount = 1000;

    /// <summary>
    /// 寻转窗口的范围的起始索引
    /// </summary>
    [ObservableProperty]
    private int _judgeWindowStartIndex = 300;

    /// <summary>
    /// 寻转窗口的范围的结束索引
    /// </summary>
    [ObservableProperty]
    private int _judgeWindowEndIndex = 600;

    #endregion 参数

    #region 数据

    /// <summary>
    /// 大一点的窗口索引对应的暗场图片最小值点
    /// </summary>
    [ObservableProperty]
    private int _maxWindowDarkImageListMinIndex;

    /// <summary>
    /// 小一点的窗口索引对应的暗场图片最小值点
    /// </summary>
    [ObservableProperty]
    private int _minWindowDarkImageListMinIndex;

    /// <summary>
    /// DarkFieldImage对应Prescan的点
    /// </summary>
    [ObservableProperty]
    private int _t1;

    /// <summary>
    /// DarkFieldImage对应Prescan需不需要反向
    /// </summary>
    [ObservableProperty]
    private bool _isReviseDarkFieldImageToPrescan;

    /// <summary>
    /// 起始位置
    /// </summary>
    [ObservableProperty]
    private int _prescanStartIndex;

    /// <summary>
    /// 结束位置
    /// </summary>
    [ObservableProperty]
    private int _prescanEndIndex;

    /// <summary>
    /// 大一点的窗口PrescanList
    /// </summary>
    [ObservableProperty]
    private List<double> _maxWindowPrescanList = [];

    /// <summary>
    /// 大一点的窗口DarkFieldImageList
    /// </summary>
    [ObservableProperty]
    private List<double> _maxScatterDarkFieldImageList = [];

    /// <summary>
    /// 大一点的窗口平滑后的DarkFieldImageList
    /// </summary>
    [ObservableProperty]
    private List<double> _maxSmoothDarkFieldImageList = [];

    /// <summary>
    /// 小一点的窗口PrescanList
    /// </summary>
    [ObservableProperty]
    private List<double> _minWindowPrescanList = [];

    /// <summary>
    /// 小一点的窗口DarkFieldImageList
    /// </summary>
    [ObservableProperty]
    private List<double> _minScatterDarkFieldImageList = [];

    /// <summary>
    /// 小一点的窗口平滑后的DarkFieldImageList
    /// </summary>
    [ObservableProperty]
    private List<double> _minSmoothDarkFieldImageList = [];

    #endregion 数据

    #region 结果

    /// <summary>
    /// PrescanList: (索引代表份数, PrescanList索引列表)
    /// </summary>
    [ObservableProperty]
    private List<int[]> _servingToPrescanListIndicesList = [];

    /// <summary>
    /// DarkFieldImageList: (索引代表份数, DarkFieldImageList索引列表)
    /// </summary>
    [ObservableProperty]
    private List<int[]> _servingToDarkFieldImageListIndicesList = [];

    #endregion 结果

    partial void OnPrescanFilePathChanged(string value)
    {
        _ = value;
        Reset();
    }

    partial void OnServingsChanged(int value)
    {
        _ = value;
        Reset();
    }

    partial void OnMaxWindowStartIndexChanged(int value)
    {
        _ = value;
        Reset();
    }

    partial void OnMinWindowStartIndexChanged(int value)
    {
        _ = value;
        Reset();
    }

    partial void OnWindowToMinAmountChanged(int value)
    {
        _ = value;
        Reset();
    }

    partial void OnJudgeWindowStartIndexChanged(int value)
    {
        _ = value;
        Reset();
    }

    partial void OnJudgeWindowEndIndexChanged(int value)
    {
        _ = value;
        Reset();
    }

    public void Reset()
    {
        IsOk = false;
        MaxWindowDarkImageListMinIndex = 0;
        MinWindowDarkImageListMinIndex = 0;
        T1 = 0;
        IsReviseDarkFieldImageToPrescan = false;
        PrescanStartIndex = 0;
        PrescanEndIndex = 0;
        MaxWindowPrescanList = [];
        MaxScatterDarkFieldImageList = [];
        MaxSmoothDarkFieldImageList = [];
        MinWindowPrescanList = [];
        MinScatterDarkFieldImageList = [];
        MinSmoothDarkFieldImageList = [];
        ServingToPrescanListIndicesList = [];
        ServingToDarkFieldImageListIndicesList = [];
    }
}