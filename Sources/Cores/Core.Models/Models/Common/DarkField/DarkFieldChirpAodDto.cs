using CommunityToolkit.Diagnostics;
using CommunityToolkit.Mvvm.ComponentModel;
using Local.NoSQL.DB.Providers.Bases;
using Net.Utilities.Algorithm.MathNet.Modules;
using Net.Utilities.Enums.Maths;
using Net.Utilities.Mapper.Interfaces;

namespace Core.Models.Models.Common.DarkField;

/// <summary>
/// Chirp AOD 波形
/// </summary>
public sealed partial class DarkFieldChirpAodWaveDto : ObservableCacheBase, ICloneable<DarkFieldChirpAodWaveDto>
{
    /// <summary>
    /// 原始文件寄存器数量
    /// </summary>
    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(RegNum))]
    private short _oriRegNum;

    /// <summary>
    /// 寄存器数量
    /// </summary>
    public short RegNum => (short)(OriRegNum + ZeroNum);

    /// <summary>
    /// 补0个数
    /// </summary>
    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(RegNum))]
    private short _zeroNum;

    /// <summary>
    /// 频率低
    /// </summary>
    [ObservableProperty]
    private double _bandWidthHigh;

    /// <summary>
    /// 频率高
    /// </summary>
    [ObservableProperty]
    private double _bandWidthLow;

    /// <summary>
    /// 带宽=频率high-频率low MHz
    /// </summary>
    [ObservableProperty]
    private double _bandWidth;

    /// <summary>
    /// 音包长度
    /// </summary>
    [ObservableProperty]
    private double _soundPackageLength;

    /// <summary>
    /// 中心频率MHz
    /// </summary>
    [ObservableProperty]
    private double _centerFrequency = 280d;

    /// <summary>
    /// 首尾端点缓冲(XTC响应不够)时间ns
    /// </summary>
    [ObservableProperty]
    private int _endpointDelay;

    /// <summary>
    /// 震动幅值
    /// </summary>
    [ObservableProperty]
    private double _amplitude = 1d;

    /// <summary>
    /// 采样率
    /// </summary>
    [ObservableProperty]
    private double _sampleRate = 1064d;

    /// <summary>
    /// 生成文件重试次数
    /// </summary>
    [ObservableProperty]
    private int _generateRetryCount = 1000;

    private double _rateChange;

    public double RateChange
    {
        get => BandWidthHigh == 0 ? _rateChange : (BandWidthHigh - BandWidthLow) / SoundPackageLength;
        set => _rateChange = value;
    }

    /// <summary>
    /// 生成的文件路径
    /// </summary>
    [ObservableProperty]
    private string _incrementChirpAodFilePath = string.Empty;

    /// <summary>
    /// chirpAod波形原始列表
    /// </summary>
    [ObservableProperty]
    private List<short> _chirpAodWaveList = [];

    /// <summary>
    /// chirpAod波形下发列表
    /// </summary>
    [ObservableProperty]
    private List<byte> _chirpAodWaveByteList = [];

    public (List<(double, double)> AodWaveSignal, List<(double, double)> AodWaveSignalFourier, bool isSuccess) GenerateChirpAodWave()
    {
        var directoryName = Path.GetDirectoryName(IncrementChirpAodFilePath);

        Guard.IsNotNullOrWhiteSpace(directoryName, nameof(directoryName));

        var (isSuccess,
            aodWaveFilePath,
            _,
            _,
            _,
            _,
            _,
            _,
            _,
            _,
            _,
            _,
            _,
            _,
            _,
            _,
            aodWaveSignals,
            aodWaveSignalsFourier,
            _) = AodWaveGenerator.GenerateChirpAodWaveFile(
            RateChange * SoundPackageLength,
            CenterFrequency,
            SoundPackageLength,
            MonotonicTypeEnum.Deceasing,
            SampleRate,
            Amplitude,
            directoryName,
            zeroSampleCount: ZeroNum,
            endpointSampleCount: EndpointDelay,
            generateRetryTimes: GenerateRetryCount);

        IncrementChirpAodFilePath = aodWaveFilePath;
        return (aodWaveSignals.Select(t => (t.X, t.Y)).ToList(), aodWaveSignalsFourier.Select(t => (t.X, t.Y)).ToList(), isSuccess);
    }

    #region Mapper

    public DarkFieldChirpAodWaveDto Clone() => new()
    {
        OriRegNum = OriRegNum,
        ZeroNum = ZeroNum,
        BandWidthHigh = BandWidthHigh,
        BandWidthLow = BandWidthLow,
        BandWidth = BandWidth,
        SoundPackageLength = SoundPackageLength,
        CenterFrequency = CenterFrequency,
        EndpointDelay = EndpointDelay,
        Amplitude = Amplitude,
        SampleRate = SampleRate,
        GenerateRetryCount = GenerateRetryCount,
        RateChange = RateChange,
        IncrementChirpAodFilePath = IncrementChirpAodFilePath,
        ChirpAodWaveList = [.. ChirpAodWaveList],
        ChirpAodWaveByteList = [.. ChirpAodWaveByteList]
    };

    #endregion Mapper
}