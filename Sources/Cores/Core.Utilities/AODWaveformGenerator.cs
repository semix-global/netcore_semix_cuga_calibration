using CommunityToolkit.Diagnostics;
using MathNet.Numerics.LinearAlgebra;
using Net.Utilities.Algorithms.Extensions;
using Net.Utilities.Algorithms.Modules;
using Net.Utilities.Helpers.Helpers.Files;
using Net.Utilities.Models;
using Net.Utilities.Models.Enums.Maths;
using Net.Utilities.Models.Geometries;
using Complex = System.Numerics.Complex;

namespace Core.Utilities;

public static class AODWaveformGenerator
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
    public sealed record AODWaveformOffsetConfiguration(string DirectoryName, double OffsetFrequency, double OffsetFrequencyPeriodCoefficient)
    {
        public void Validate()
        {
            Guard.IsNotNullOrWhiteSpace(DirectoryName, nameof(DirectoryName));
            Guard.IsGreaterThanOrEqualTo(OffsetFrequency, 0d, nameof(OffsetFrequency));
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
            Guard.IsGreaterThan(Frequency, 0d, nameof(Frequency));
            Guard.IsGreaterThan(Coefficient, 0d, nameof(Coefficient));
            Guard.IsBetweenOrEqualTo(Coefficient, 0d, 1d, nameof(Coefficient));
        }

        internal Point ToPoint() => new(Frequency, Coefficient);
    }


    /// <inheritdoc cref="AbstractAODWaveformParam"/>
    /// <remarks>
    /// PrescanAOD波形生成参数
    /// </remarks>
    public sealed record PrescanAODWaveformParam(
        double BandWidth,
        double CenterFrequency,
        double FlatnessTime,
        FunctionMonotonicTypeEnum FunctionMonotonicTypeEnum,
        double SampleRate,
        double Amplitude,
        string DirectoryPath,
        int ZeroSampleCount,
        int EndpointSampleCount,
        int GenerateRetryTimes,
        IReadOnlyList<AODWaveformOffsetConfiguration> OffsetConfigurations,
        IReadOnlyList<AODWaveformUniformityConfiguration> UniformityConfigurations,
        double SincCoefficient = 0d,
        double AstigmatismCompensationCoefficient = 0d,
        double SphericalAberrationCompensationCoefficient = 0d,
        double SecondaryAstigmatismCompensationCoefficient = 0d,
        double ComaCompensationCoefficient = 0d,
        double TrefoilCompensationCoefficient = 0d,
        double QuadrafoilCompensationCoefficient = 0d,
        double AlphaOrder = 0d,
        double AlphaOrderCoefficient = 0d) : AbstractAODWaveformParam(
        BandWidth,
        CenterFrequency,
        FlatnessTime,
        FunctionMonotonicTypeEnum,
        SampleRate,
        Amplitude,
        DirectoryPath,
        ZeroSampleCount,
        EndpointSampleCount,
        GenerateRetryTimes,
        OffsetConfigurations,
        UniformityConfigurations,
        SincCoefficient,
        AstigmatismCompensationCoefficient,
        SphericalAberrationCompensationCoefficient,
        SecondaryAstigmatismCompensationCoefficient,
        ComaCompensationCoefficient,
        TrefoilCompensationCoefficient,
        QuadrafoilCompensationCoefficient,
        AlphaOrder,
        AlphaOrderCoefficient);

    /// <inheritdoc cref="AbstractAODWaveformParam"/>
    /// <remarks>
    /// ChirpAOD波形生成参数, 在基类参数的基础上增加了<paramref name="SoundPacketLength"/>和<paramref name="SoundSpeed"/>
    /// </remarks>
    /// <param name="SoundPacketLength">音包长度(mm)</param>
    /// <param name="SoundSpeed">音速(mm/us)，固体声速更快 (默认: 5.742 mm/us)</param>
    public sealed record ChirpAODWaveformParam(
        double BandWidth,
        double CenterFrequency,
        double SoundPacketLength,
        double SoundSpeed,
        FunctionMonotonicTypeEnum FunctionMonotonicTypeEnum,
        double SampleRate,
        double Amplitude,
        string DirectoryPath,
        int ZeroSampleCount,
        int EndpointSampleCount,
        int GenerateRetryTimes,
        IReadOnlyList<AODWaveformOffsetConfiguration> OffsetConfigurations,
        IReadOnlyList<AODWaveformUniformityConfiguration> UniformityConfigurations,
        double SincCoefficient = 0d,
        double AstigmatismCompensationCoefficient = 0d,
        double SphericalAberrationCompensationCoefficient = 0d,
        double SecondaryAstigmatismCompensationCoefficient = 0d,
        double ComaCompensationCoefficient = 0d,
        double TrefoilCompensationCoefficient = 0d,
        double QuadrafoilCompensationCoefficient = 0d,
        double AlphaOrder = 0d,
        double AlphaOrderCoefficient = 0d) : AbstractAODWaveformParam(
        BandWidth,
        CenterFrequency,
        SoundSpeed > 0d ? Math.Round(SoundPacketLength / SoundSpeed * 1000d, MidpointRounding.AwayFromZero) : -1d, // (ns): mm/(mm/us) * 1000 = us * 1000 = ns
        FunctionMonotonicTypeEnum,
        SampleRate,
        Amplitude,
        DirectoryPath,
        ZeroSampleCount,
        EndpointSampleCount,
        GenerateRetryTimes,
        OffsetConfigurations,
        UniformityConfigurations,
        SincCoefficient,
        AstigmatismCompensationCoefficient,
        SphericalAberrationCompensationCoefficient,
        SecondaryAstigmatismCompensationCoefficient,
        ComaCompensationCoefficient,
        TrefoilCompensationCoefficient,
        QuadrafoilCompensationCoefficient,
        AlphaOrder,
        AlphaOrderCoefficient)
    {
        internal override void Validate()
        {
            Guard.IsGreaterThan(SoundPacketLength, 0d, nameof(SoundPacketLength));
            Guard.IsGreaterThan(SoundSpeed, 0d, nameof(SoundSpeed));

            base.Validate();
        }
    }

    /// <summary>
    /// AOD波形生成参数 [中心频率 - 带宽/2, 中心频率 + 带宽/2]
    /// </summary>
    /// <param name="BandWidth">带宽(MHz)</param>
    /// <param name="CenterFrequency">中心频率(Mhz)</param>
    /// <param name="FlatnessTime">平坦时间(ns)</param>
    /// <param name="FunctionMonotonicTypeEnum">递增, 递减, 平坦</param>
    /// <param name="SampleRate">采样率(Msa/s)</param>
    /// <param name="Amplitude">幅值</param>
    /// <param name="DirectoryPath">生成的目录</param>
    /// <param name="ZeroSampleCount">前面添加多少补零采样点个数, 相当于添加延迟(sa)</param>
    /// <param name="EndpointSampleCount">端点头尾添加多少采样点个数, 缓冲(XTC响应不够)(sa)</param>
    /// <param name="GenerateRetryTimes">生成AOD波形文件重试次数</param>
    /// <param name="OffsetConfigurations">AOD波形的频率偏移配置集合</param>
    /// <param name="UniformityConfigurations">AOD波形频率均匀性配置集合</param>
    /// <param name="SincCoefficient">sin(cx)/cx</param>
    /// <param name="AstigmatismCompensationCoefficient">二次补偿系数t^2 散光</param>
    /// <param name="SphericalAberrationCompensationCoefficient">三次补偿系数t^3 球差</param>
    /// <param name="SecondaryAstigmatismCompensationCoefficient">四次补偿系数t^4 二阶散光</param>
    /// <param name="ComaCompensationCoefficient">sin(2πt/T)</param>
    /// <param name="TrefoilCompensationCoefficient">sin(6πt/T)</param>
    /// <param name="QuadrafoilCompensationCoefficient">sin(8πt/T)</param>
    /// <param name="AlphaOrder">α次补偿</param>
    /// <param name="AlphaOrderCoefficient">α次补偿系数t^α</param>
    public abstract record AbstractAODWaveformParam(
        double BandWidth,
        double CenterFrequency,
        double FlatnessTime,
        FunctionMonotonicTypeEnum FunctionMonotonicTypeEnum,
        double SampleRate,
        double Amplitude,
        string DirectoryPath,
        int ZeroSampleCount,
        int EndpointSampleCount,
        int GenerateRetryTimes,
        IReadOnlyList<AODWaveformOffsetConfiguration> OffsetConfigurations,
        IReadOnlyList<AODWaveformUniformityConfiguration> UniformityConfigurations,
        double SincCoefficient,
        double AstigmatismCompensationCoefficient,
        double SphericalAberrationCompensationCoefficient,
        double SecondaryAstigmatismCompensationCoefficient,
        double ComaCompensationCoefficient,
        double TrefoilCompensationCoefficient,
        double QuadrafoilCompensationCoefficient,
        double AlphaOrder,
        double AlphaOrderCoefficient)
    {
        public readonly double LowFrequency = FunctionMonotonicTypeEnum switch // 低频
        {
            FunctionMonotonicTypeEnum.Increasing or FunctionMonotonicTypeEnum.Deceasing => CenterFrequency - BandWidth / 2d,
            FunctionMonotonicTypeEnum.Flatness => CenterFrequency,
            _ => ThrowHelper.ThrowArgumentOutOfRangeException<double>(nameof(FunctionMonotonicTypeEnum))
        };

        public readonly double HighFrequency = FunctionMonotonicTypeEnum switch // 高频
        {
            FunctionMonotonicTypeEnum.Increasing or FunctionMonotonicTypeEnum.Deceasing => CenterFrequency + BandWidth / 2d,
            FunctionMonotonicTypeEnum.Flatness => CenterFrequency,
            _ => ThrowHelper.ThrowArgumentOutOfRangeException<double>(nameof(FunctionMonotonicTypeEnum))
        };

        public readonly int NumberOfSamples = (int)Math.Round(FlatnessTime * SampleRate / 1000d + 2d * EndpointSampleCount, MidpointRounding.AwayFromZero); // 总采样点的个数: ns * (Msa/s) / 1000 = ns * (Gsa/s) = (10^-9s)*(10^9sa/s) = sa

        /// <summary>
        /// 包含在波形文件名中的可选标识
        /// </summary>
        public string FileNameSuffix { get; init; } = string.Empty;

        internal virtual void Validate()
        {
            Guard.IsGreaterThanOrEqualTo(BandWidth, 0d, nameof(BandWidth));
            Guard.IsGreaterThan(CenterFrequency, 0d, nameof(CenterFrequency));
            Guard.IsGreaterThan(FlatnessTime, 0d, nameof(FlatnessTime));

            Guard.IsGreaterThan(SampleRate, 0d, nameof(SampleRate));
            Guard.IsBetweenOrEqualTo(Amplitude, 0d, 1d, nameof(Amplitude));
            Guard.IsNotNullOrWhiteSpace(DirectoryPath, nameof(DirectoryPath));

            foreach (var item in OffsetConfigurations) item.Validate();
            foreach (var item in UniformityConfigurations) item.Validate();

            Guard.IsTrue(EnumerableHelper.IsStrictlyIncreasing(UniformityConfigurations.Select(t => t.Frequency)), "Uniformity Configurations must be sorted by Frequency");

            Guard.IsGreaterThanOrEqualTo(ZeroSampleCount, 0d, nameof(ZeroSampleCount));
            Guard.IsGreaterThanOrEqualTo(EndpointSampleCount, 0d, nameof(EndpointSampleCount));
            Guard.IsGreaterThan(GenerateRetryTimes, 0d, nameof(GenerateRetryTimes));

            if (FunctionMonotonicTypeEnum is FunctionMonotonicTypeEnum.Flatness)
            {
                if(BandWidth != 0d) ThrowHelper.ThrowArgumentException(nameof(BandWidth), "if monotonic is flatness then band Width is must 0");
                if(SincCoefficient != 0d) ThrowHelper.ThrowArgumentException(nameof(SincCoefficient), "if monotonic is flatness then Sinc Coefficient is must 0");
                if(AstigmatismCompensationCoefficient != 0d) ThrowHelper.ThrowArgumentException(nameof(AstigmatismCompensationCoefficient), "if monotonic is flatness then Astigmatism Compensation Coefficient is must 0");
                if(SphericalAberrationCompensationCoefficient != 0d) ThrowHelper.ThrowArgumentException(nameof(SphericalAberrationCompensationCoefficient), "if monotonic is flatness then Spherical Aberration Compensation Coefficient is must 0");
                if(SecondaryAstigmatismCompensationCoefficient != 0d) ThrowHelper.ThrowArgumentException(nameof(SecondaryAstigmatismCompensationCoefficient), "if monotonic is flatness then Secondary Astigmatism Compensation Coefficient is must 0");
                if(ComaCompensationCoefficient != 0d) ThrowHelper.ThrowArgumentException(nameof(ComaCompensationCoefficient), "if monotonic is flatness then Coma Compensation Coefficient is must 0");
                if(TrefoilCompensationCoefficient != 0d) ThrowHelper.ThrowArgumentException(nameof(TrefoilCompensationCoefficient), "if monotonic is flatness then Trefoil Compensation Coefficient is must 0");
                if(QuadrafoilCompensationCoefficient != 0d) ThrowHelper.ThrowArgumentException(nameof(QuadrafoilCompensationCoefficient), "if monotonic is flatness then Quadrafoil Compensation Coefficient is must 0");
                if(AlphaOrder != 0d) ThrowHelper.ThrowArgumentException(nameof(AlphaOrder), "if monotonic is flatness then Alpha Order is must 0");
                if(AlphaOrderCoefficient  != 0d) ThrowHelper.ThrowArgumentException(nameof(AlphaOrderCoefficient), "if monotonic is flatness then Alpha Order Coefficient is must 0");
            }
            if (BandWidth == 0d && FunctionMonotonicTypeEnum is not FunctionMonotonicTypeEnum.Flatness) ThrowHelper.ThrowArgumentException(nameof(FunctionMonotonicTypeEnum), "if band Width is must 0 then monotonic is flatness and band");
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
            var fileName = $"prescan" +
                           $"_{Param.FileNameSuffix}" +
                           $"{Id}" +
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

            FilePath = FileHelper.GetEnsureLongPathSupport(FileHelper.RemoveInvalidFileName(Path.Combine(Param.DirectoryPath, fileName)));

            var itemList = new List<AODWaveformResultItem>();
            foreach (var item in Param.OffsetConfigurations)
            {
                fileName = $"prescan" +
                           $"_{Param.FileNameSuffix}" +
                           $"{Id}" +
                           $"${Param.NumberOfSamples + Param.ZeroSampleCount}${Param.ZeroSampleCount}$600$02${item.OffsetFrequency:0.###}${item.OffsetFrequencyPeriodCoefficient:0.###}$.txt";

                if (IsSuccess == false) fileName = $"ERROR_{fileName}";

                itemList.Add(new AODWaveformResultItem(item, FileHelper.GetEnsureLongPathSupport(FileHelper.RemoveInvalidFileName(Path.Combine(Param.DirectoryPath, item.DirectoryName, fileName)))));
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
            var fileName = $"chirp" +
                           $"_{Param.FileNameSuffix}" +
                           $"{Id}" +
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

            FilePath = FileHelper.GetEnsureLongPathSupport(FileHelper.RemoveInvalidFileName(Path.Combine(Param.DirectoryPath, fileName)));

            var itemList = new List<AODWaveformResultItem>();
            foreach (var item in Param.OffsetConfigurations)
            {
                fileName = $"chirp" +
                           $"_{Param.FileNameSuffix}" +
                           $"{Id}" +
                           $"${Param.NumberOfSamples + Param.ZeroSampleCount}${Param.ZeroSampleCount}$600$03${item.OffsetFrequency:0.###}${item.OffsetFrequencyPeriodCoefficient:0.###}$.txt";
                if (IsSuccess == false) fileName = $"ERROR_{fileName}";

                itemList.Add(new AODWaveformResultItem(item, FileHelper.GetEnsureLongPathSupport(FileHelper.RemoveInvalidFileName(Path.Combine(Param.DirectoryPath, item.DirectoryName, fileName)))));
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
        protected readonly string Id = Guid.NewGuid().ToString("N");

        /// <summary>
        /// AOD波形结果项集合
        /// </summary>
        public IReadOnlyList<AODWaveformResultItem> Items { get; protected set; } = [];

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
        public IReadOnlyList<Point> Signals { get; internal set; } = [];

        /// <summary>
        /// AOD波形信号FFT分析
        ///</summary>
        public IReadOnlyList<Point> FFTSignals { get; internal set; } = [];

        /// <summary>
        /// AOD波形频率补偿信号
        ///</summary>
        public IReadOnlyList<Point> FrequencyCoefficients { get; internal set; } = [];

        /// <summary>
        /// AOD波形线性频率信号
        ///</summary>
        public IReadOnlyList<Point> FlatnessLinearFrequencySignals { get; internal set; } = [];

        /// <summary>
        /// AOD波形非线性频率信号
        ///</summary>
        public IReadOnlyList<Point> FlatnessTotalFrequencySignals { get; internal set; } = [];

        /// <summary>
        /// AOD波形总频率信号
        ///</summary>
        public IReadOnlyList<Point> FlatnessAstigmatismCompensationSignals { get; internal set; } = [];

        /// <summary>
        /// AOD波形线性补偿信号
        ///</summary>
        public IReadOnlyList<Point> FlatnessSphericalAberrationCompensationSignals { get; internal set; } = [];

        /// <summary>
        /// AOD波形散光补偿信号
        ///</summary>
        public IReadOnlyList<Point> FlatnessSecondaryAstigmatismCompensationSignals { get; internal set; } = [];

        /// <summary>
        /// AOD波形球差补偿信号
        ///</summary>
        public IReadOnlyList<Point> FlatnessComaCompensationSignals { get; internal set; } = [];

        /// <summary>
        /// AOD波形二阶散光补偿信号
        ///</summary>
        public IReadOnlyList<Point> FlatnessTrefoilCompensationSignals { get; internal set; } = [];

        /// <summary>  
        /// AOD波形coma补偿信号
        ///</summary>
        public IReadOnlyList<Point> FlatnessQuadrafoilCompensationSignals { get; internal set; } = [];

        /// <summary>
        /// AOD波形trefoil补偿信号
        ///</summary>
        public IReadOnlyList<Point> FlatnessAlphaOrderCompensationSignals { get; internal set; } = [];
    }

    #endregion 结果

    /// <summary>
    /// 生成PrescanAOD波形文件
    /// </summary>
    /// <param name="prescanAODWaveformParam">PrescanAOD波形生成参数</param>
    /// <returns>(是否成功, 异常信息, 结果)</returns>
    public static (bool IsSuccess, Exception? Exception, PrescanAODWaveformResult AODWaveformResult) GeneratePrescanAODWaveformFile(PrescanAODWaveformParam prescanAODWaveformParam) => GenerateAODWaveformFile<PrescanAODWaveformResult, PrescanAODWaveformParam>(prescanAODWaveformParam);

    /// <summary>
    /// 生成ChirpAOD波形文件
    /// </summary>
    /// <param name="chirpAODWaveformParam">ChirpAOD波形生成参数</param>
    /// <returns>(是否成功, 异常信息, 结果)</returns>
    public static (bool IsSuccess, Exception? Exception, ChirpAODWaveformResult AODWaveformResult) GenerateChirpAODWaveformFile(ChirpAODWaveformParam chirpAODWaveformParam) => GenerateAODWaveformFile<ChirpAODWaveformResult, ChirpAODWaveformParam>(chirpAODWaveformParam);

    /// <summary>
    /// 生成AOD波形文件
    /// </summary>
    /// <param name="param">AOD波形生成参数</param>
    /// <returns>(是否成功, 异常信息, 结果)</returns>
    private static (bool IsSuccess, Exception? Exception, TResult AODWaveformResult) GenerateAODWaveformFile<TResult, TParam>(TParam param)
        where TResult : AbstractAODWaveformResult<TParam>
        where TParam : AbstractAODWaveformParam
    {
        param.Validate();

        var centerFrequency = param.CenterFrequency;
        var bandWidth = param.BandWidth;
        var lowFrequency = param.LowFrequency;
        var highFrequency = param.HighFrequency;

        #region 返回结果

        var dt = 1d / param.SampleRate; // 每个采样点的时间间隔 (us/sa): 1 / (Msa/s) = 10^-6s/sa = us/sa

        var allSampleIndices = GenerateUtils.LinearIndexRange(0, param.NumberOfSamples - 1);
        var headerSampleIndices = GenerateUtils.LinearIndexRange(0, param.EndpointSampleCount - 1);
        var flatnessSampleIndices = GenerateUtils.LinearIndexRange(param.EndpointSampleCount, param.NumberOfSamples - param.EndpointSampleCount - 1);
        var footerSampleIndices = GenerateUtils.LinearIndexRange(param.NumberOfSamples - param.EndpointSampleCount, param.NumberOfSamples - 1);

        var aodWaveformSignals = Vector<double>.Build.Dense(param.NumberOfSamples);

        var dLinearFrequencies = Vector<double>.Build.Dense(0);
        var dAstigmatismFrequencies = Vector<double>.Build.Dense(0);
        var dSphericalAberrationFrequencies = Vector<double>.Build.Dense(0);
        var dSecondaryAstigmatismFrequencies = Vector<double>.Build.Dense(0);
        var dComaFrequencies = Vector<double>.Build.Dense(0);
        var dTrefoilFrequencies = Vector<double>.Build.Dense(0);
        var dQuadrafoilFrequencies = Vector<double>.Build.Dense(0);
        var dAlphaOrderFrequencies = Vector<double>.Build.Dense(0);
        var dHeaderFrequencies = Vector<double>.Build.Dense(0);
        var dFlatnessFrequencies = Vector<double>.Build.Dense(0);
        var dFooterFrequencies = Vector<double>.Build.Dense(0);

        var minFlatnessFrequency = 0d;
        var maxFlatnessFrequency = 0d;

        Vector<Complex> fftResult;
        Vector<double> fftFrequencies;
        Vector<double> fftMagnitudes;
        bool isSuccess;
        Exception? exception = null;

        #endregion 返回结果

        var count = 1;
        while (true)
        {
            try
            {
                #region 频率

                var t = (Vector<double>.Build.DenseOfArray(flatnessSampleIndices) - flatnessSampleIndices[0]) * dt;

                switch (param.FunctionMonotonicTypeEnum)
                {
                    case FunctionMonotonicTypeEnum.Increasing:
                    case FunctionMonotonicTypeEnum.Deceasing:
                        dLinearFrequencies = bandWidth / t.Maximum() * t;
                        dAstigmatismFrequencies = param.AstigmatismCompensationCoefficient * t.PointwisePower(2d);
                        dSphericalAberrationFrequencies = param.SphericalAberrationCompensationCoefficient * t.PointwisePower(3d);
                        dSecondaryAstigmatismFrequencies = param.SecondaryAstigmatismCompensationCoefficient * t.PointwisePower(4d);
                        dComaFrequencies = param.ComaCompensationCoefficient * (2d * Math.PI * t / t.Maximum()).PointwiseSin();
                        dTrefoilFrequencies = param.TrefoilCompensationCoefficient * (6d * Math.PI * t / t.Maximum()).PointwiseSin();
                        dQuadrafoilFrequencies = param.QuadrafoilCompensationCoefficient * (8d * Math.PI * t / t.Maximum()).PointwiseSin();
                        dAlphaOrderFrequencies = param.AlphaOrderCoefficient * t.PointwisePower(param.AlphaOrder);

                        break;

                    case FunctionMonotonicTypeEnum.Flatness:
                    default:
                        dLinearFrequencies = Vector<double>.Build.Dense(t.Count, 0d);
                        dAstigmatismFrequencies = Vector<double>.Build.Dense(t.Count, 0d);
                        dSphericalAberrationFrequencies = Vector<double>.Build.Dense(t.Count, 0d);
                        dSecondaryAstigmatismFrequencies = Vector<double>.Build.Dense(t.Count, 0d);
                        dComaFrequencies = Vector<double>.Build.Dense(t.Count, 0d);
                        dTrefoilFrequencies = Vector<double>.Build.Dense(t.Count, 0d);
                        dQuadrafoilFrequencies = Vector<double>.Build.Dense(t.Count, 0d);
                        dAlphaOrderFrequencies = Vector<double>.Build.Dense(t.Count, 0d);

                        break;
                }

                dHeaderFrequencies = param.FunctionMonotonicTypeEnum switch
                {
                    FunctionMonotonicTypeEnum.Increasing => Vector<double>.Build.Dense(headerSampleIndices.Length, lowFrequency),
                    FunctionMonotonicTypeEnum.Deceasing => Vector<double>.Build.Dense(headerSampleIndices.Length, highFrequency),
                    FunctionMonotonicTypeEnum.Flatness => Vector<double>.Build.Dense(headerSampleIndices.Length, centerFrequency),
                    _ => ThrowHelper.ThrowArgumentOutOfRangeException<Vector<double>>(nameof(param.FunctionMonotonicTypeEnum))
                };

                dFooterFrequencies = param.FunctionMonotonicTypeEnum switch
                {
                    FunctionMonotonicTypeEnum.Increasing => Vector<double>.Build.Dense(footerSampleIndices.Length, highFrequency),
                    FunctionMonotonicTypeEnum.Deceasing => Vector<double>.Build.Dense(footerSampleIndices.Length, lowFrequency),
                    FunctionMonotonicTypeEnum.Flatness => Vector<double>.Build.Dense(footerSampleIndices.Length, centerFrequency),
                    _ => ThrowHelper.ThrowArgumentOutOfRangeException<Vector<double>>(nameof(param.FunctionMonotonicTypeEnum))
                };

                dFlatnessFrequencies = param.FunctionMonotonicTypeEnum switch
                {
                    FunctionMonotonicTypeEnum.Increasing => lowFrequency + dLinearFrequencies + dAstigmatismFrequencies + dSphericalAberrationFrequencies + dSecondaryAstigmatismFrequencies + dComaFrequencies + dTrefoilFrequencies + dQuadrafoilFrequencies + dAlphaOrderFrequencies,
                    FunctionMonotonicTypeEnum.Deceasing => highFrequency - dLinearFrequencies - dAstigmatismFrequencies - dSphericalAberrationFrequencies - dSecondaryAstigmatismFrequencies - dComaFrequencies - dTrefoilFrequencies - dQuadrafoilFrequencies - dAlphaOrderFrequencies,
                    FunctionMonotonicTypeEnum.Flatness => Vector<double>.Build.Dense(flatnessSampleIndices.Length, centerFrequency),
                    _ => ThrowHelper.ThrowArgumentOutOfRangeException<Vector<double>>(nameof(param.FunctionMonotonicTypeEnum))
                };

                if (dFlatnessFrequencies.Exists(f => f <= 0 || f > param.HighFrequency * 1.5d)) ThrowHelper.ThrowArgumentException(nameof(dFlatnessFrequencies), $"Synthesized frequencies must be in the range (0, {param.HighFrequency * 1.5d:f3} MHz)");

                #endregion 频率

                FFT(0d, 0d);

                #region 平坦部分的起始和终止频率

                var fftDerivative = fftMagnitudes.Differentiate() / fftFrequencies.Differentiate();
                var fftDerivativeFrequencies = fftFrequencies.SubVectorRange(0, fftFrequencies.Count - 2);

                // 寻找极值点
                const double threshold = 0.0001d;
                var indices = fftDerivative.FindAbsAbove(threshold);
                var headerIndices = GenerateUtils.LinearIndexRange(0, indices[0] - 1);
                var flatnessIndices = GenerateUtils.LinearIndexRange(indices[0], indices[^1]);
                var footerIndices = GenerateUtils.LinearIndexRange(indices[^1] + 1, fftDerivative.Count - 1);

                var fftDerivativeSign = Vector<double>.Build.SameAs(fftDerivative);
                fftDerivativeSign.SetByIndices(flatnessIndices, fftDerivative.GetByIndices(flatnessIndices).PointwiseSign());
                fftDerivativeSign.SetByIndices(headerIndices, Vector<double>.Build.Dense(headerIndices.Length, fftDerivativeSign[flatnessIndices[0]]));
                fftDerivativeSign.SetByIndices(footerIndices, Vector<double>.Build.Dense(footerIndices.Length, fftDerivativeSign[flatnessIndices[^1]]));

                var fftExtremumPointIndices = fftDerivativeSign.Differentiate().FindAll(d => d != 0); // 通过sign寻找极值点, 极值点的索引都提前了1个点
                var fftExtremumPointFrequencies = fftFrequencies.GetByIndices([.. fftExtremumPointIndices.Select(d => d + 1)]);
                var readonlyMinFrequency = minFlatnessFrequency = fftExtremumPointFrequencies[0];
                var readonlyMaxFrequency = maxFlatnessFrequency = fftExtremumPointFrequencies[^1];

                // 寻找拐点
                var fftSecondDerivative = fftDerivative.Differentiate() / fftDerivativeFrequencies.Differentiate();

                indices = fftSecondDerivative.FindAbsAbove(0.0001d);
                headerIndices = GenerateUtils.LinearIndexRange(0, indices[0] - 1);
                flatnessIndices = GenerateUtils.LinearIndexRange(indices[0], indices[^1]);
                footerIndices = GenerateUtils.LinearIndexRange(indices[^1] + 1, fftSecondDerivative.Count - 1);

                var fftSecondDerivativeSign = Vector<double>.Build.SameAs(fftSecondDerivative);
                fftSecondDerivativeSign.SetByIndices(flatnessIndices, fftSecondDerivative.GetByIndices(flatnessIndices).PointwiseSign());
                fftSecondDerivativeSign.SetByIndices(headerIndices, Vector<double>.Build.Dense(headerIndices.Length, fftSecondDerivativeSign[flatnessIndices[0]]));
                fftSecondDerivativeSign.SetByIndices(footerIndices, Vector<double>.Build.Dense(footerIndices.Length, fftSecondDerivativeSign[flatnessIndices[^1]]));

                var fftInflectionPointIndices = fftSecondDerivativeSign.Differentiate().FindAll(d => d != 0); // 通过sign寻找拐点点, 极值点的索引都提前了1个点
                var fftInflectionPointFrequencies = fftFrequencies.GetByIndices([.. fftInflectionPointIndices.Select(d => d + 1)]);

                switch (param.FunctionMonotonicTypeEnum)
                {
                    case FunctionMonotonicTypeEnum.Flatness:
                        if (Math.Abs(minFlatnessFrequency - maxFlatnessFrequency) > Constants.Tolerance) ThrowHelper.ThrowArgumentException("leftFreq != rightFreq");
                        break;

                    case FunctionMonotonicTypeEnum.Deceasing:
                    case FunctionMonotonicTypeEnum.Increasing:
                        var leftIndex = fftInflectionPointFrequencies.Find(d => d < readonlyMinFrequency);
                        var rightIndex = fftInflectionPointFrequencies.FindLast(d => d > readonlyMaxFrequency);

                        minFlatnessFrequency = leftIndex?.Item2 ?? double.NaN;
                        maxFlatnessFrequency = rightIndex?.Item2 ?? double.NaN;
                        if (double.IsNaN(minFlatnessFrequency) || double.IsNaN(maxFlatnessFrequency)) ThrowHelper.ThrowArgumentException("leftFreq or rightFreq is NaN");

                        break;

                    default:
                        ThrowHelper.ThrowArgumentOutOfRangeException(nameof(param.FunctionMonotonicTypeEnum));
                        break;
                }

                #endregion 平坦部分的起始和终止频率

                #region 修正

                if (param.FunctionMonotonicTypeEnum == FunctionMonotonicTypeEnum.Flatness)
                {
                    if (Math.Abs(minFlatnessFrequency - param.CenterFrequency) < param.SampleRate / flatnessSampleIndices.Length)
                    {
                        isSuccess = true;
                        break;
                    }

                    if (minFlatnessFrequency < param.CenterFrequency)
                    {
                        centerFrequency += param.SampleRate / (2d * flatnessSampleIndices.Length);
                    }
                    else
                    {
                        centerFrequency -= param.SampleRate / (2d * flatnessSampleIndices.Length);
                    }
                }
                else if (param.FunctionMonotonicTypeEnum == FunctionMonotonicTypeEnum.Increasing)
                {
                    if (Math.Abs(minFlatnessFrequency - param.LowFrequency) < param.SampleRate / flatnessSampleIndices.Length)
                    {
                        if (Math.Abs(maxFlatnessFrequency - param.HighFrequency) < param.SampleRate / flatnessSampleIndices.Length)
                        {
                            isSuccess = true;
                            break;
                        }

                        if (maxFlatnessFrequency < param.HighFrequency)
                            bandWidth += param.SampleRate / (2d * flatnessSampleIndices.Length);
                        else
                            bandWidth -= param.SampleRate / (2d * flatnessSampleIndices.Length);
                    }
                    else if (minFlatnessFrequency < param.LowFrequency)
                        lowFrequency += param.SampleRate / (2d * flatnessSampleIndices.Length);
                    else
                        lowFrequency -= param.SampleRate / (2d * flatnessSampleIndices.Length);
                }
                else if (param.FunctionMonotonicTypeEnum == FunctionMonotonicTypeEnum.Deceasing)
                {
                    if (Math.Abs(maxFlatnessFrequency - param.HighFrequency) < param.SampleRate / flatnessSampleIndices.Length)
                    {
                        if (Math.Abs(minFlatnessFrequency - param.LowFrequency) < param.SampleRate / flatnessSampleIndices.Length)
                        {
                            isSuccess = true;
                            break;
                        }

                        if (minFlatnessFrequency < param.LowFrequency)
                            bandWidth -= param.SampleRate / (2d * flatnessSampleIndices.Length);
                        else
                            bandWidth += param.SampleRate / (2d * flatnessSampleIndices.Length);
                    }
                    else if (maxFlatnessFrequency < param.HighFrequency)
                        highFrequency += param.SampleRate / (2d * flatnessSampleIndices.Length);
                    else
                        highFrequency -= param.SampleRate / (2d * flatnessSampleIndices.Length);
                }
                else
                {
                    ThrowHelper.ThrowArgumentOutOfRangeException(nameof(param.FunctionMonotonicTypeEnum));
                }

                #endregion 修正

                if (++count > param.GenerateRetryTimes) ThrowHelper.ThrowInvalidOperationException("AOD waveforms could not be generated");
            }
            catch (Exception ex)
            {
                exception = ex;
                isSuccess = false;
                break;
            }
        }

        var result = GuardUtils.IsNotNullAndAssignableToType<TResult>(Activator.CreateInstance(typeof(TResult), param, isSuccess));
        result.Initialize();

        foreach (var item in result.Items)
        {
            FFT(item.OffsetConfiguration.OffsetFrequency, item.OffsetConfiguration.OffsetFrequencyPeriodCoefficient);
            var frequencyCoefficientList = new List<Point>();

            if (isSuccess && param.FunctionMonotonicTypeEnum != FunctionMonotonicTypeEnum.Flatness && (param.UniformityConfigurations.Count > 0 || param.SincCoefficient != 0d))
            {
                var fftFullFrequencies = fftResult.GetFullFrequencies(param.SampleRate);
                var indices = fftFullFrequencies.FindAll(d => minFlatnessFrequency <= Math.Abs(d) && Math.Abs(d) <= maxFlatnessFrequency);
                if (param.UniformityConfigurations.Count > 0)
                {
                    foreach (var index in indices)
                    {
                        var f = Math.Abs(fftFullFrequencies[index]);
                        var compensation = BinarySearch.TryValueIndexRange(
                            [.. param.UniformityConfigurations.Select(tt => tt.Frequency)],
                            f,
                            out var startColumnIndex,
                            out var endColumnIndex)
                            ? Interpolator.Linear(param.UniformityConfigurations[startColumnIndex].ToPoint(), param.UniformityConfigurations[endColumnIndex].ToPoint(), f)
                            : f <= param.UniformityConfigurations.First().Frequency
                                ? param.UniformityConfigurations.First().Coefficient
                                : param.UniformityConfigurations.Last().Coefficient;

                        fftResult[index] *= compensation;
                        if (fftFullFrequencies[index] >= 0d) frequencyCoefficientList.Add(new Point(f, compensation));
                    }
                }
                else
                {
                    var compensations = Vector<double>.Build.Dense(indices.Length);
                    var flatnessBandwidth = maxFlatnessFrequency - minFlatnessFrequency;
                    var flatnessCenterFrequency = minFlatnessFrequency + flatnessBandwidth / 2d;
                    for (var i = 0; i < indices.Length; i++)
                    {
                        var index = indices[i];
                        var f = Math.Abs(fftFullFrequencies[index]);
                        var x = param.SincCoefficient * (f - flatnessCenterFrequency) / flatnessBandwidth;
                        var compensation = x == 0d
                            ? 1d
                            : 1d / (Math.Sin(x) / x + 1e-10d);
                        compensations[i] = compensation;
                    }

                    compensations /= compensations.AbsoluteMaximum();

                    for (var i = 0; i < indices.Length; i++)
                    {
                        var index = indices[i];
                        var f = Math.Abs(fftFullFrequencies[index]);

                        var compensation = compensations[i];
                        fftResult[index] *= compensation;

                        if (fftFullFrequencies[index] >= 0d) frequencyCoefficientList.Add(new Point(f, compensation));
                    }
                }

                var recoveredSignal = fftResult.InverseFastFourierTransform().Real();
                if (recoveredSignal.AbsoluteMaximum() > param.Amplitude)
                    recoveredSignal = recoveredSignal / recoveredSignal.AbsoluteMaximum() * param.Amplitude;
                aodWaveformSignals.SetSubVectorRange(flatnessSampleIndices[0], flatnessSampleIndices[^1], recoveredSignal);

                fftResult = recoveredSignal.ToComplex().FastFourierTransform();
                (fftFrequencies, fftMagnitudes) = fftResult.GetPositiveFrequencies(param.SampleRate);
            }

            /*
             * double[-1,1]归一化数据需要转换为16-bit或32-bit整数格式进行传输[DSP、FPGA、DAC数字信号转换为模拟信号, 目前这个是16-bit PCM(脉冲编码调制)格式
             * 1. aodWaveformSignal ∈ [-1, 1] 归一化
             * 2. 放大到 [-2^15, 2^15 - 1] 之间的16位整数(Int16的取值范围)
             *          - 防止溢出的四舍五入[-1，1] * 2^15 ∈ [-2^15, 2^15 - 1]
             *          1.0  :  +32767（避免溢出到 +32768，因为 Int16 最高是 32767）
             *          0.0  :  0
             *          -1.0 :  -32768
             * 3. Int16取值范围内, < 0: 负数在二进制补码表示下，等价于加 2^32
             *          - Int16的负数补码
             *          ((short)-900).ToString("X4")                        : FC7C
             *          ((long)-900).ToString("X4")                         : FFFFFC7C
             *          ((long)-900 + (long)Math.Pow(2, 32)).ToString("X4") : FFFFFC7C
             * 4. 获取Int16所有的补码, 只会有4位
             * 5. Excel拷贝txt显示曲线:
             *          一、公式: =(HEX2DEC(A1)-IF(HEX2DEC(A1)>32768,65536,0))/POWER(2,15)
             *          二、不要[下拉填充点]直接推拽下拉太慢, 快速公式下拉填充: 直接双击[下拉填充点]一行直接生成
             */

            // 将结果转换为16位整数并保存到文件
            var aodWaveformSignalResult = aodWaveformSignals
                .Select(t => ConvertUtils.ToInt16NotOverflowException(Math.Round(Math.Pow(2d, 15d) * t, MidpointRounding.AwayFromZero)))
                .Select(Convert.ToInt64)
                .Select(t => t < 0 ? t + (long)Math.Pow(2d, 32d) : t)
                .ToArray();
            var hexStrings = aodWaveformSignalResult
                .Select(t => t.ToString("X4")) // 转换为16进制补码字符串, 至少4位不足右边补0
                .Select(t => t[^4..]) // 字符串的最后 4 位
                .ToArray();

            DirectoryHelper.CreateFileDirectoryIfNotExists(item.FilePath);
            FileHelper.DeleteFileIfExists(item.FilePath);
            File.WriteAllText(item.FilePath, string.Join(Environment.NewLine, hexStrings));

            item.Signals = [..allSampleIndices.Select(t => new Point(t + 1d, aodWaveformSignals[t]))];
            item.FFTSignals = [..fftFrequencies.Zip(fftMagnitudes, (t1, t2) => new Point(t1, t2))];
            item.FrequencyCoefficients = [..frequencyCoefficientList];
            item.FlatnessLinearFrequencySignals = [..flatnessSampleIndices.Select((t, index) => new Point(t + 1d, dLinearFrequencies[index]))];
            item.FlatnessTotalFrequencySignals = [..flatnessSampleIndices.Select((t, index) => new Point(t + 1d, dFlatnessFrequencies[index]))];
            item.FlatnessAstigmatismCompensationSignals = [..flatnessSampleIndices.Select((t, index) => new Point(t + 1d, dAstigmatismFrequencies[index]))];
            item.FlatnessSphericalAberrationCompensationSignals = [..flatnessSampleIndices.Select((t, index) => new Point(t + 1d, dSphericalAberrationFrequencies[index]))];
            item.FlatnessSecondaryAstigmatismCompensationSignals = [..flatnessSampleIndices.Select((t, index) => new Point(t + 1d, dSecondaryAstigmatismFrequencies[index]))];
            item.FlatnessComaCompensationSignals = [..flatnessSampleIndices.Select((t, index) => new Point(t + 1d, dComaFrequencies[index]))];
            item.FlatnessTrefoilCompensationSignals = [..flatnessSampleIndices.Select((t, index) => new Point(t + 1d, dTrefoilFrequencies[index]))];
            item.FlatnessQuadrafoilCompensationSignals = [..flatnessSampleIndices.Select((t, index) => new Point(t + 1d, dQuadrafoilFrequencies[index]))];
            item.FlatnessAlphaOrderCompensationSignals = [..flatnessSampleIndices.Select((t, index) => new Point(t + 1d, dAlphaOrderFrequencies[index]))];
        }

        DirectoryHelper.CreateFileDirectoryIfNotExists(result.FilePath);
        FileHelper.DeleteFileIfExists(result.FilePath);
        FileHelper.SerializeOperate(result, result.FilePath);

        return (
            isSuccess,
            exception,
            result
        );

        void FFT(double offsetFrequency, double offsetFrequencyPeriodCoefficient)
        {
            #region 相位

            var dHeaderPhases = 2d * Math.PI * dHeaderFrequencies * dt;
            var headerPhases = dHeaderPhases.IntegrateCumulative();
            var dFlatnessPhases = 2d * Math.PI * dFlatnessFrequencies * dt;
            var flatnessPhases = dFlatnessPhases.IntegrateCumulative();
            if (offsetFrequency != 0d && offsetFrequencyPeriodCoefficient != 0d) flatnessPhases += 2d * Math.PI * dFlatnessFrequencies * offsetFrequencyPeriodCoefficient * 1d / offsetFrequency;
            var dFooterPhases = 2d * Math.PI * dFooterFrequencies * dt;
            var footerPhases = dFooterPhases.IntegrateCumulative();

            #endregion 相位

            #region 波形

            if (headerSampleIndices.Length > 0) aodWaveformSignals.SetSubVectorRange(headerSampleIndices[0], headerSampleIndices[^1], param.Amplitude * headerPhases.PointwiseCos().PointwiseMultiply(Vector<double>.Build.DenseOfArray(headerSampleIndices) / headerSampleIndices.Length));
            aodWaveformSignals.SetSubVectorRange(flatnessSampleIndices[0], flatnessSampleIndices[^1], param.Amplitude * flatnessPhases.PointwiseCos());
            if (footerSampleIndices.Length > 0) aodWaveformSignals.SetSubVectorRange(footerSampleIndices[0], footerSampleIndices[^1], param.Amplitude * footerPhases.PointwiseCos().PointwiseMultiply(1d - (Vector<double>.Build.DenseOfArray(footerSampleIndices) - footerSampleIndices[0] + 1d) / footerSampleIndices.Length));

            #endregion 波形

            #region 傅里叶

            var flatnessAODWaveformSignals = aodWaveformSignals.SubVectorRange(flatnessSampleIndices[0], flatnessSampleIndices[^1]);
            fftResult = flatnessAODWaveformSignals.ToComplex().FastFourierTransform();
            (fftFrequencies, fftMagnitudes) = fftResult.GetPositiveFrequencies(param.SampleRate);

            #endregion 傅里叶
        }
    }
}