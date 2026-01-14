using CommunityToolkit.Diagnostics;
using Core.Models.Enums.Optics;
using Local.NoSQL.DB.Providers.Bases;
using Net.Utilities.Algorithms.Modules;
using Net.Utilities.Helpers.Helpers.Files;
using Net.Utilities.Helpers.Helpers.Structs;
using Net.Utilities.Mapper.Interfaces;
using Net.Utilities.Models.Geometries;
using Net.Utilities.Nlog.Entities.HtmlElements;
using System.IO;
using System.Text;

namespace Core.Models.Models.Common.AODWaveform;

public abstract class AbstractAODWaveformProfile :
    ObservableCacheBase,
    IAdaptIn<AbstractAODWaveformProfile, AbstractAODWaveformProfile>
{
    public OpticsAODElectrodeEnum OpticsAODElectrodeEnum
    {
        get;
        internal set => SetProperty(ref field, value);
    }

    public string FilePath
    {
        get;
        internal set
        {
            if (SetProperty(ref field, value)) OnFilePathChanged(value);
        }
    } = string.Empty;

    public int ZeroSampleCount
    {
        get;
        internal set
        {
            if (SetProperty(ref field, value)) OnPropertyChanged(nameof(TotalSampleCount));
        }
    }

    public double OffsetFrequency
    {
        get;
        internal set => SetProperty(ref field, value);
    }

    public double OffsetFrequencyPeriodCoefficient
    {
        get;
        internal set => SetProperty(ref field, value);
    }

    public int TotalSampleCount => Shorts.Count + ZeroSampleCount;

    [Newtonsoft.Json.JsonIgnore]
    [System.Text.Json.Serialization.JsonIgnore]
    [System.Xml.Serialization.XmlIgnore]
    [LiteDB.BsonIgnore]
    public IReadOnlyList<short> Shorts
    {
        get;
        private set
        {
            if (SetProperty(ref field, value)) OnPropertyChanged(nameof(TotalSampleCount));
        }
    } = [];

    [Newtonsoft.Json.JsonIgnore]
    [System.Text.Json.Serialization.JsonIgnore]
    [System.Xml.Serialization.XmlIgnore]
    [LiteDB.BsonIgnore]
    public IReadOnlyList<byte> Bytes
    {
        get;
        private set
        {
            if (SetProperty(ref field, value)) OnPropertyChanged(nameof(TotalSampleCount));
        }
    } = [];

    #region 波形

    /// <inheritdoc cref="AODWaveformGenerator.AODWaveformResultItem.Signals"/>
    [Newtonsoft.Json.JsonIgnore]
    [System.Text.Json.Serialization.JsonIgnore]
    [System.Xml.Serialization.XmlIgnore]
    [LiteDB.BsonIgnore]
    public IReadOnlyList<Point> Signals { get; internal set; } = [];

    /// <inheritdoc cref="AODWaveformGenerator.AODWaveformResultItem.FFTSignals"/>
    [Newtonsoft.Json.JsonIgnore]
    [System.Text.Json.Serialization.JsonIgnore]
    [System.Xml.Serialization.XmlIgnore]
    [LiteDB.BsonIgnore]
    public IReadOnlyList<Point> FFTSignals { get; internal set; } = [];

    /// <inheritdoc cref="AODWaveformGenerator.AODWaveformResultItem.FrequencyCoefficients"/>
    [Newtonsoft.Json.JsonIgnore]
    [System.Text.Json.Serialization.JsonIgnore]
    [System.Xml.Serialization.XmlIgnore]
    [LiteDB.BsonIgnore]
    public IReadOnlyList<Point> FrequencyCoefficients { get; internal set; } = [];

    /// <inheritdoc cref="AODWaveformGenerator.AODWaveformResultItem.FlatnessLinearFrequencySignals"/>
    [Newtonsoft.Json.JsonIgnore]
    [System.Text.Json.Serialization.JsonIgnore]
    [System.Xml.Serialization.XmlIgnore]
    [LiteDB.BsonIgnore]
    public IReadOnlyList<Point> FlatnessLinearFrequencySignals { get; internal set; } = [];

    /// <inheritdoc cref="AODWaveformGenerator.AODWaveformResultItem.FlatnessTotalFrequencySignals"/>
    [Newtonsoft.Json.JsonIgnore]
    [System.Text.Json.Serialization.JsonIgnore]
    [System.Xml.Serialization.XmlIgnore]
    [LiteDB.BsonIgnore]
    public IReadOnlyList<Point> FlatnessTotalFrequencySignals { get; internal set; } = [];

    /// <inheritdoc cref="AODWaveformGenerator.AODWaveformResultItem.FlatnessAstigmatismCompensationSignals"/>
    [Newtonsoft.Json.JsonIgnore]
    [System.Text.Json.Serialization.JsonIgnore]
    [System.Xml.Serialization.XmlIgnore]
    [LiteDB.BsonIgnore]
    public IReadOnlyList<Point> FlatnessAstigmatismCompensationSignals { get; internal set; } = [];

    /// <inheritdoc cref="AODWaveformGenerator.AODWaveformResultItem.FlatnessSphericalAberrationCompensationSignals"/>
    [Newtonsoft.Json.JsonIgnore]
    [System.Text.Json.Serialization.JsonIgnore]
    [System.Xml.Serialization.XmlIgnore]
    [LiteDB.BsonIgnore]
    public IReadOnlyList<Point> FlatnessSphericalAberrationCompensationSignals { get; internal set; } = [];

    /// <inheritdoc cref="AODWaveformGenerator.AODWaveformResultItem.FlatnessSecondaryAstigmatismCompensationSignals"/>
    [Newtonsoft.Json.JsonIgnore]
    [System.Text.Json.Serialization.JsonIgnore]
    [System.Xml.Serialization.XmlIgnore]
    [LiteDB.BsonIgnore]
    public IReadOnlyList<Point> FlatnessSecondaryAstigmatismCompensationSignals { get; internal set; } = [];

    /// <inheritdoc cref="AODWaveformGenerator.AODWaveformResultItem.FlatnessComaCompensationSignals"/>
    [Newtonsoft.Json.JsonIgnore]
    [System.Text.Json.Serialization.JsonIgnore]
    [System.Xml.Serialization.XmlIgnore]
    [LiteDB.BsonIgnore]
    public IReadOnlyList<Point> FlatnessComaCompensationSignals { get; internal set; } = [];

    /// <inheritdoc cref="AODWaveformGenerator.AODWaveformResultItem.FlatnessTrefoilCompensationSignals"/>
    [Newtonsoft.Json.JsonIgnore]
    [System.Text.Json.Serialization.JsonIgnore]
    [System.Xml.Serialization.XmlIgnore]
    [LiteDB.BsonIgnore]
    public IReadOnlyList<Point> FlatnessTrefoilCompensationSignals { get; internal set; } = [];

    /// <inheritdoc cref="AODWaveformGenerator.AODWaveformResultItem.FlatnessQuadrafoilCompensationSignals"/>
    [Newtonsoft.Json.JsonIgnore]
    [System.Text.Json.Serialization.JsonIgnore]
    [System.Xml.Serialization.XmlIgnore]
    [LiteDB.BsonIgnore]
    public IReadOnlyList<Point> FlatnessQuadrafoilCompensationSignals { get; internal set; } = [];

    /// <inheritdoc cref="AODWaveformGenerator.AODWaveformResultItem.FlatnessAlphaOrderCompensationSignals"/>
    [Newtonsoft.Json.JsonIgnore]
    [System.Text.Json.Serialization.JsonIgnore]
    [System.Xml.Serialization.XmlIgnore]
    [LiteDB.BsonIgnore]
    public IReadOnlyList<Point> FlatnessAlphaOrderCompensationSignals { get; internal set; } = [];

    #endregion 波形

    private void OnFilePathChanged(string value)
    {
        if (File.Exists(value) == false) return;

        // $总byte长度$补零个数$包分割长度$下发寄存器号(02prescan, 03chirp)$偏移的频率$偏移的频率的2π周期的倍率$
        var strings = value.Split('$');
        Guard.IsTrue(strings.Length >= 7, "filePath name error.");

        ZeroSampleCount = int.Parse(strings[2]);
        OffsetFrequency = double.Parse(strings[5]);
        OffsetFrequencyPeriodCoefficient = double.Parse(strings[6]);

        var resultString = File.ReadAllLines(value)
            .Select(t => t.Trim())
            .Where(t => string.IsNullOrWhiteSpace(t) == false)
            .ToList();

        if (resultString.Count <= 0 && resultString.All(t => t.Length == 4) == false) ThrowHelper.ThrowNotSupportedException("filePath value error.");

        Shorts = [.. resultString.Select(str => Convert.ToInt16(str, 16))];

        SetByteList(1);
    }

    protected void SetByteList(double coefficient) => SetByteList([.. Enumerable.Repeat(coefficient, Shorts.Count)]);

    protected void SetByteList(IReadOnlyList<double> coefficientWindowList)
    {
        foreach (var coefficient in coefficientWindowList) Guard.IsTrue(coefficient >= 0, "coefficient is muse be >= 0.");
        Guard.IsTrue(Shorts.Count == coefficientWindowList.Count, "Count is not equal.");

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
        var signals = new List<Point>();

        foreach (var (index, value) in Shorts
                     .Index()
                     .Select(t => (short)Math.Round(t.Item * coefficientWindowList[t.Index], MidpointRounding.AwayFromZero))
                     .Index())
        {
            signals[index] = new Point(index, value / Math.Pow(2d, 15d));

            var bytes = BitConverter.GetBytes(value);

            result.Add(bytes[1]);
            result.Add(bytes[0]);
        }

        Signals = signals;
        Bytes = result;
    }

    protected string Save(string directoryPath)
    {
        // $总byte长度$补零个数$包分割长度$下发寄存器号(02prescan, 03chirp)$偏移的频率$偏移的频率的2π周期的倍率$
        var strings = FilePath.Split('$');
        Guard.IsTrue(strings.Length >= 7, "filePath name error.");

        var registerId = strings[4];
        var filePath = Path.Combine(directoryPath, EnumHelper.ToDescriptionString(OpticsAODElectrodeEnum), $"{Guid.NewGuid():N}${TotalSampleCount}${ZeroSampleCount}$600${registerId}${OffsetFrequency:0.###}${OffsetFrequencyPeriodCoefficient:0.###}$.txt");
        FileHelper.DeleteFileIfExists(filePath);
        DirectoryHelper.CreateFileDirectoryIfNotExists(filePath);

        Guard.IsTrue(Shorts.Count * 2 == Bytes.Count, "Count is not equal.");

        var stringBuilder = new StringBuilder();
        for (var i = 0; i < Bytes.Count; i += 2)
        {
            stringBuilder.AppendFormat("{0:X2}{1:X2}", Bytes[i], Bytes[i + 1]);
            stringBuilder.AppendLine();
        }

        File.WriteAllText(filePath, stringBuilder.ToString());

        return filePath;
    }

    public AbstractAODWaveformProfile AdaptIn(AbstractAODWaveformProfile obj)
    {
        OpticsAODElectrodeEnum = obj.OpticsAODElectrodeEnum;
        FilePath = obj.FilePath;
        ZeroSampleCount = obj.ZeroSampleCount;
        OffsetFrequency = obj.OffsetFrequency;
        OffsetFrequencyPeriodCoefficient = obj.OffsetFrequencyPeriodCoefficient;
        Shorts = [.. obj.Shorts];
        Bytes = [.. obj.Bytes];

        Signals = [.. obj.Signals];
        FFTSignals = [.. obj.FFTSignals];
        FrequencyCoefficients = [.. obj.FrequencyCoefficients];
        FlatnessLinearFrequencySignals = [.. obj.FlatnessLinearFrequencySignals];
        FlatnessTotalFrequencySignals = [.. obj.FlatnessTotalFrequencySignals];
        FlatnessAstigmatismCompensationSignals = [.. obj.FlatnessAstigmatismCompensationSignals];
        FlatnessSphericalAberrationCompensationSignals = [.. obj.FlatnessSphericalAberrationCompensationSignals];
        FlatnessSecondaryAstigmatismCompensationSignals = [.. obj.FlatnessSecondaryAstigmatismCompensationSignals];
        FlatnessComaCompensationSignals = [.. obj.FlatnessComaCompensationSignals];
        FlatnessTrefoilCompensationSignals = [.. obj.FlatnessTrefoilCompensationSignals];
        FlatnessQuadrafoilCompensationSignals = [.. obj.FlatnessQuadrafoilCompensationSignals];
        FlatnessAlphaOrderCompensationSignals = [.. obj.FlatnessAlphaOrderCompensationSignals];

        return this;
    }

    public virtual object ToFlatnessHtmlAnonymous() => new
    {
        OpticsAODElectrodeEnum,
        FilePath,
        ZeroSampleCount,
        OffsetFrequency,
        OffsetFrequencyPeriodCoefficient,
        Plot = new HtmlTab(new
        {
            TimeDomainSignal = new HtmlPlot2DLinesChart([(string.Empty, [.. Signals])], string.Empty),
            SpectrumFFTAnalysis = new HtmlPlot2DLinesChart([(string.Empty, [.. FFTSignals])], string.Empty)
        })
    };

    public virtual object ToHtmlAnonymous() => new
    {
        OpticsAODElectrodeEnum,
        FilePath,
        ZeroSampleCount,
        OffsetFrequency,
        OffsetFrequencyPeriodCoefficient,
        Plot = new HtmlTab(new
        {
            TimeDomainSignal = new HtmlPlot2DLinesChart([(string.Empty, [.. Signals])], string.Empty),
            SpectrumFFTAnalysis = new HtmlPlot2DLinesChart([(string.Empty, [.. FFTSignals])], string.Empty),
            DynamicFrequencyCoefficient = new HtmlPlot2DLinesChart([(string.Empty, [.. FrequencyCoefficients])], string.Empty),
            FlatnessFrequency = new HtmlPlot2DLinesChart([
                (nameof(FlatnessLinearFrequencySignals), [.. FlatnessLinearFrequencySignals]),
                (nameof(FlatnessAstigmatismCompensationSignals), [.. FlatnessAstigmatismCompensationSignals]),
                (nameof(FlatnessSphericalAberrationCompensationSignals), [.. FlatnessSphericalAberrationCompensationSignals]),
                (nameof(FlatnessSecondaryAstigmatismCompensationSignals), [.. FlatnessSecondaryAstigmatismCompensationSignals]),
                (nameof(FlatnessComaCompensationSignals), [.. FlatnessComaCompensationSignals]),
                (nameof(FlatnessTrefoilCompensationSignals), [.. FlatnessTrefoilCompensationSignals]),
                (nameof(FlatnessQuadrafoilCompensationSignals), [.. FlatnessQuadrafoilCompensationSignals]),
                (nameof(FlatnessAlphaOrderCompensationSignals), [.. FlatnessAlphaOrderCompensationSignals])
            ], string.Empty),
            TotalFlatnessFrequency = new HtmlPlot2DLinesChart([(string.Empty, [.. FlatnessTotalFrequencySignals])], string.Empty)
        })
    };
}