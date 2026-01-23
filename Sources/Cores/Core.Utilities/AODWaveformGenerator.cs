using CommunityToolkit.Diagnostics;
using MathNet.Numerics.Interpolation;
using MathNet.Numerics.LinearAlgebra;
using Net.Utilities.Algorithms.Extensions;
using Net.Utilities.Helpers.Helpers.Files;
using Net.Utilities.Models;
using Net.Utilities.Models.Enums.Maths;
using Net.Utilities.Models.Geometries;
using Complex = System.Numerics.Complex;

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
        /// 生成AOD波形文件重试次数
        /// </summary>
        public int GenerateRetryTimes { get; init; }

        /// <summary>
        /// 生成多个AOD波形中每个波形的频率偏移配置集合
        /// </summary>
        public IReadOnlyList<AODWaveformOffsetConfiguration> OffsetConfigurations { get; init; } = [];

        /// <summary>
        /// 二次补偿系数t^2
        /// </summary>
        public double C2CompensationCoefficient { get; init; }

        /// <summary>
        /// 三次补偿系数t^3
        /// </summary>
        public double C3CompensationCoefficient { get; init; }

        /// <summary>
        /// 四次补偿系数t^4
        /// </summary>
        public double C4CompensationCoefficient { get; init; }

        /// <summary>
        /// 五次补偿系数t^5
        /// </summary>
        public double C5CompensationCoefficient { get; init; }

        /// <summary>
        /// 六次补偿系数t^6
        /// </summary>
        public double C6CompensationCoefficient { get; init; }

        /// <summary>
        /// 七次补偿系数t^7
        /// </summary>
        public double C7CompensationCoefficient { get; init; }

        /// <summary>
        /// 八次补偿系数t^8
        /// </summary>
        public double C8CompensationCoefficient { get; init; }

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
            Guard.IsGreaterThan(GenerateRetryTimes, 0d);

            Guard.IsNotEmpty(OffsetConfigurations, "Offset Configuration is must be not empty.");

            foreach (var item in OffsetConfigurations)
            {
                item.Validate();
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
    public sealed record PrescanAODWaveformResult(PrescanAODWaveformParam Param, bool IsSuccess) : AbstractAODWaveformResult<PrescanAODWaveformParam>(Param, IsSuccess)
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

            if (IsSuccess == false) fileName = $"ERROR_{fileName}";

            FilePath = FileHelper.GetEnsureLongPathSupport(Path.Combine(Param.DirectoryPath, prescan + Id, FileHelper.RemoveInvalidFileName(fileName)));

            var itemList = new List<AODWaveformResultItem>();
            foreach (var item in Param.OffsetConfigurations)
            {
                fileName = prescan +
                           $"_{Param.FileNameSuffix}" +
                           $"${Param.NumberOfSamples + Param.ZeroSampleCount}${Param.ZeroSampleCount}$600$02${item.OffsetFrequency:0.###}${item.OffsetFrequencyPeriodCoefficient:0.###}$.txt";

                if (IsSuccess == false) fileName = $"ERROR_{fileName}";

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
    public sealed record ChirpAODWaveformResult(ChirpAODWaveformParam Param, bool IsSuccess) : AbstractAODWaveformResult<ChirpAODWaveformParam>(Param, IsSuccess)
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

            if (IsSuccess == false) fileName = $"ERROR_{fileName}";

            FilePath = FileHelper.GetEnsureLongPathSupport(Path.Combine(Param.DirectoryPath, chirp + Id, FileHelper.RemoveInvalidFileName(fileName)));

            var itemList = new List<AODWaveformResultItem>();
            foreach (var item in Param.OffsetConfigurations)
            {
                fileName = chirp +
                           $"_{Param.FileNameSuffix}" +
                           $"${Param.NumberOfSamples + Param.ZeroSampleCount}${Param.ZeroSampleCount}$600$03${item.OffsetFrequency:0.###}${item.OffsetFrequencyPeriodCoefficient:0.###}$.txt";
                if (IsSuccess == false) fileName = $"ERROR_{fileName}";

                itemList.Add(new AODWaveformResultItem(item, FileHelper.GetEnsureLongPathSupport(Path.Combine(Param.DirectoryPath, chirp + Id, FileHelper.RemoveInvalidFileName(item.DirectoryName), FileHelper.RemoveInvalidFileName(fileName)))));
            }

            Items = itemList;
        }
    }

    /// <summary>
    /// AOD波形结果
    /// </summary>
    /// <param name="Param">AOD波形生成参数</param>
    /// <param name="IsSuccess">是否成功</param>
    public abstract record AbstractAODWaveformResult<TParam>(TParam Param, bool IsSuccess) where TParam : AbstractAODWaveformParam
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
        public IReadOnlyList<Point> FlatnessLinearFrequencySignals { get; internal set; } = [];

        /// <summary>
        /// AOD波形非线性频率信号
        ///</summary>
        [Newtonsoft.Json.JsonIgnore]
        [System.Text.Json.Serialization.JsonIgnore]
        [System.Xml.Serialization.XmlIgnore]
        public IReadOnlyList<Point> FlatnessTotalFrequencySignals { get; internal set; } = [];

        /// <summary>
        /// AOD波形二次补偿信号
        ///</summary>
        [Newtonsoft.Json.JsonIgnore]
        [System.Text.Json.Serialization.JsonIgnore]
        [System.Xml.Serialization.XmlIgnore]
        public IReadOnlyList<Point> FlatnessC2CompensationSignals { get; internal set; } = [];

        /// <summary>
        /// AOD波形三次补偿信号
        ///</summary>
        [Newtonsoft.Json.JsonIgnore]
        [System.Text.Json.Serialization.JsonIgnore]
        [System.Xml.Serialization.XmlIgnore]
        public IReadOnlyList<Point> FlatnessC3CompensationSignals { get; internal set; } = [];

        /// <summary>
        /// AOD波形四次补偿信号
        ///</summary>
        [Newtonsoft.Json.JsonIgnore]
        [System.Text.Json.Serialization.JsonIgnore]
        [System.Xml.Serialization.XmlIgnore]
        public IReadOnlyList<Point> FlatnessC4CompensationSignals { get; internal set; } = [];

        /// <summary>
        /// AOD波形五次补偿信号
        ///</summary>
        [Newtonsoft.Json.JsonIgnore]
        [System.Text.Json.Serialization.JsonIgnore]
        [System.Xml.Serialization.XmlIgnore]
        public IReadOnlyList<Point> FlatnessC5CompensationSignals { get; internal set; } = [];

        /// <summary>
        /// AOD波形六次补偿信号
        ///</summary>
        [Newtonsoft.Json.JsonIgnore]
        [System.Text.Json.Serialization.JsonIgnore]
        [System.Xml.Serialization.XmlIgnore]
        public IReadOnlyList<Point> FlatnessC6CompensationSignals { get; internal set; } = [];

        /// <summary>
        /// AOD波形七次补偿信号
        ///</summary>
        [Newtonsoft.Json.JsonIgnore]
        [System.Text.Json.Serialization.JsonIgnore]
        [System.Xml.Serialization.XmlIgnore]
        public IReadOnlyList<Point> FlatnessC7CompensationSignals { get; internal set; } = [];

        /// <summary>
        /// AOD波形八次补偿信号
        ///</summary>
        [Newtonsoft.Json.JsonIgnore]
        [System.Text.Json.Serialization.JsonIgnore]
        [System.Xml.Serialization.XmlIgnore]
        public IReadOnlyList<Point> FlatnessC8CompensationSignals { get; internal set; } = [];


    }

    #endregion 结果

    /// <inheritdoc cref="GenerateAODWaveform{TResult,TParam}"/>
    /// <remarks>
    /// 生成PrescanAOD波形
    /// </remarks>
    public static (PrescanAODWaveformResult AODWaveformResult, Exception? Exception) GeneratePrescanAODWaveform(PrescanAODWaveformParam param, CancellationToken cancellationToken) => GenerateAODWaveform<PrescanAODWaveformResult, PrescanAODWaveformParam>(param, cancellationToken);

    /// <inheritdoc cref="GenerateAODWaveform{TResult,TParam}"/>
    /// <remarks>
    /// 生成ChirpAOD波形
    /// </remarks>
    public static (ChirpAODWaveformResult AODWaveformResult, Exception? Exception) GenerateChirpAODWaveform(ChirpAODWaveformParam param, CancellationToken cancellationToken) => GenerateAODWaveform<ChirpAODWaveformResult, ChirpAODWaveformParam>(param, cancellationToken);

    /// <summary>
    /// 生成AOD波形
    /// </summary>
    /// <param name="param">AOD波形生成参数</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>(结果, 异常信息)</returns>
    private static (TResult AODWaveformResult, Exception? Exception) GenerateAODWaveform<TResult, TParam>(TParam param, CancellationToken cancellationToken)
        where TResult : AbstractAODWaveformResult<TParam>
        where TParam : AbstractAODWaveformParam
    {
        try
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

            var allSampleIndices = GenerateUtils.LinearIndexRange(0, param.NumberOfSamples - 1);
            var headerSampleIndices = GenerateUtils.LinearIndexRange(0, param.EndpointSampleCount - 1);
            var flatnessSampleIndices = GenerateUtils.LinearIndexRange(param.EndpointSampleCount, param.NumberOfSamples - param.EndpointSampleCount - 1);
            var footerSampleIndices = GenerateUtils.LinearIndexRange(param.NumberOfSamples - param.EndpointSampleCount, param.NumberOfSamples - 1);

            var t = (Vector<double>.Build.DenseOfArray(flatnessSampleIndices) - flatnessSampleIndices[0]) * dt; // us
            t -= (t[^1] - t[0]) / 2d; // us
            var k = (footerFrequency - lowFrequency) / (t[^1] - t[0]); // MHz/us

            var aodWaveformSignals = Vector<double>.Build.Dense(param.NumberOfSamples);

            Vector<double> linearFrequencies;
            Vector<double> c2Frequencies;
            Vector<double> c3Frequencies;
            Vector<double> c4Frequencies;
            Vector<double> c5Frequencies;
            Vector<double> c6Frequencies;
            Vector<double> c7Frequencies;
            Vector<double> c8Frequencies;

            Vector<Complex> fftResult;
            Vector<double> fftFrequencies;
            Vector<double> fftMagnitudes;

            #endregion 返回结果

            #region 频率

            switch (param.FunctionMonotonicTypeEnum)
            {
                case FunctionMonotonicTypeEnum.Increasing:
                case FunctionMonotonicTypeEnum.Deceasing:
                    linearFrequencies = k * t;
                    c2Frequencies = param.C2CompensationCoefficient * t;
                    c3Frequencies = param.C3CompensationCoefficient * t.PointwisePower(2d);
                    c4Frequencies = param.C4CompensationCoefficient * t.PointwisePower(3d);
                    c5Frequencies = param.C5CompensationCoefficient * t.PointwisePower(4d);
                    c6Frequencies = param.C6CompensationCoefficient * t.PointwisePower(5d);
                    c7Frequencies = param.C7CompensationCoefficient * t.PointwisePower(6d);
                    c8Frequencies = param.C8CompensationCoefficient * t.PointwisePower(7d);

                    break;

                case FunctionMonotonicTypeEnum.Flatness:
                default:
                    linearFrequencies = Vector<double>.Build.Dense(t.Count, 0d);
                    c2Frequencies = Vector<double>.Build.Dense(t.Count, 0d);
                    c3Frequencies = Vector<double>.Build.Dense(t.Count, 0d);
                    c4Frequencies = Vector<double>.Build.Dense(t.Count, 0d);
                    c5Frequencies = Vector<double>.Build.Dense(t.Count, 0d);
                    c6Frequencies = Vector<double>.Build.Dense(t.Count, 0d);
                    c7Frequencies = Vector<double>.Build.Dense(t.Count, 0d);
                    c8Frequencies = Vector<double>.Build.Dense(t.Count, 0d);

                    break;
            }

            var flatnessFrequencies = centerFrequency + linearFrequencies + c2Frequencies + c3Frequencies + c4Frequencies + c5Frequencies + c6Frequencies + c7Frequencies + c8Frequencies;

            if (flatnessFrequencies.Exists(f => f <= 0 || f > param.HighFrequency * 1.5d)) ThrowHelper.ThrowArgumentException(nameof(flatnessFrequencies), $"Synthesized frequencies must be in the range (0, {param.HighFrequency * 1.5d:f3} MHz)");

            #endregion 频率

            var result = GuardUtils.IsNotNullAndAssignableToType<TResult>(Activator.CreateInstance(typeof(TResult), param, true));
            result.Initialize();

            foreach (var item in result.Items)
            {
                cancellationToken.ThrowIfCancellationRequested();

                FFT(item.OffsetConfiguration.Amplitude, item.OffsetConfiguration.OffsetFrequency, item.OffsetConfiguration.OffsetFrequencyPeriodCoefficient);
                var frequencyCoefficientList = new List<Point>();

                if (param.FunctionMonotonicTypeEnum != FunctionMonotonicTypeEnum.Flatness && item.OffsetConfiguration.UniformityConfigurations.Count > 0)
                {
                    var linearSpline = LinearSpline.InterpolateSorted(
                        [.. item.OffsetConfiguration.UniformityConfigurations.Select(configuration => configuration.Frequency)],
                        [.. item.OffsetConfiguration.UniformityConfigurations.Select(configuration => configuration.Coefficient)]);

                    var flatnessAODWaveformSignals = aodWaveformSignals.SubVectorRange(flatnessSampleIndices[0], flatnessSampleIndices[^1]);
                    for (var i = 0; i < flatnessAODWaveformSignals.Count; i++)
                    {
                        var frequency = flatnessFrequencies[i];
                        var coefficient = linearSpline.Interpolate(frequency);

                        flatnessAODWaveformSignals[i] *= coefficient;

                        frequencyCoefficientList.Add(new Point(frequency, coefficient));
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
                 *   -1.0 → -32768 (0x8000) Math.Pow(2d, 15d) -1
                 *    0.0 → 0      (0x0000)
                 *   +1.0 → +32767 (0x7FFF) -Math.Pow(2d, 15d)
                 */

                // 将结果转换为16位整数并保存到文件
                var hexStrings = item.OffsetConfiguration.IsGenerateAODWaveformZero
                    ? (string[])[..Enumerable.Repeat(((short)0).ToString("x4"), aodWaveformSignals.Count)]
                    : [..aodWaveformSignals.Select(y => ((short)Math.Clamp(Math.Round(y * Math.Pow(2d, 15d), MidpointRounding.AwayFromZero), short.MinValue, short.MaxValue)).ToString("x4"))];

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
                item.FlatnessLinearFrequencySignals = item.OffsetConfiguration.IsGenerateAODWaveformZero
                    ? (Point[])[.. flatnessSampleIndices.Index().Select(tuple => new Point(tuple.Item, 0d))]
                    : [.. flatnessSampleIndices.Index().Select(tuple => new Point(tuple.Item, linearFrequencies[tuple.Index]))];
                item.FlatnessTotalFrequencySignals = item.OffsetConfiguration.IsGenerateAODWaveformZero
                    ? (Point[])[.. flatnessSampleIndices.Index().Select(tuple => new Point(tuple.Item, 0d))]
                    : [.. flatnessSampleIndices.Index().Select(tuple => new Point(tuple.Item, flatnessFrequencies[tuple.Index]))];
                item.FlatnessC2CompensationSignals = item.OffsetConfiguration.IsGenerateAODWaveformZero
                    ? (Point[])[.. flatnessSampleIndices.Index().Select(tuple => new Point(tuple.Item, 0d))]
                    : [.. flatnessSampleIndices.Index().Select(tuple => new Point(tuple.Item, c2Frequencies[tuple.Index]))];
                item.FlatnessC3CompensationSignals = item.OffsetConfiguration.IsGenerateAODWaveformZero
                    ? (Point[])[.. flatnessSampleIndices.Index().Select(tuple => new Point(tuple.Item, 0d))]
                    : [.. flatnessSampleIndices.Index().Select(tuple => new Point(tuple.Item, c3Frequencies[tuple.Index]))];
                item.FlatnessC4CompensationSignals = item.OffsetConfiguration.IsGenerateAODWaveformZero
                    ? (Point[])[.. flatnessSampleIndices.Index().Select(tuple => new Point(tuple.Item, 0d))]
                    : [.. flatnessSampleIndices.Index().Select(tuple => new Point(tuple.Item, c4Frequencies[tuple.Index]))];
                item.FlatnessC5CompensationSignals = item.OffsetConfiguration.IsGenerateAODWaveformZero
                    ? (Point[])[.. flatnessSampleIndices.Index().Select(tuple => new Point(tuple.Item, 0d))]
                    : [.. flatnessSampleIndices.Index().Select(tuple => new Point(tuple.Item, c5Frequencies[tuple.Index]))];
                item.FlatnessC6CompensationSignals = item.OffsetConfiguration.IsGenerateAODWaveformZero
                    ? (Point[])[.. flatnessSampleIndices.Index().Select(tuple => new Point(tuple.Item, 0d))]
                    : [.. flatnessSampleIndices.Index().Select(tuple => new Point(tuple.Item, c6Frequencies[tuple.Index]))];
                item.FlatnessC7CompensationSignals = item.OffsetConfiguration.IsGenerateAODWaveformZero
                    ? (Point[])[.. flatnessSampleIndices.Index().Select(tuple => new Point(tuple.Item, 0d))]
                    : [.. flatnessSampleIndices.Index().Select(tuple => new Point(tuple.Item, c7Frequencies[tuple.Index]))];
                item.FlatnessC8CompensationSignals = item.OffsetConfiguration.IsGenerateAODWaveformZero
                    ? (Point[])[.. flatnessSampleIndices.Index().Select(tuple => new Point(tuple.Item, 0d))]
                    : [.. flatnessSampleIndices.Index().Select(tuple => new Point(tuple.Item, c8Frequencies[tuple.Index]))];

            }

            DirectoryHelper.CreateFileDirectoryIfNotExists(result.FilePath);
            FileHelper.DeleteFileIfExists(result.FilePath);
            FileHelper.SerializeOperate(result, result.FilePath);

            return (result, null);

            void FFT(double amplitude, double offsetFrequency, double offsetFrequencyPeriodCoefficient)
            {
                cancellationToken.ThrowIfCancellationRequested();

                var δt = offsetFrequencyPeriodCoefficient * 1d / offsetFrequency;

                #region 相位

                var headerPhases = 2d * Math.PI * (Vector<double>.Build.Dense(headerSampleIndices.Length, headerFrequency) * dt).IntegrateCumulative();
                var flatnessPhases = param.FunctionMonotonicTypeEnum != FunctionMonotonicTypeEnum.Flatness
                    ? 2d * Math.PI * (centerFrequency * (t - δt)
                                      + 1d / 2d * k * (t - δt).PointwisePower(2)
                                      + param.C2CompensationCoefficient * 1d / 2d * (t - δt).PointwisePower(2)
                                      + param.C3CompensationCoefficient * 1d / 3d * (t - δt).PointwisePower(3)
                                      + param.C4CompensationCoefficient * 1d / 4d * (t - δt).PointwisePower(4)
                                      + param.C5CompensationCoefficient * 1d / 5d * (t - δt).PointwisePower(5)
                                      + param.C6CompensationCoefficient * 1d / 6d * (t - δt).PointwisePower(6)
                                      + param.C7CompensationCoefficient * 1d / 7d * (t - δt).PointwisePower(7)
                                      + param.C8CompensationCoefficient * 1d / 8d * (t - δt).PointwisePower(8))
                    : 2d * Math.PI * (centerFrequency * (t - δt));
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
            }
        }
        catch (Exception ex)
        {
            return (GuardUtils.IsNotNullAndAssignableToType<TResult>(Activator.CreateInstance(typeof(TResult), param, false)), ex);
        }
    }
}