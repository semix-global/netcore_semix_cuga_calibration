using CommunityToolkit.Mvvm.ComponentModel;
using Local.NoSQL.DB.Providers.Bases;
using Net.Utilities.Mapper.Interfaces;

namespace Core.Models.Models.Common.DarkField;

/// <summary>
/// 扫描线功率
/// </summary>
public sealed partial class DarkFieldPrescanDto : ObservableCacheBase, ICloneable<DarkFieldPrescanDto>
{
    /// <summary>
    /// 寄存器数量, byte数组总长度
    /// </summary>
    [ObservableProperty]
    private short _regNum;

    /// <summary>
    /// 补0个数
    /// </summary>
    [ObservableProperty]
    private short _zeroNum;

    /// <summary>
    /// 带宽=频率high-频率low MHz
    /// </summary>
    [ObservableProperty]
    private double _bandWidth;

    /// <summary>
    /// 平坦时间(中心频率所占用的时间)ns
    /// </summary>
    [ObservableProperty]
    private double _flatnessTime;

    /// <summary>
    /// 生成文件所在文件夹路径
    /// </summary>
    [ObservableProperty]
    private string _aodDirectoryPath = string.Empty;

    /// <summary>
    /// 中心频率MHz
    /// </summary>
    [ObservableProperty]
    private double _centerFrequency = 280d;

    /// <summary>
    /// 频率是否递增
    /// </summary>
    [ObservableProperty]
    private bool _isMonotonicIncreasing;

    /// <summary>
    /// 首尾端点缓冲(XTC响应不够)时间ns
    /// </summary>
    [ObservableProperty]
    private int _endpointDelay = 300;

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

    /// <summary>
    /// 扫描线功率原始列表
    /// </summary>
    [ObservableProperty]
    private List<short> _prescanList = [];

    /// <summary>
    /// 扫描线功率下发列表
    /// </summary>
    [ObservableProperty]
    private List<byte> _prescanByteList = [];

    #region Mapper

    public DarkFieldPrescanDto Clone() => new()
    {
        RegNum = RegNum,
        ZeroNum = ZeroNum,
        BandWidth = BandWidth,
        FlatnessTime = FlatnessTime,
        AodDirectoryPath = AodDirectoryPath,
        CenterFrequency = CenterFrequency,
        IsMonotonicIncreasing = IsMonotonicIncreasing,
        EndpointDelay = EndpointDelay,
        Amplitude = Amplitude,
        SampleRate = SampleRate,
        GenerateRetryCount = GenerateRetryCount,
        PrescanList = [.. PrescanList],
        PrescanByteList = [.. PrescanByteList],
        Id = Id,
        Expiration = Expiration
    };

    #endregion Mapper
}