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
        /// 三次补偿系数t^3
        /// </summary>
        public double P3CompensationCoefficient { get; init; }

        /// <summary>
        /// 四次补偿系数t^4
        /// </summary>
        public double P4CompensationCoefficient { get; init; }

        /// <summary>
        /// 五次补偿系数t^5
        /// </summary>
        public double P5CompensationCoefficient { get; init; }

        /// <summary>
        /// 六次补偿系数t^6
        /// </summary>
        public double P6CompensationCoefficient { get; init; }

        /// <summary>
        /// 七次补偿系数t^7
        /// </summary>
        public double P7CompensationCoefficient { get; init; }

        /// <summary>
        /// 八次补偿系数t^8
        /// </summary>
        public double P8CompensationCoefficient { get; init; }

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
        /// AOD波形非线性相位信号
        ///</summary>
        [Newtonsoft.Json.JsonIgnore]
        [System.Text.Json.Serialization.JsonIgnore]
        [System.Xml.Serialization.XmlIgnore]
        public IReadOnlyList<Point> FlatnessTotalPhaseSignals { get; internal set; } = [];

        /// <summary>
        /// AOD波形总补偿频率信号(P3-P8)
        ///</summary>
        [Newtonsoft.Json.JsonIgnore]
        [System.Text.Json.Serialization.JsonIgnore]
        [System.Xml.Serialization.XmlIgnore]
        public IReadOnlyList<Point> FlatnessTotalCompensationFrequencySignals { get; internal set; } = [];

        /// <summary>
        /// AOD波形总补偿相位信号(P3-P8)
        ///</summary>
        [Newtonsoft.Json.JsonIgnore]
        [System.Text.Json.Serialization.JsonIgnore]
        [System.Xml.Serialization.XmlIgnore]
        public IReadOnlyList<Point> FlatnessTotalCompensationPhaseSignals { get; internal set; } = [];

        /// <summary>
        /// AOD波形三次补偿信号
        ///</summary>
        [Newtonsoft.Json.JsonIgnore]
        [System.Text.Json.Serialization.JsonIgnore]
        [System.Xml.Serialization.XmlIgnore]
        public IReadOnlyList<Point> FlatnessP3CompensationFrequencySignals { get; internal set; } = [];

        /// <summary>
        /// AOD波形三次补偿相位信号
        ///</summary>
        [Newtonsoft.Json.JsonIgnore]
        [System.Text.Json.Serialization.JsonIgnore]
        [System.Xml.Serialization.XmlIgnore]
        public IReadOnlyList<Point> FlatnessP3CompensationPhaseSignals { get; internal set; } = [];

        /// <summary>
        /// AOD波形四次补偿信号
        ///</summary>
        [Newtonsoft.Json.JsonIgnore]
        [System.Text.Json.Serialization.JsonIgnore]
        [System.Xml.Serialization.XmlIgnore]
        public IReadOnlyList<Point> FlatnessP4CompensationFrequencySignals { get; internal set; } = [];

        /// <summary>
        /// AOD波形四次补偿相位信号
        ///</summary>
        [Newtonsoft.Json.JsonIgnore]
        [System.Text.Json.Serialization.JsonIgnore]
        [System.Xml.Serialization.XmlIgnore]
        public IReadOnlyList<Point> FlatnessP4CompensationPhaseSignals { get; internal set; } = [];

        /// <summary>
        /// AOD波形五次补偿信号
        ///</summary>
        [Newtonsoft.Json.JsonIgnore]
        [System.Text.Json.Serialization.JsonIgnore]
        [System.Xml.Serialization.XmlIgnore]
        public IReadOnlyList<Point> FlatnessP5CompensationFrequencySignals { get; internal set; } = [];

        /// <summary>
        /// AOD波形五次补偿相位信号
        ///</summary>
        [Newtonsoft.Json.JsonIgnore]
        [System.Text.Json.Serialization.JsonIgnore]
        [System.Xml.Serialization.XmlIgnore]
        public IReadOnlyList<Point> FlatnessP5CompensationPhaseSignals { get; internal set; } = [];

        /// <summary>
        /// AOD波形六次补偿信号
        ///</summary>
        [Newtonsoft.Json.JsonIgnore]
        [System.Text.Json.Serialization.JsonIgnore]
        [System.Xml.Serialization.XmlIgnore]
        public IReadOnlyList<Point> FlatnessP6CompensationFrequencySignals { get; internal set; } = [];

        /// <summary>
        /// AOD波形六次补偿相位信号
        ///</summary>
        [Newtonsoft.Json.JsonIgnore]
        [System.Text.Json.Serialization.JsonIgnore]
        [System.Xml.Serialization.XmlIgnore]
        public IReadOnlyList<Point> FlatnessP6CompensationPhaseSignals { get; internal set; } = [];

        /// <summary>
        /// AOD波形七次补偿信号
        ///</summary>
        [Newtonsoft.Json.JsonIgnore]
        [System.Text.Json.Serialization.JsonIgnore]
        [System.Xml.Serialization.XmlIgnore]
        public IReadOnlyList<Point> FlatnessP7CompensationFrequencySignals { get; internal set; } = [];

        /// <summary>
        /// AOD波形七次补偿相位信号
        ///</summary>
        [Newtonsoft.Json.JsonIgnore]
        [System.Text.Json.Serialization.JsonIgnore]
        [System.Xml.Serialization.XmlIgnore]
        public IReadOnlyList<Point> FlatnessP7CompensationPhaseSignals { get; internal set; } = [];

        /// <summary>
        /// AOD波形八次补偿信号
        ///</summary>
        [Newtonsoft.Json.JsonIgnore]
        [System.Text.Json.Serialization.JsonIgnore]
        [System.Xml.Serialization.XmlIgnore]
        public IReadOnlyList<Point> FlatnessP8CompensationFrequencySignals { get; internal set; } = [];

        /// <summary>
        /// AOD波形八次补偿相位信号
        ///</summary>
        [Newtonsoft.Json.JsonIgnore]
        [System.Text.Json.Serialization.JsonIgnore]
        [System.Xml.Serialization.XmlIgnore]
        public IReadOnlyList<Point> FlatnessP8CompensationPhaseSignals { get; internal set; } = [];
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
            var halfT = (t[^1] - t[0]) / 2d;
            t -= halfT; // us
            t /= halfT; // us

            var aodWaveformSignals = Vector<double>.Build.Dense(param.NumberOfSamples);

            Vector<Complex> fftResult;
            Vector<double> fftFrequencies;
            Vector<double> fftMagnitudes;

            #endregion 返回结果

            var result = GuardUtils.IsNotNullAndAssignableToType<TResult>(Activator.CreateInstance(typeof(TResult), param, true));
            result.Initialize();

            foreach (var item in result.Items)
            {
                cancellationToken.ThrowIfCancellationRequested();

                var (linearFrequencies, _,
                    p3Frequencies, p3Phases,
                    p4Frequencies, p4Phases,
                    p5Frequencies, p5Phases,
                    p6Frequencies, p6Phases,
                    p7Frequencies, p7Phases,
                    p8Frequencies, p8Phases,
                    flatnessCompensationFrequencies, flatnessCompensationPhases,
                    flatnessFrequencies, flatnessPhases) = FFT(item.OffsetConfiguration.Amplitude, item.OffsetConfiguration.OffsetFrequency, item.OffsetConfiguration.OffsetFrequencyPeriodCoefficient);

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

                item.FlatnessTotalFrequencySignals = item.OffsetConfiguration.IsGenerateAODWaveformZero
                    ? (Point[])[.. flatnessSampleIndices.Index().Select(tuple => new Point(tuple.Item, 0d))]
                    : [.. flatnessSampleIndices.Index().Select(tuple => new Point(tuple.Item, flatnessFrequencies[tuple.Index]))];
                item.FlatnessTotalPhaseSignals = item.OffsetConfiguration.IsGenerateAODWaveformZero
                    ? (Point[])[.. flatnessSampleIndices.Index().Select(tuple => new Point(tuple.Item, 0d))]
                    : [.. flatnessSampleIndices.Index().Select(tuple => new Point(tuple.Item, flatnessPhases[tuple.Index]))];
                item.FlatnessTotalCompensationFrequencySignals = item.OffsetConfiguration.IsGenerateAODWaveformZero
                    ? (Point[])[.. flatnessSampleIndices.Index().Select(tuple => new Point(tuple.Item, 0d))]
                    : [.. flatnessSampleIndices.Index().Select(tuple => new Point(tuple.Item, flatnessCompensationFrequencies[tuple.Index]))];
                item.FlatnessTotalCompensationPhaseSignals = item.OffsetConfiguration.IsGenerateAODWaveformZero
                    ? (Point[])[.. flatnessSampleIndices.Index().Select(tuple => new Point(tuple.Item, 0d))]
                    : [.. flatnessSampleIndices.Index().Select(tuple => new Point(tuple.Item, flatnessCompensationPhases[tuple.Index]))];

                item.FlatnessLinearFrequencySignals = item.OffsetConfiguration.IsGenerateAODWaveformZero
                    ? (Point[])[.. flatnessSampleIndices.Index().Select(tuple => new Point(tuple.Item, 0d))]
                    : [.. flatnessSampleIndices.Index().Select(tuple => new Point(tuple.Item, linearFrequencies[tuple.Index]))];
                item.FlatnessP3CompensationFrequencySignals = item.OffsetConfiguration.IsGenerateAODWaveformZero
                    ? (Point[])[.. flatnessSampleIndices.Index().Select(tuple => new Point(tuple.Item, 0d))]
                    : [.. flatnessSampleIndices.Index().Select(tuple => new Point(tuple.Item, p3Frequencies[tuple.Index]))];
                item.FlatnessP3CompensationPhaseSignals = item.OffsetConfiguration.IsGenerateAODWaveformZero
                    ? (Point[])[.. flatnessSampleIndices.Index().Select(tuple => new Point(tuple.Item, 0d))]
                    : [.. flatnessSampleIndices.Index().Select(tuple => new Point(tuple.Item, p3Phases[tuple.Index]))];

                item.FlatnessP4CompensationFrequencySignals = item.OffsetConfiguration.IsGenerateAODWaveformZero
                    ? (Point[])[.. flatnessSampleIndices.Index().Select(tuple => new Point(tuple.Item, 0d))]
                    : [.. flatnessSampleIndices.Index().Select(tuple => new Point(tuple.Item, p4Frequencies[tuple.Index]))];
                item.FlatnessP4CompensationPhaseSignals = item.OffsetConfiguration.IsGenerateAODWaveformZero
                    ? (Point[])[.. flatnessSampleIndices.Index().Select(tuple => new Point(tuple.Item, 0d))]
                    : [.. flatnessSampleIndices.Index().Select(tuple => new Point(tuple.Item, p4Phases[tuple.Index]))];

                item.FlatnessP5CompensationFrequencySignals = item.OffsetConfiguration.IsGenerateAODWaveformZero
                    ? (Point[])[.. flatnessSampleIndices.Index().Select(tuple => new Point(tuple.Item, 0d))]
                    : [.. flatnessSampleIndices.Index().Select(tuple => new Point(tuple.Item, p5Frequencies[tuple.Index]))];
                item.FlatnessP5CompensationPhaseSignals = item.OffsetConfiguration.IsGenerateAODWaveformZero
                    ? (Point[])[.. flatnessSampleIndices.Index().Select(tuple => new Point(tuple.Item, 0d))]
                    : [.. flatnessSampleIndices.Index().Select(tuple => new Point(tuple.Item, p5Phases[tuple.Index]))];

                item.FlatnessP6CompensationFrequencySignals = item.OffsetConfiguration.IsGenerateAODWaveformZero
                    ? (Point[])[.. flatnessSampleIndices.Index().Select(tuple => new Point(tuple.Item, 0d))]
                    : [.. flatnessSampleIndices.Index().Select(tuple => new Point(tuple.Item, p6Frequencies[tuple.Index]))];
                item.FlatnessP6CompensationPhaseSignals = item.OffsetConfiguration.IsGenerateAODWaveformZero
                    ? (Point[])[.. flatnessSampleIndices.Index().Select(tuple => new Point(tuple.Item, 0d))]
                    : [.. flatnessSampleIndices.Index().Select(tuple => new Point(tuple.Item, p6Phases[tuple.Index]))];

                item.FlatnessP7CompensationFrequencySignals = item.OffsetConfiguration.IsGenerateAODWaveformZero
                    ? (Point[])[.. flatnessSampleIndices.Index().Select(tuple => new Point(tuple.Item, 0d))]
                    : [.. flatnessSampleIndices.Index().Select(tuple => new Point(tuple.Item, p7Frequencies[tuple.Index]))];
                item.FlatnessP7CompensationPhaseSignals = item.OffsetConfiguration.IsGenerateAODWaveformZero
                    ? (Point[])[.. flatnessSampleIndices.Index().Select(tuple => new Point(tuple.Item, 0d))]
                    : [.. flatnessSampleIndices.Index().Select(tuple => new Point(tuple.Item, p7Phases[tuple.Index]))];

                item.FlatnessP8CompensationFrequencySignals = item.OffsetConfiguration.IsGenerateAODWaveformZero
                    ? (Point[])[.. flatnessSampleIndices.Index().Select(tuple => new Point(tuple.Item, 0d))]
                    : [.. flatnessSampleIndices.Index().Select(tuple => new Point(tuple.Item, p8Frequencies[tuple.Index]))];
                item.FlatnessP8CompensationPhaseSignals = item.OffsetConfiguration.IsGenerateAODWaveformZero
                    ? (Point[])[.. flatnessSampleIndices.Index().Select(tuple => new Point(tuple.Item, 0d))]
                    : [.. flatnessSampleIndices.Index().Select(tuple => new Point(tuple.Item, p8Phases[tuple.Index]))];
            }

            DirectoryHelper.CreateFileDirectoryIfNotExists(result.FilePath);
            FileHelper.DeleteFileIfExists(result.FilePath);
            FileHelper.SerializeOperate(result, result.FilePath);

            return (result, null);

            (Vector<double> LinearFrequencies, Vector<double> P2Phases,
                Vector<double> P3Frequencies, Vector<double> P3Phases,
                Vector<double> P4Frequencies, Vector<double> P4Phases,
                Vector<double> P5Frequencies, Vector<double> P5Phases,
                Vector<double> P6Frequencies, Vector<double> P6Phases,
                Vector<double> P7Frequencies, Vector<double> P7Phases,
                Vector<double> P8Frequencies, Vector<double> P8Phases,
                Vector<double> FlatnessCompensationFrequencies, Vector<double> FlatnessCompensationPhases,
                Vector<double> FlatnessFrequencies, Vector<double> FlatnessPhases)
                FFT(double amplitude, double offsetFrequency, double offsetFrequencyPeriodCoefficient)
            {
                cancellationToken.ThrowIfCancellationRequested();

                var δt = offsetFrequency == 0 ? 0d : offsetFrequencyPeriodCoefficient * 1d / offsetFrequency;
                var tShift = t - δt / halfT;

                var (linearFrequencies, p2Phases) = GetP2CompensationSignals(tShift);
                var (p3Frequencies, p3Phases) = GetP3CompensationSignals(param.P3CompensationCoefficient, tShift);
                var (p4Frequencies, p4Phases) = GetP4CompensationSignals(param.P4CompensationCoefficient, tShift);
                var (p5Frequencies, p5Phases) = GetP5CompensationSignals(param.P5CompensationCoefficient, tShift);
                var (p6Frequencies, p6Phases) = GetP6CompensationSignals(param.P6CompensationCoefficient, tShift);
                var (p7Frequencies, p7Phases) = GetP7CompensationSignals(param.P7CompensationCoefficient, tShift);
                var (p8Frequencies, p8Phases) = GetP8CompensationSignals(param.P8CompensationCoefficient, tShift);

                var flatnessCompensationFrequencies = (footerFrequency - headerFrequency) / 4d * (p3Frequencies + p4Frequencies + p5Frequencies + p6Frequencies + p7Frequencies + p8Frequencies);
                var flatnessFrequencies = centerFrequency + (footerFrequency - headerFrequency) / 4d * linearFrequencies + flatnessCompensationFrequencies;

                #region 相位

                var headerPhases = 2d * Math.PI * (Vector<double>.Build.Dense(headerSampleIndices.Length, headerFrequency) * dt).IntegrateCumulative();

                var flatnessCompensationPhases = (footerFrequency - headerFrequency) * halfT / 4d * (p3Phases + p4Phases + p5Phases + p6Phases + p7Phases + p8Phases);
                var flatnessPhases = 2 * Math.PI * (centerFrequency * halfT * tShift + (footerFrequency - headerFrequency) * halfT / 4d * p2Phases + flatnessCompensationPhases);

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

                return (linearFrequencies, p2Phases,
                    p3Frequencies, p3Phases,
                    p4Frequencies, p4Phases,
                    p5Frequencies, p5Phases,
                    p6Frequencies, p6Phases,
                    p7Frequencies, p7Phases,
                    p8Frequencies, p8Phases,
                    flatnessCompensationFrequencies, flatnessCompensationPhases,
                    flatnessFrequencies, flatnessPhases);
            }

            (Vector<double> Frequency, Vector<double> Phase) GetP2CompensationSignals(Vector<double> tShift)
            {
                return param.FunctionMonotonicTypeEnum != FunctionMonotonicTypeEnum.Flatness
                    ? (2d * tShift.PointwisePower(1d), tShift.PointwisePower(2))
                    : (Vector<double>.Build.Dense(t.Count, 0d), Vector<double>.Build.Dense(t.Count, 0d));
            }

            (Vector<double> Frequency, Vector<double> Phase) GetP3CompensationSignals(double coefficient, Vector<double> tShift)
            {
                if (param.FunctionMonotonicTypeEnum == FunctionMonotonicTypeEnum.Flatness)
                {
                    var zeros = Vector<double>.Build.Dense(t.Count, 0d);
                    return (zeros, zeros);
                }

                // P3(x) = 1/2 * (5x^3 - 3x)
                var t3 = tShift.PointwisePower(3d);

                return (Vector<double>.Build.Dense(t.Count, 0d), coefficient * 1d / 2d * (5d * t3 - 3d * tShift));
            }

            (Vector<double> Frequency, Vector<double> Phase) GetP4CompensationSignals(double coefficient, Vector<double> tShift)
            {
                if (param.FunctionMonotonicTypeEnum == FunctionMonotonicTypeEnum.Flatness)
                {
                    var zeros = Vector<double>.Build.Dense(t.Count, 0d);
                    return (zeros, zeros);
                }

                // P4(x) = 1/8 * (35x^4 - 30x^2 + 3)
                var t2 = tShift.PointwisePower(2d);
                var t4 = tShift.PointwisePower(4d);

                return (Vector<double>.Build.Dense(t.Count, 0d), coefficient * 1d / 8d * (35d * t4 - 30d * t2 + 3d));
            }

            (Vector<double> Frequency, Vector<double> Phase) GetP5CompensationSignals(double coefficient, Vector<double> tShift)
            {
                if (param.FunctionMonotonicTypeEnum == FunctionMonotonicTypeEnum.Flatness)
                {
                    var zeros = Vector<double>.Build.Dense(t.Count, 0d);
                    return (zeros, zeros);
                }

                // P5(x) = 1/8 * (63x^5 - 70x^3 + 15x)
                var t3 = tShift.PointwisePower(3d);
                var t5 = tShift.PointwisePower(5d);

                return (Vector<double>.Build.Dense(t.Count, 0d), coefficient * 1d / 8d * (63d * t5 - 70d * t3 + 15d * tShift));
            }

            (Vector<double> Frequency, Vector<double> Phase) GetP6CompensationSignals(double coefficient, Vector<double> tShift)
            {
                if (param.FunctionMonotonicTypeEnum == FunctionMonotonicTypeEnum.Flatness)
                {
                    var zeros = Vector<double>.Build.Dense(t.Count, 0d);
                    return (zeros, zeros);
                }

                // P6(x) = 1/16 * (231x^6 - 315x^4 + 105x^2 - 5)
                var t2 = tShift.PointwisePower(2d);
                var t4 = tShift.PointwisePower(4d);
                var t6 = tShift.PointwisePower(6d);

                return (Vector<double>.Build.Dense(t.Count, 0d), coefficient * 1d / 16d * (231d * t6 - 315d * t4 + 105d * t2 - 5d));
            }

            (Vector<double> Frequency, Vector<double> Phase) GetP7CompensationSignals(double coefficient, Vector<double> tShift)
            {
                if (param.FunctionMonotonicTypeEnum == FunctionMonotonicTypeEnum.Flatness)
                {
                    var zeros = Vector<double>.Build.Dense(t.Count, 0d);
                    return (zeros, zeros);
                }

                // P7(x) = 1/16 * (429x^7 - 693x^5 + 315x^3 - 35x)
                var t3 = tShift.PointwisePower(3d);
                var t5 = tShift.PointwisePower(5d);
                var t7 = tShift.PointwisePower(7d);

                return (Vector<double>.Build.Dense(t.Count, 0d), coefficient * 1d / 16d * (429d * t7 - 693d * t5 + 315d * t3 - 35d * tShift));
            }

            (Vector<double> Frequency, Vector<double> Phase) GetP8CompensationSignals(double coefficient, Vector<double> tShift)
            {
                if (param.FunctionMonotonicTypeEnum == FunctionMonotonicTypeEnum.Flatness)
                {
                    var zeros = Vector<double>.Build.Dense(t.Count, 0d);
                    return (zeros, zeros);
                }

                // P8(x) = 1/128 * (6435x^8 - 12012x^6 + 6930x^4 - 1260x^2 + 35)
                var t2 = tShift.PointwisePower(2d);
                var t4 = tShift.PointwisePower(4d);
                var t6 = tShift.PointwisePower(6d);
                var t8 = tShift.PointwisePower(8d);

                return (Vector<double>.Build.Dense(t.Count, 0d), coefficient * 1d / 128d * (6435d * t8 - 12012d * t6 + 6930d * t4 - 1260d * t2 + 35d));
            }
        }
        catch (Exception ex)
        {
            return (GuardUtils.IsNotNullAndAssignableToType<TResult>(Activator.CreateInstance(typeof(TResult), param, false)), ex);
        }
    }
}