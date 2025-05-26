using CommunityToolkit.Mvvm.ComponentModel;
using Core.Models.Enums.Optics;
using Local.NoSQL.DB.Providers.Bases;
using Net.Utilities.Enums.Maths;

namespace Core.Models.Models.Common.DarkField;

public sealed partial class GenerateAodWaveFileCache : ObservableCacheBase
{
    /// <summary>
    /// AOD类型
    /// </summary>
    [ObservableProperty]
    private OpticsAodTypeEnum _aodTypeEnum;

    /// <summary>
    /// 频率单调性
    /// </summary>
    [ObservableProperty]
    private MonotonicTypeEnum _monotonicTypeEnum;

    #region ChirpAodParam

    /// <summary>
    /// 带宽=频率high-频率low MHz Chirp
    /// </summary>
    [ObservableProperty]
    private double _bandWidthChirp;

    /// <summary>
    /// 音包长度 Chirp
    /// </summary>
    [ObservableProperty]
    private double _soundPackageLengthChirp;

    /// <summary>
    /// 中心频率MHz Chirp
    /// </summary>
    [ObservableProperty]
    private double _centerFrequencyChirp = 280d;

    /// <summary>
    /// 二阶补偿系数 Chirp
    /// </summary>
    [ObservableProperty]
    private double _quadraticCompensationCoefficientChirp;

    /// <summary>
    /// 首尾端点缓冲(XTC响应不够)时间ns Chirp
    /// </summary>
    [ObservableProperty]
    private int _endpointDelayChirp = 300;

    /// <summary>
    /// 震动幅值 Chirp
    /// </summary>
    [ObservableProperty]
    private double _amplitudeChirp = 1d;

    /// <summary>
    /// 采样率 Chirp
    /// </summary>
    [ObservableProperty]
    private double _sampleRateChirp = 1064d;

    /// <summary>
    /// 补0个数
    /// </summary>
    [ObservableProperty]
    private int _zeroCountChirp;

    /// <summary>
    /// 生成文件重试次数 Chirp
    /// </summary>
    [ObservableProperty]
    private int _generateRetryCountChirp = 1000;

    /// <summary>
    /// 生成文件所在文件夹路径 Chirp
    /// </summary>
    [ObservableProperty]
    private string _aodDirectoryPathChirp = string.Empty;

    #endregion ChirpAodParam

    #region PrescanAodParam

    /// <summary>
    /// 带宽=频率high-频率low MHz Prescan
    /// </summary>
    [ObservableProperty]
    private double _bandWidthPrescan;

    /// <summary>
    /// 平坦时间(中心频率所占用的时间)ns Prescan
    /// </summary>
    [ObservableProperty]
    private double _flatnessTimePrescan;

    /// <summary>
    /// 生成文件所在文件夹路径 Prescan
    /// </summary>
    [ObservableProperty]
    private string _aodDirectoryPathPrescan = string.Empty;

    /// <summary>
    /// 二阶补偿系数 Prescan
    /// </summary>
    [ObservableProperty]
    private double _quadraticCompensationCoefficientPrescan;

    /// <summary>
    /// 中心频率MHz Prescan
    /// </summary>
    [ObservableProperty]
    private double _centerFrequencyPrescan = 280d;

    /// <summary>
    /// 首尾端点缓冲(XTC响应不够)时间ns Prescan
    /// </summary>
    [ObservableProperty]
    private int _endpointDelayPrescan = 300;

    /// <summary>
    /// 震动幅值 Prescan
    /// </summary>
    [ObservableProperty]
    private double _amplitudePrescan = 1d;

    /// <summary>
    /// 采样率 Prescan
    /// </summary>
    [ObservableProperty]
    private double _sampleRatePrescan = 1064d;

    /// <summary>
    /// 补0个数
    /// </summary>
    [ObservableProperty]
    private int _zeroCountPrescan;

    /// <summary>
    /// 生成文件重试次数 Prescan
    /// </summary>
    [ObservableProperty]
    private int _generateRetryCountPrescan = 1000;

    #endregion PrescanAodParam

    /// <summary>
    /// 生成AOD的波形
    /// </summary>
    [ObservableProperty]
    private List<(double, double)>? _aodWaveSignal;

    /// <summary>
    /// 生成AOD的傅里叶变化后波形
    /// </summary>
    [ObservableProperty]
    private List<(double, double)>? _aodWaveSignalFourier;

    /// <summary>
    /// 生成波形文件路径
    /// </summary>
    [ObservableProperty]
    private string _aodWaveGeneratedFilePath = string.Empty;

    public string SetDirectoryPath(string value) => AodTypeEnum switch
    {
        OpticsAodTypeEnum.Prescan => AodDirectoryPathPrescan = value,
        OpticsAodTypeEnum.Chirp => AodDirectoryPathChirp = value,
        _ => throw new ArgumentOutOfRangeException()
    };
}