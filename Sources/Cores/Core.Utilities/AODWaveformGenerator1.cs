using CommunityToolkit.Diagnostics;
using MathNet.Numerics;
using MathNet.Numerics.Interpolation;
using MathNet.Numerics.LinearAlgebra;
using Net.Utilities.Algorithms.Extensions;
using Net.Utilities.Helpers.Extensions;
using Net.Utilities.Helpers.Helpers.Files;
using Net.Utilities.Models.Enums.Maths;
using Net.Utilities.Models.Geometries;
using Complex = System.Numerics.Complex;
using Constants = Net.Utilities.Models.Constants;

// ReSharper disable once CheckNamespace
namespace Net.Utilities.Algorithms.Modules;

public static class AODWaveformGenerator1
{
    public const string ChirpAODWaveformFileExtension = ".caw";
    public const string PrescanAODWaveformFileExtension = ".paw";

    #region 参数

    /// <summary>
    /// AOD波形的频率偏移配置
    /// </summary>
    /// <param name="DirectoryName">文件夹名称</param>
    /// <param name="OffsetFrequency">偏移频率(Mhz)</param>
    /// <param name="OffsetFrequencyPeriodCoefficient">偏移频率的2π周期的系数</param>
    /// <param name="Amplitude">幅值</param>
    /// <param name="IsGenerateAODWaveformZero">生成的波形是否都为0</param>
    public sealed record AODWaveformOffsetConfiguration(string DirectoryName, double OffsetFrequency, double OffsetFrequencyPeriodCoefficient, double Amplitude, bool IsGenerateAODWaveformZero = false)
    {
        /// <summary>
        /// AOD波形频的率均匀性配置集合
        /// </summary>
        public IReadOnlyList<AODWaveformUniformityConfiguration> UniformityConfigurations { get; init; } = [];

        public void Validate()
        {
            Guard.IsNotNullOrWhiteSpace(DirectoryName, nameof(AODWaveformOffsetConfiguration) + nameof(DirectoryName));
            Guard.IsGreaterThanOrEqualTo(OffsetFrequency, 0d, nameof(AODWaveformOffsetConfiguration) + nameof(OffsetFrequency));
            Guard.IsBetweenOrEqualTo(Amplitude, 0d, 1d);

            foreach (var item in UniformityConfigurations) item.Validate();

            Guard.IsTrue(UniformityConfigurations.Select(t => t.Frequency).IsIncreasing(true), "Uniformity Configurations must be sorted by Frequency");
        }
    }

    /// <summary>
    /// AOD波形频率均匀性配置
    /// </summary>
    /// <param name="Frequency">频率</param>
    /// <param name="Coefficient">均匀性</param>
    public sealed record AODWaveformUniformityConfiguration(double Frequency, double Coefficient)
    {
        public void Validate()
        {
            Guard.IsGreaterThan(Frequency, 0d, nameof(AODWaveformUniformityConfiguration) + nameof(Frequency));
            Guard.IsGreaterThan(Coefficient, 0d, nameof(AODWaveformUniformityConfiguration) + nameof(Coefficient));
            Guard.IsBetweenOrEqualTo(Coefficient, 0d, 1d, nameof(AODWaveformUniformityConfiguration) + nameof(Coefficient));
        }

        internal Point ToPoint() => new(Frequency, Coefficient);
    }

    /// <summary>
    /// 斜率变化率分段的配置项
    /// </summary>
    /// <param name="DeltaKRate">每一段的频率变换率的百分比</param>
    /// <param name="Coefficient">均匀性</param>
    public sealed record AODWaveformSlopeConfiguration(double DeltaKRate, double Coefficient);

    /// <inheritdoc cref="AbstractAODWaveformParam"/>
    /// <remarks>
    /// PrescanAOD波形生成参数
    /// </remarks>
    public sealed record PrescanAODWaveformParam(double FlatnessTime) : AbstractAODWaveformParam(FlatnessTime);

    /// <inheritdoc cref="AbstractAODWaveformParam"/>
    /// <remarks>
    /// ChirpAOD波形生成参数
    /// </remarks>
    /// <code>
    /// <see cref="AbstractAODWaveformParam.FlatnessTime"/> = <see cref="SoundPacketLength"/>/<see cref="SoundSpeed"/>; // (ns): mm/(mm/us) * 1000 = us * 1000 = ns
    /// </code>
    /// <param name="SoundPacketLength">音包长度(mm)</param>
    /// <param name="SoundSpeed">音速(mm/us)，固体声速更快 (默认: 5.742 mm/us)</param>
    public sealed record ChirpAODWaveformParam(double SoundPacketLength, double SoundSpeed)
        : AbstractAODWaveformParam(SoundSpeed > 0d ? Math.Round(SoundPacketLength / SoundSpeed * 1000d, MidpointRounding.AwayFromZero) : -1d)
    {
        internal override void Validate()
        {
            Guard.IsGreaterThan(SoundPacketLength, 0d);
            Guard.IsGreaterThan(SoundSpeed, 0d);

            base.Validate();
        }
    }

    /// <summary>
    /// AOD波形生成参数 [中心频率 - 带宽/2, 中心频率 + 带宽/2]
    /// </summary>
    /// <param name="FlatnessTime">平坦时间(ns)</param>
    public abstract record AbstractAODWaveformParam(double FlatnessTime)
    {
        /// <summary>
        /// 带宽(MHz)
        /// </summary>
        public double BandWidth { get; init; }

        /// <summary>
        /// 中心频率(Mhz)
        /// </summary>
        public double CenterFrequency { get; init; }

        /// <summary>
        /// 递增, 递减, 平坦
        /// </summary>
        public FunctionMonotonicTypeEnum FunctionMonotonicTypeEnum { get; init; }

        /// <summary>
        /// 采样率(Msa/s)
        /// </summary>
        public double SampleRate { get; init; }

        /// <summary>
        /// 生成的目录
        /// </summary>
        public string DirectoryPath { get; init; } = string.Empty;

        /// <summary>
        /// 包含在波形文件名中的可选标识
        /// </summary>
        public string FileNameSuffix { get; init; } = string.Empty;

        /// <summary>
        /// 前面添加多少补零采样点个数, 相当于添加延迟(sa)
        /// </summary>
        public int ZeroSampleCount { get; init; }

        /// <summary>
        /// 端点头尾添加多少采样点个数, 缓冲(XTC响应不够)(sa)
        /// </summary>
        public int EndpointSampleCount { get; init; }

        /// <summary>
        /// 生成多个AOD波形中每个波形的频率偏移配置集合
        /// </summary>
        public IReadOnlyList<AODWaveformOffsetConfiguration> OffsetConfigurations { get; init; } = [];

        /// <summary>
        /// AOD波形的斜率变化率分段的配置项集合
        /// </summary>
        public IReadOnlyList<AODWaveformSlopeConfiguration> SlopeConfigurations { get; init; } = [];

        /// <summary>
        /// 低频
        /// </summary>
        public double LowFrequency => FunctionMonotonicTypeEnum switch
        {
            FunctionMonotonicTypeEnum.Increasing or FunctionMonotonicTypeEnum.Deceasing => CenterFrequency - BandWidth / 2d,
            FunctionMonotonicTypeEnum.Flatness => CenterFrequency,
            _ => ThrowHelper.ThrowArgumentOutOfRangeException<double>(nameof(FunctionMonotonicTypeEnum))
        };

        /// <summary>
        /// 高频
        /// </summary>
        public double HighFrequency => FunctionMonotonicTypeEnum switch
        {
            FunctionMonotonicTypeEnum.Increasing or FunctionMonotonicTypeEnum.Deceasing => CenterFrequency + BandWidth / 2d,
            FunctionMonotonicTypeEnum.Flatness => CenterFrequency,
            _ => ThrowHelper.ThrowArgumentOutOfRangeException<double>(nameof(FunctionMonotonicTypeEnum))
        };

        /// <summary>
        /// 总采样点的个数: ns * (Msa/s) / 1000 = ns * (Gsa/s) = (10^-9s)*(10^9sa/s) = sa
        /// </summary>
        public int NumberOfSamples => (int)Math.Round(FlatnessTime * SampleRate / 1000d + 2d * EndpointSampleCount, MidpointRounding.AwayFromZero);

        internal virtual void Validate()
        {
            Guard.IsGreaterThanOrEqualTo(BandWidth, 0d);
            Guard.IsGreaterThan(CenterFrequency, 0d);
            Guard.IsGreaterThan(FlatnessTime, 0d);

            Guard.IsGreaterThan(SampleRate, 0d);
            Guard.IsNotNullOrWhiteSpace(DirectoryPath);
            Guard.IsGreaterThanOrEqualTo(ZeroSampleCount, 0d);
            Guard.IsGreaterThanOrEqualTo(EndpointSampleCount, 0d);

            Guard.IsNotEmpty(OffsetConfigurations, "Offset Configuration is must be not empty.");

            foreach (var item in OffsetConfigurations)
            {
                item.Validate();
            }

            if (SlopeConfigurations.Count > 0)
            {
                Guard.IsTrue((SlopeConfigurations.Count & 1) == 1, nameof(SlopeConfigurations), "Slope Configurations count must be odd.");
            }

            if (FunctionMonotonicTypeEnum is FunctionMonotonicTypeEnum.Flatness)
            {
                if (BandWidth != 0d) ThrowHelper.ThrowArgumentException(nameof(BandWidth), "if monotonic is flatness then band Width is must 0");
            }
            else
            {
                if (BandWidth == 0d) ThrowHelper.ThrowArgumentException(nameof(BandWidth), "if monotonic is not flatness then band Width is must > 0");
            }
        }
    }

    #endregion 参数

    #region 结果

    /// <inheritdoc cref="AbstractAODWaveformResult{T}"/>
    /// <remarks>
    /// PrescanAOD波形结果
    /// </remarks>
    /// <param name="Param">PrescanAOD波形生成参数</param>
    public sealed record PrescanAODWaveformResult(PrescanAODWaveformParam Param) : AbstractAODWaveformResult<PrescanAODWaveformParam>(Param)
    {
        internal override void Initialize()
        {
            const string prescan = nameof(prescan);

            var fileName = prescan +
                           $"_{Param.FileNameSuffix}" +
                           $"_{Param.FlatnessTime:0.###}ns" +
                           $"_{Param.FunctionMonotonicTypeEnum switch
                           {
                               FunctionMonotonicTypeEnum.Increasing => $"{Param.LowFrequency:0.###}Mhz_{Param.HighFrequency:0.###}Mhz",
                               FunctionMonotonicTypeEnum.Deceasing => $"{Param.HighFrequency:0.###}Mhz_{Param.LowFrequency:0.###}Mhz",
                               FunctionMonotonicTypeEnum.Flatness => $"{Param.CenterFrequency:0.###}Mhz_{Param.CenterFrequency:0.###}Mhz",
                               _ => ThrowHelper.ThrowArgumentOutOfRangeException<string>(nameof(FunctionMonotonicTypeEnum))
                           }}" +
                           $"{PrescanAODWaveformFileExtension}";

            FilePath = FileHelper.GetEnsureLongPathSupport(Path.Combine(Param.DirectoryPath, prescan + Id, FileHelper.RemoveInvalidFileName(fileName)));

            var itemList = new List<AODWaveformResultItem>();
            foreach (var item in Param.OffsetConfigurations)
            {
                fileName = prescan +
                           $"_{Param.FileNameSuffix}" +
                           $"${Param.NumberOfSamples + Param.ZeroSampleCount}${Param.ZeroSampleCount}$600$02${item.OffsetFrequency:0.###}${item.OffsetFrequencyPeriodCoefficient:0.###}$.txt";

                itemList.Add(new AODWaveformResultItem(item, FileHelper.GetEnsureLongPathSupport(Path.Combine(Param.DirectoryPath, prescan + Id, FileHelper.RemoveInvalidFileName(item.DirectoryName), FileHelper.RemoveInvalidFileName(fileName)))));
            }

            Items = itemList;
        }
    }

    /// <inheritdoc cref="AbstractAODWaveformResult{T}"/>
    /// <remarks>
    /// ChirpAOD波形结果
    /// </remarks>
    /// <param name="Param">ChirpAOD波形生成参数</param>
    public sealed record ChirpAODWaveformResult(ChirpAODWaveformParam Param) : AbstractAODWaveformResult<ChirpAODWaveformParam>(Param)
    {
        internal override void Initialize()
        {
            const string chirp = nameof(chirp);

            var fileName = chirp +
                           $"_{Param.FileNameSuffix}" +
                           $"_{Param.SoundPacketLength:0.###}mm" +
                           $"_{Param.FunctionMonotonicTypeEnum switch
                           {
                               FunctionMonotonicTypeEnum.Increasing => $"{Param.LowFrequency:0.###}Mhz_{Param.HighFrequency:0.###}Mhz",
                               FunctionMonotonicTypeEnum.Deceasing => $"{Param.HighFrequency:0.###}Mhz_{Param.LowFrequency:0.###}Mhz",
                               FunctionMonotonicTypeEnum.Flatness => $"{Param.CenterFrequency:0.###}Mhz_{Param.CenterFrequency:0.###}Mhz",
                               _ => ThrowHelper.ThrowArgumentOutOfRangeException<string>(nameof(FunctionMonotonicTypeEnum))
                           }}" +
                           $"{ChirpAODWaveformFileExtension}";

            FilePath = FileHelper.GetEnsureLongPathSupport(Path.Combine(Param.DirectoryPath, chirp + Id, FileHelper.RemoveInvalidFileName(fileName)));

            var itemList = new List<AODWaveformResultItem>();
            foreach (var item in Param.OffsetConfigurations)
            {
                fileName = chirp +
                           $"_{Param.FileNameSuffix}" +
                           $"${Param.NumberOfSamples + Param.ZeroSampleCount}${Param.ZeroSampleCount}$600$03${item.OffsetFrequency:0.###}${item.OffsetFrequencyPeriodCoefficient:0.###}$.txt";

                itemList.Add(new AODWaveformResultItem(item, FileHelper.GetEnsureLongPathSupport(Path.Combine(Param.DirectoryPath, chirp + Id, FileHelper.RemoveInvalidFileName(item.DirectoryName), FileHelper.RemoveInvalidFileName(fileName)))));
            }

            Items = itemList;
        }
    }

    /// <summary>
    /// AOD波形结果
    /// </summary>
    /// <param name="Param">AOD波形生成参数</param>
    public abstract record AbstractAODWaveformResult<TParam>(TParam Param) where TParam : AbstractAODWaveformParam
    {
        protected readonly string Id = DateTime.Now.ToString(Constants.LongFileDateTimeFormat);

        /// <summary>
        /// AOD波形结果项集合
        /// </summary>
        public List<AODWaveformResultItem> Items { get; protected set; } = [];

        /// <summary>
        /// AOD波形结果文件
        /// </summary>
        public string FilePath { get; protected set; } = string.Empty;

        internal abstract void Initialize();
    }

    /// <summary>
    /// AOD波形结果项
    /// </summary>
    /// <param name="OffsetConfiguration">AOD波形的频率偏移配置</param>
    /// <param name="FilePath">AOD波形文件</param>
    public sealed record AODWaveformResultItem(AODWaveformOffsetConfiguration OffsetConfiguration, string FilePath)
    {
        /// <summary>
        /// AOD波形信号
        ///</summary>
        [Newtonsoft.Json.JsonIgnore]
        [System.Text.Json.Serialization.JsonIgnore]
        [System.Xml.Serialization.XmlIgnore]
        public IReadOnlyList<Point> Signals { get; internal set; } = [];

        /// <summary>
        /// AOD波形信号FFT分析
        ///</summary>
        [Newtonsoft.Json.JsonIgnore]
        [System.Text.Json.Serialization.JsonIgnore]
        [System.Xml.Serialization.XmlIgnore]
        public IReadOnlyList<Point> FFTSignals { get; internal set; } = [];

        /// <summary>
        /// AOD波形频率补偿信号
        ///</summary>
        [Newtonsoft.Json.JsonIgnore]
        [System.Text.Json.Serialization.JsonIgnore]
        [System.Xml.Serialization.XmlIgnore]
        public IReadOnlyList<Point> FrequencyCoefficients { get; internal set; } = [];

        /// <summary>
        /// AOD波形线性频率信号
        ///</summary>
        [Newtonsoft.Json.JsonIgnore]
        [System.Text.Json.Serialization.JsonIgnore]
        [System.Xml.Serialization.XmlIgnore]
        public IReadOnlyList<Point> FlatnessFrequencySignals { get; internal set; } = [];

        /// <summary>
        /// AOD波形线性相位信号
        ///</summary>
        [Newtonsoft.Json.JsonIgnore]
        [System.Text.Json.Serialization.JsonIgnore]
        [System.Xml.Serialization.XmlIgnore]
        public IReadOnlyList<Point> FlatnessPhaseSignals { get; internal set; } = [];
    }

    #endregion 结果

    /// <inheritdoc cref="GenerateAODWaveform{TResult,TParam}"/>
    /// <remarks>
    /// 生成PrescanAOD波形
    /// </remarks>
    public static PrescanAODWaveformResult GeneratePrescanAODWaveform(PrescanAODWaveformParam param, CancellationToken cancellationToken) => GenerateAODWaveform<PrescanAODWaveformResult, PrescanAODWaveformParam>(param, cancellationToken);

    /// <inheritdoc cref="GenerateAODWaveform{TResult,TParam}"/>
    /// <remarks>
    /// 生成ChirpAOD波形
    /// </remarks>
    public static ChirpAODWaveformResult GenerateChirpAODWaveform(ChirpAODWaveformParam param, CancellationToken cancellationToken) => GenerateAODWaveform<ChirpAODWaveformResult, ChirpAODWaveformParam>(param, cancellationToken);

    /// <summary>
    /// 生成AOD波形
    /// </summary>
    /// <param name="param">AOD波形生成参数</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>结果</returns>
    private static TResult GenerateAODWaveform<TResult, TParam>(TParam param, CancellationToken cancellationToken)
        where TResult : AbstractAODWaveformResult<TParam>
        where TParam : AbstractAODWaveformParam
    {
        param.Validate();

        var centerFrequency = param.CenterFrequency;
        var lowFrequency = param.LowFrequency;
        var highFrequency = param.HighFrequency;

        var headerFrequency = param.FunctionMonotonicTypeEnum switch
        {
            FunctionMonotonicTypeEnum.Increasing => lowFrequency,
            FunctionMonotonicTypeEnum.Deceasing => highFrequency,
            FunctionMonotonicTypeEnum.Flatness => centerFrequency,
            _ => ThrowHelper.ThrowArgumentOutOfRangeException<double>(nameof(param.FunctionMonotonicTypeEnum))
        };

        var footerFrequency = param.FunctionMonotonicTypeEnum switch
        {
            FunctionMonotonicTypeEnum.Increasing => highFrequency,
            FunctionMonotonicTypeEnum.Deceasing => lowFrequency,
            FunctionMonotonicTypeEnum.Flatness => centerFrequency,
            _ => ThrowHelper.ThrowArgumentOutOfRangeException<double>(nameof(param.FunctionMonotonicTypeEnum))
        };

        #region 返回结果

        var dt = 1d / param.SampleRate; // 每个采样点的时间间隔 (us/sa): 1 / (Msa/s) = 10^-6s/sa = us/sa

        var allSampleIndices = Generate.LinearRangeInt32(0, param.NumberOfSamples - 1);
        var headerSampleIndices = Generate.LinearRangeInt32(0, param.EndpointSampleCount - 1);
        var flatnessSampleIndices = Generate.LinearRangeInt32(param.EndpointSampleCount, param.NumberOfSamples - param.EndpointSampleCount - 1);
        var footerSampleIndices = Generate.LinearRangeInt32(param.NumberOfSamples - param.EndpointSampleCount, param.NumberOfSamples - 1);

        var t = (Vector<double>.Build.DenseOfArray(flatnessSampleIndices) - flatnessSampleIndices[0]) * dt; // us

        var aodWaveformSignals = Vector<double>.Build.Dense(param.NumberOfSamples);

        Vector<Complex> fftResult;
        Vector<double> fftFrequencies;
        Vector<double> fftMagnitudes;

        #endregion 返回结果

        var result = Guard.IsNotNullAndAssignableToTypeAndReturn<TResult>(Activator.CreateInstance(typeof(TResult), param));
        result.Initialize();

        foreach (var item in result.Items)
        {
            cancellationToken.ThrowIfCancellationRequested();

            var (flatnessFrequencies, flatnessUniformities, flatnessPhases) = FFT(item.OffsetConfiguration.Amplitude, item.OffsetConfiguration.OffsetFrequency, item.OffsetConfiguration.OffsetFrequencyPeriodCoefficient);

            var frequencyCoefficientList = new List<Point>();

            if (param.FunctionMonotonicTypeEnum != FunctionMonotonicTypeEnum.Flatness)
            {
                var linearSpline = item.OffsetConfiguration.UniformityConfigurations.Count > 0
                    ? LinearSpline.InterpolateSorted(
                        [.. item.OffsetConfiguration.UniformityConfigurations.Select(configuration => configuration.Frequency)],
                        [.. item.OffsetConfiguration.UniformityConfigurations.Select(configuration => configuration.Coefficient)])
                    : null;

                var flatnessAODWaveformSignals = aodWaveformSignals.SubVectorRange(flatnessSampleIndices[0], flatnessSampleIndices[^1]);
                for (var i = 0; i < flatnessAODWaveformSignals.Count; i++)
                {
                    var frequency = flatnessFrequencies[i];
                    var coefficient = (linearSpline?.Interpolate(frequency) ?? 1d) * flatnessUniformities[i];

                    flatnessAODWaveformSignals[i] *= coefficient;

                    frequencyCoefficientList.Add(new Point(frequency, coefficient));

                    if (i == 0)
                    {
                        if (headerSampleIndices.Length > 0)
                        {
                            var headerAODWaveformSignals = aodWaveformSignals.SubVectorRange(headerSampleIndices[0], headerSampleIndices[^1]);

                            aodWaveformSignals.SetSubVectorRange(headerSampleIndices[0], headerSampleIndices[^1], headerAODWaveformSignals * coefficient);
                        }
                    }

                    if (i == flatnessAODWaveformSignals.Count - 1)
                    {
                        if (headerSampleIndices.Length > 0)
                        {
                            var footerAODWaveformSignals = aodWaveformSignals.SubVectorRange(footerSampleIndices[0], footerSampleIndices[^1]);

                            aodWaveformSignals.SetSubVectorRange(footerSampleIndices[0], footerSampleIndices[^1], footerAODWaveformSignals * coefficient);
                        }
                    }
                }

                aodWaveformSignals.SetSubVectorRange(flatnessSampleIndices[0], flatnessSampleIndices[^1], flatnessAODWaveformSignals);

                fftResult = flatnessAODWaveformSignals.ToComplex().FastFourierTransform();
                (fftFrequencies, fftMagnitudes) = fftResult.GetPositiveFrequencies(param.SampleRate);
            }

            /*
             * double[-1,1]归一化数据需要转换为16-bit整数格式进行传输[DSP、FPGA、DAC数字信号转换为模拟信号]
             * 16-bit PCM(脉冲编码调制)格式: Int16 范围 [-32768, 32767]
             *
             * 归一化映射:
             *   -1.0 → -32768 (0x8000) Math.Pow(2d, 15d) - 1
             *    0.0 → 0      (0x0000)
             *   +1.0 → +32767 (0x7FFF) -Math.Pow(2d, 15d)
             */

            // 将结果转换为16位整数并保存到文件
            var hexStrings = item.OffsetConfiguration.IsGenerateAODWaveformZero
                ? (string[])[.. Enumerable.Repeat(((short)0).ToString("x4"), aodWaveformSignals.Count)]
                : [.. aodWaveformSignals.Select(y => ((short)Math.Clamp(Math.Round(y * Math.Pow(2d, 15d), MidpointRounding.AwayFromZero), short.MinValue, short.MaxValue)).ToString("x4"))];

            DirectoryHelper.CreateFileDirectoryIfNotExists(item.FilePath);
            FileHelper.DeleteFileIfExists(item.FilePath);
            File.WriteAllText(item.FilePath, string.Join(Environment.NewLine, hexStrings));

            item.Signals = item.OffsetConfiguration.IsGenerateAODWaveformZero
                ? (Point[])[.. allSampleIndices.Index().Select(tuple => new Point(tuple.Item, 0d))]
                : [.. allSampleIndices.Index().Select(tuple => new Point(tuple.Item, aodWaveformSignals[tuple.Index]))];
            item.FFTSignals = item.OffsetConfiguration.IsGenerateAODWaveformZero
                ? (Point[])[.. fftFrequencies.Zip(fftMagnitudes, (x, _) => new Point(x, 0))]
                : [.. fftFrequencies.Zip(fftMagnitudes, (x, y) => new Point(x, y))];
            item.FrequencyCoefficients = item.OffsetConfiguration.IsGenerateAODWaveformZero
                ? (Point[])[.. frequencyCoefficientList.Select(point => new Point(point.X, 0d))]
                : [.. frequencyCoefficientList];

            item.FlatnessFrequencySignals = item.OffsetConfiguration.IsGenerateAODWaveformZero
                ? (Point[])[.. flatnessSampleIndices.Index().Select(tuple => new Point(tuple.Item, 0d))]
                : [.. flatnessSampleIndices.Index().Select(tuple => new Point(tuple.Item, flatnessFrequencies[tuple.Index]))];
            item.FlatnessPhaseSignals = item.OffsetConfiguration.IsGenerateAODWaveformZero
                ? (Point[])[.. flatnessSampleIndices.Index().Select(tuple => new Point(tuple.Item, 0d))]
                : [.. flatnessSampleIndices.Index().Select(tuple => new Point(tuple.Item, flatnessPhases[tuple.Index]))];
        }

        DirectoryHelper.CreateFileDirectoryIfNotExists(result.FilePath);
        FileHelper.DeleteFileIfExists(result.FilePath);
        FileHelper.SerializeOperate(result, result.FilePath);

        return result;

        (Vector<double> FlatnessFrequencies, Vector<double> FlatnessUniformities, Vector<double> FlatnessPhases) FFT(double amplitude, double offsetFrequency, double offsetFrequencyPeriodCoefficient)
        {
            cancellationToken.ThrowIfCancellationRequested();

            var (dFlatnessFrequencies, flatnessUniformities) = BuildFlatnessFrequencies();

            #region 相位

            var headerPhases = 2d * Math.PI * (Vector<double>.Build.Dense(headerSampleIndices.Length, headerFrequency) * dt).IntegrateCumulative();

            var flatnessPhases = (2d * Math.PI * dFlatnessFrequencies * dt).IntegrateCumulative();
            if (offsetFrequency != 0d && offsetFrequencyPeriodCoefficient != 0d) flatnessPhases += 2d * Math.PI * dFlatnessFrequencies * offsetFrequencyPeriodCoefficient * 1d / offsetFrequency;

            var footerPhases = 2d * Math.PI * (Vector<double>.Build.Dense(headerSampleIndices.Length, footerFrequency) * dt).IntegrateCumulative();

            #endregion

            #region 波形

            if (headerSampleIndices.Length > 0) aodWaveformSignals.SetSubVectorRange(headerSampleIndices[0], headerSampleIndices[^1], amplitude * headerPhases.PointwiseCos().PointwiseMultiply(Vector<double>.Build.DenseOfArray(headerSampleIndices) / headerSampleIndices.Length));
            aodWaveformSignals.SetSubVectorRange(flatnessSampleIndices[0], flatnessSampleIndices[^1], amplitude * flatnessPhases.PointwiseCos());
            if (footerSampleIndices.Length > 0) aodWaveformSignals.SetSubVectorRange(footerSampleIndices[0], footerSampleIndices[^1], amplitude * footerPhases.PointwiseCos().PointwiseMultiply(1d - (Vector<double>.Build.DenseOfArray(footerSampleIndices) - footerSampleIndices[0] + 1d) / footerSampleIndices.Length));

            #endregion 波形

            #region 傅里叶

            var flatnessAODWaveformSignals = aodWaveformSignals.SubVectorRange(flatnessSampleIndices[0], flatnessSampleIndices[^1]);
            fftResult = flatnessAODWaveformSignals.ToComplex().FastFourierTransform();
            (fftFrequencies, fftMagnitudes) = fftResult.GetPositiveFrequencies(param.SampleRate);

            #endregion 傅里叶

            return (dFlatnessFrequencies, flatnessUniformities, flatnessPhases);
        }

        (Vector<double> Frequencies, Vector<double> Uniformities) BuildFlatnessFrequencies()
        {
            if (param.FunctionMonotonicTypeEnum == FunctionMonotonicTypeEnum.Flatness)
                return (Vector<double>.Build.Dense(t.Count, centerFrequency), Vector<double>.Build.Dense(t.Count, 1d));

            var standardSlope = (footerFrequency - headerFrequency) / (t.Count - 1);
            var totalLength = t.Count;
            var segmentCount = param.SlopeConfigurations.Count;

            var uniformities = new double[totalLength];
            var frequencies = new double[totalLength];
            if (segmentCount == 0)
            {
                var centerMiddleIndex = (totalLength - 1) / 2;
                frequencies[centerMiddleIndex] = centerFrequency;
                uniformities[centerMiddleIndex] = 1d;
                for (var i = centerMiddleIndex - 1; i >= 0; i--)
                {
                    frequencies[i] = frequencies[i + 1] - standardSlope;
                    uniformities[i] = 1d;
                }

                for (var i = centerMiddleIndex + 1; i <= totalLength - 1; i++)
                {
                    frequencies[i] = frequencies[i - 1] + standardSlope;
                    uniformities[i] = 1d;
                }
            }
            else
            {
                var regions = GenerateSymmetricRegions(segmentCount, totalLength);

                Guard.IsNotEmpty(regions, "Segment count is too large for the number of flatness samples.");

                var centerSegmentIndex = regions.Length / 2;

                // 中心
                var (centerStartIndex, centerMiddleIndex, centerStopIndex) = regions[centerSegmentIndex];
                frequencies[centerMiddleIndex] = centerFrequency;
                uniformities[centerMiddleIndex] = param.SlopeConfigurations[centerSegmentIndex].Coefficient;
                var centerSlope = standardSlope * (1 + param.SlopeConfigurations[centerSegmentIndex].DeltaKRate);
                for (var i = centerMiddleIndex - 1; i >= centerStartIndex; i--)
                {
                    frequencies[i] = frequencies[i + 1] - centerSlope;
                    uniformities[i] = param.SlopeConfigurations[centerSegmentIndex].Coefficient;
                }

                for (var i = centerMiddleIndex + 1; i <= centerStopIndex; i++)
                {
                    frequencies[i] = frequencies[i - 1] + centerSlope;
                    uniformities[i] = param.SlopeConfigurations[centerSegmentIndex].Coefficient;
                }

                // 往左
                for (var segmentIndex = centerSegmentIndex - 1; segmentIndex >= 0; segmentIndex--)
                {
                    var (startIndex, _, stopIndex) = regions[segmentIndex];
                    var slope = standardSlope * (1 + param.SlopeConfigurations[segmentIndex].DeltaKRate);
                    for (var i = stopIndex; i >= startIndex; i--)
                    {
                        frequencies[i] = frequencies[i + 1] - slope;
                        uniformities[i] = param.SlopeConfigurations[segmentIndex].Coefficient;
                    }
                }

                // 往右
                for (var segmentIndex = centerSegmentIndex + 1; segmentIndex <= regions.Length - 1; segmentIndex++)
                {
                    var (startIndex, _, stopIndex) = regions[segmentIndex];
                    var slope = standardSlope * (1 + param.SlopeConfigurations[segmentIndex].DeltaKRate);
                    for (var i = startIndex; i <= stopIndex; i++)
                    {
                        frequencies[i] = frequencies[i - 1] + slope;
                        uniformities[i] = param.SlopeConfigurations[segmentIndex].Coefficient;
                    }
                }
            }

            return (Vector<double>.Build.Dense(frequencies), Vector<double>.Build.Dense(uniformities));
        }

        static (int StartIndex, int MiddleIndex, int StopIndex)[] GenerateSymmetricRegions(int segmentCount, int totalLength)
        {
            var regions = new (int StartIndex, int MiddleIndex, int StopIndex)[segmentCount];

            var centerIndex = (totalLength - 1) / 2;

            var segmentLength = totalLength / segmentCount;
            var halfWidth = segmentLength / 2;

            var centerSegmentIndex = regions.Length / 2;

            regions[centerSegmentIndex] = (
                centerIndex - halfWidth,
                centerIndex,
                centerIndex + halfWidth
            );

            // 往左
            for (var segmentIndex = centerSegmentIndex - 1; segmentIndex >= 0; segmentIndex--)
            {
                var stopIndex = regions[segmentIndex + 1].StartIndex - 1;
                var startIndex = Math.Max(0, stopIndex - segmentLength);
                if (segmentIndex == 0) startIndex = 0;
                var middleIndex = (startIndex + stopIndex) / 2;

                regions[segmentIndex] = (startIndex, middleIndex, stopIndex);
            }

            // 往右
            for (var segmentIndex = centerSegmentIndex + 1; segmentIndex <= regions.Length - 1; segmentIndex++)
            {
                var startIndex = regions[segmentIndex - 1].StopIndex + 1;
                var stopIndex = Math.Min(totalLength - 1, startIndex + segmentLength);
                if (segmentIndex == regions.Length - 1) stopIndex = totalLength - 1;
                var middleIndex = (startIndex + stopIndex) / 2;

                regions[segmentIndex] = (startIndex, middleIndex, stopIndex);
            }

            return regions;
        }
    }
}