using CommunityToolkit.Diagnostics;
using CommunityToolkit.Mvvm.ComponentModel;
using Core.Models.Enums.Optics;
using Net.Utilities.Helpers.Helpers.Files;
using Net.Utilities.Helpers.Helpers.Structs;
using System.Text;
using Net.Utilities.Models.Geometries;

namespace Core.Models.Models.Common.AODWaveform;

public abstract class AbstractAODWaveformProfile<T> : ObservableObject where T : AbstractAODWaveformProfile<T>
{
    private OpticsAODElectrodeEnum _opticsAODElectrodeEnum;
    private string _filePath = string.Empty;
    private int _zeroSampleCount;
    private double _offsetFrequency;
    private double _offsetFrequencyPeriodMultiple;
    private IReadOnlyList<short> _shortList = [];
    private IReadOnlyList<byte> _byteList = [];

    public OpticsAODElectrodeEnum OpticsAODElectrodeEnum
    {
        get => _opticsAODElectrodeEnum;
        internal set => SetProperty(ref _opticsAODElectrodeEnum, value);
    }

    public string FilePath
    {
        get => _filePath;
        internal set
        {
            if (SetProperty(ref _filePath, value)) OnFilePathChanged(value);
        }
    }

    public int ZeroSampleCount
    {
        get => _zeroSampleCount;
        internal set
        {
            if (SetProperty(ref _zeroSampleCount, value)) OnPropertyChanged(nameof(TotalSampleCount));
        }
    }

    public double OffsetFrequency
    {
        get => _offsetFrequency;
        internal set => SetProperty(ref _offsetFrequency, value);
    }

    public double OffsetFrequencyPeriodMultiple
    {
        get => _offsetFrequencyPeriodMultiple;
        internal set => SetProperty(ref _offsetFrequencyPeriodMultiple, value);
    }

    public int TotalSampleCount => ShortList.Count + ZeroSampleCount;

    public IReadOnlyList<short> ShortList
    {
        get => _shortList;
        private set
        {
            if (SetProperty(ref _shortList, value)) OnPropertyChanged(nameof(TotalSampleCount));
        }
    }

    public IReadOnlyList<byte> ByteList
    {
        get => _byteList;
        private set
        {
            if (SetProperty(ref _byteList, value)) OnPropertyChanged(nameof(TotalSampleCount));
        }
    }

    #region 波形

    /// <inheritdoc cref="Core.Utilities.AODWaveformGenerator.AODWaveformResultItem.Signals"/>
    public IReadOnlyList<Point> Signals { get; internal set; } = [];

    /// <inheritdoc cref="Core.Utilities.AODWaveformGenerator.AODWaveformResultItem.FFTSignals"/>
    public IReadOnlyList<Point> FFTSignals { get; internal set; } = [];

    /// <inheritdoc cref="Core.Utilities.AODWaveformGenerator.AODWaveformResultItem.FrequencyAmplitudes"/>
    public IReadOnlyList<Point> FrequencyAmplitudes { get; internal set; } = [];

    /// <inheritdoc cref="Core.Utilities.AODWaveformGenerator.AODWaveformResultItem.FlatnessLinearFrequencySignals"/>
    public IReadOnlyList<Point> FlatnessLinearFrequencySignals { get; internal set; } = [];

    /// <inheritdoc cref="Core.Utilities.AODWaveformGenerator.AODWaveformResultItem.FlatnessTotalFrequencySignals"/>
    public IReadOnlyList<Point> FlatnessTotalFrequencySignals { get; internal set; } = [];

    /// <inheritdoc cref="Core.Utilities.AODWaveformGenerator.AODWaveformResultItem.FlatnessAstigmatismCompensationSignals"/>
    public IReadOnlyList<Point> FlatnessAstigmatismCompensationSignals { get; internal set; } = [];

    /// <inheritdoc cref="Core.Utilities.AODWaveformGenerator.AODWaveformResultItem.FlatnessSphericalAberrationCompensationSignals"/>
    public IReadOnlyList<Point> FlatnessSphericalAberrationCompensationSignals { get; internal set; } = [];

    /// <inheritdoc cref="Core.Utilities.AODWaveformGenerator.AODWaveformResultItem.FlatnessSecondaryAstigmatismCompensationSignals"/>
    public IReadOnlyList<Point> FlatnessSecondaryAstigmatismCompensationSignals { get; internal set; } = [];

    /// <inheritdoc cref="Core.Utilities.AODWaveformGenerator.AODWaveformResultItem.FlatnessComaCompensationSignals"/>
    public IReadOnlyList<Point> FlatnessComaCompensationSignals { get; internal set; } = [];

    /// <inheritdoc cref="Core.Utilities.AODWaveformGenerator.AODWaveformResultItem.FlatnessTrefoilCompensationSignals"/>
    public IReadOnlyList<Point> FlatnessTrefoilCompensationSignals { get; internal set; } = [];

    /// <inheritdoc cref="Core.Utilities.AODWaveformGenerator.AODWaveformResultItem.FlatnessQuadrafoilCompensationSignals"/>
    public IReadOnlyList<Point> FlatnessQuadrafoilCompensationSignals { get; internal set; } = [];

    /// <inheritdoc cref="Core.Utilities.AODWaveformGenerator.AODWaveformResultItem.FlatnessAlphaOrderCompensationSignals"/>
    public IReadOnlyList<Point> FlatnessAlphaOrderCompensationSignals { get; internal set; } = [];

    #endregion 波形

    private void OnFilePathChanged(string value)
    {
        // $总byte长度$补零个数$包分割长度$下发寄存器号(02prescan, 03chirp)$偏移的频率$偏移的频率的2π周期的倍率$
        var strings = value.Split('$');
        Guard.IsTrue(strings.Length >= 7, "filePath name error.");

        ZeroSampleCount = int.Parse(strings[2]);
        OffsetFrequency = double.Parse(strings[5]);
        OffsetFrequencyPeriodMultiple = double.Parse(strings[6]);

        var resultString = File.ReadAllLines(value)
            .Select(t => t.Trim())
            .Where(t => string.IsNullOrWhiteSpace(t) == false)
            .ToList();

        if (resultString.Count <= 0 && resultString.All(t => t.Length == 4) == false) ThrowHelper.ThrowNotSupportedException("filePath value error.");

        ShortList = [.. resultString.Select(str => Convert.ToInt16(str, 16))];
        Signals = [..ShortList.Select((t, i) => new Point(i, (t - (t > Math.Pow(2d, 15d) ? Math.Pow(2d, 32d) : 0)) / Math.Pow(2d, 15d)))];

        SetByteList(1);
    }

    protected void SetByteList(double coefficient) => SetByteList([.. Enumerable.Repeat(coefficient, ShortList.Count)]);

    protected void SetByteList(IReadOnlyList<double> coefficientWindowList)
    {
        foreach (var coefficient in coefficientWindowList) Guard.IsTrue(coefficient >= 0, "coefficient is muse be >= 0.");
        Guard.IsTrue(ShortList.Count == coefficientWindowList.Count, "Count is not equal.");

        /*
         * double[-1,1]归一化数据需要转换为16-bit或32-bit整数格式进行传输[DSP、FPGA、DAC数模转换器硬件], 目前这个是16-bit PCM(脉冲编码调制)格式
         * 1. aodWaveSignal ∈ [-1, 1] 归一化
         * 2. 放大到 [-2^15, 2^15 - 1] 之间的16位整数(Int16的取值范围)
         *          # 防止溢出的四舍五入[-1，1] * 2^15 ∈ [-2^15, 2^15 - 1]
         *          1.0  :  +32767（避免溢出到 +32768，因为 Int16 最高是 32767）
         *          0.0  :  0
         *          -1.0 :  -32768
         * 3. Int16取值范围内, < 0: 负数在二进制补码表示下，等价于加 2^32
         *          # Int16的负数补码
         *          ((short)-900).ToString("X4")                        : FC7C
         *          ((long)-900).ToString("X4")                         : FFFFFC7C
         *          ((long)-900 + (long)Math.Pow(2, 32)).ToString("X4") : FFFFFC7C
         * 4. 获取Int16所有的补码, 只会有4位
         * 5. Excel拷贝txt显示曲线:
         *          一、公式: =(HEX2DEC(A1)-IF(HEX2DEC(A1)>=32768,65536,0))/POWER(2,16)
         *          二、不要[下拉填充点]直接推拽下拉太慢, 快速公式下拉填充: 直接双击[下拉填充点]一行直接生成
         */

        /*
         * 1. 将补码转换为 [-2^15, 2^15 - 1] Convert.ToInt16(str, 16)
         * 2. 将16位整数 * 增益Rate ∈ (0,1]
         * 3. 在转换为补码
         *          # BitConverter.GetBytes, 低位在前 高位在后, 按照内存顺序返回byte[], 但是转换为Hex字符串时, 高位在前 低位在后, 所以需要反向
         *          ((short)-900).ToString("X4")                              : FC7C
         *          BitConverter.ToString(BitConverter.GetBytes((short)-900)) : 7C-FC
         *          ((short)1).ToString("x4")                                 : 0001
         *          BitConverter.ToString(BitConverter.GetBytes((short)1))    : 01-00
         */

        var result = new List<byte>();
        foreach (var bytes in ShortList.Select((value, i) => (short)(value * coefficientWindowList[i])).Select(BitConverter.GetBytes))
        {
            result.Add(bytes[1]);
            result.Add(bytes[0]);
        }

        ByteList = result;
    }

    protected string Save(string directoryPath)
    {
        // $总byte长度$补零个数$包分割长度$下发寄存器号(02prescan, 03chirp)$偏移的频率$偏移的频率的2π周期的倍率$
        var strings = FilePath.Split('$');
        Guard.IsTrue(strings.Length >= 7, "filePath name error.");

        var registerId = strings[4];
        var filePath = Path.Combine(directoryPath, EnumHelper.ToDescriptionString(OpticsAODElectrodeEnum), $"{Guid.NewGuid():N}${TotalSampleCount}${ZeroSampleCount}$600${registerId}${OffsetFrequency:0.###}${OffsetFrequencyPeriodMultiple:0.###}$.txt");
        FileHelper.DeleteFileIfExists(filePath);
        DirectoryHelper.CreateFileDirectoryIfNotExists(filePath);

        Guard.IsTrue(ShortList.Count * 2 == ByteList.Count, "Count is not equal.");

        var stringBuilder = new StringBuilder();
        for (var i = 0; i < ByteList.Count; i += 2)
        {
            stringBuilder.AppendFormat("{0:X2}{1:X2}", ByteList[i], ByteList[i + 1]);
            stringBuilder.AppendLine();
        }

        File.WriteAllText(filePath, stringBuilder.ToString());

        return filePath;
    }

    protected T AdaptIn(T obj)
    {
        obj.OpticsAODElectrodeEnum = OpticsAODElectrodeEnum;
        obj.FilePath = FilePath;
        obj.ZeroSampleCount = ZeroSampleCount;
        obj.OffsetFrequency = OffsetFrequency;
        obj.OffsetFrequencyPeriodMultiple = OffsetFrequencyPeriodMultiple;
        obj.ShortList = [.. ShortList];
        obj.ByteList = [.. ByteList];

        obj.Signals = [..Signals];
        obj.FFTSignals = [..FFTSignals];
        obj.FrequencyAmplitudes = [..FrequencyAmplitudes];
        obj.FlatnessLinearFrequencySignals = [..FlatnessLinearFrequencySignals];
        obj.FlatnessTotalFrequencySignals = [..FlatnessTotalFrequencySignals];
        obj.FlatnessAstigmatismCompensationSignals = [..FlatnessAstigmatismCompensationSignals];
        obj.FlatnessSphericalAberrationCompensationSignals = [..FlatnessSphericalAberrationCompensationSignals];
        obj.FlatnessSecondaryAstigmatismCompensationSignals = [..FlatnessSecondaryAstigmatismCompensationSignals];
        obj.FlatnessComaCompensationSignals = [..FlatnessComaCompensationSignals];
        obj.FlatnessTrefoilCompensationSignals = [..FlatnessTrefoilCompensationSignals];
        obj.FlatnessQuadrafoilCompensationSignals = [..FlatnessQuadrafoilCompensationSignals];
        obj.FlatnessAlphaOrderCompensationSignals = [..FlatnessAlphaOrderCompensationSignals];

        return obj;
    }
}