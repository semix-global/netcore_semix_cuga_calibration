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
    #region 参数

    /// <summary>
    /// AOD波形的频率偏移配置
    /// </summary>
    /// <param name="DirectoryName">文件夹名称</param>
    /// <param name="OffsetFrequency">偏移频率(Mhz)</param>
    /// <param name="OffsetFrequencyPeriodMultiple">偏移频率的2π周期的倍率</param>
    public sealed record GenerateAODWaveformConfiguration(
        string DirectoryName,
        double OffsetFrequency,
        double OffsetFrequencyPeriodMultiple)
    {
        public string FileNameWithoutExtension { get; internal set; } = string.Empty;

        internal void Validate()
        {
            Guard.IsNotNullOrWhiteSpace(DirectoryName, nameof(DirectoryName));
            Guard.IsGreaterThanOrEqualTo(OffsetFrequency, 0, nameof(OffsetFrequency));
        }
    }

    /// <inheritdoc cref="AbstractGenerateAODWaveformParam"/>
    /// <remarks>
    /// ChirpAOD波形生成参数, 在基类参数的基础上增加了<paramref name="SoundPacketLength"/>和<paramref name="AODSoundSpeed"/>
    /// </remarks>
    /// <param name="SoundPacketLength">音包长度(mm)</param>
    /// <param name="AODSoundSpeed">音速(mm/us)，固体声速更快 (默认: 5.742 mm/us)</param>
    public sealed record GenerateChirpAODWaveformParam(
        double BandWidth,
        double CenterFrequency,
        double SoundPacketLength,
        FunctionMonotonicTypeEnum FunctionMonotonicTypeEnum,
        double SampleRate,
        double Amplitude,
        string DirectoryPath,
        IReadOnlyList<GenerateAODWaveformConfiguration> Configurations,
        int ZeroSampleCount = 0,
        int EndpointSampleCount = 0,
        double AODSoundSpeed = 5.742,
        double SincCoefficient = 0,
        double AstigmatismCompensationCoefficient = 0,
        double SphericalAberrationCompensationCoefficient = 0,
        double SecondaryAstigmatismCompensationCoefficient = 0,
        double ComaCompensationCoefficient = 0,
        double TrefoilCompensationCoefficient = 0,
        double QuadrafoilCompensationCoefficient = 0,
        double AlphaOrder = 0,
        double AlphaOrderCoefficient = 0,
        Point[]? FrequencyAmplitudes = null,
        int GenerateRetryTimes = 1000) : AbstractGenerateAODWaveformParam(
        BandWidth,
        CenterFrequency,
        AODSoundSpeed > 0 ? Math.Round(SoundPacketLength / AODSoundSpeed * 1000d, MidpointRounding.AwayFromZero) : -1d, // (ns): mm/(mm/us) * 1000 = us * 1000 = ns
        FunctionMonotonicTypeEnum,
        SampleRate,
        Amplitude,
        DirectoryPath,
        Configurations,
        ZeroSampleCount,
        EndpointSampleCount,
        SincCoefficient,
        AstigmatismCompensationCoefficient,
        SphericalAberrationCompensationCoefficient,
        SecondaryAstigmatismCompensationCoefficient,
        ComaCompensationCoefficient,
        TrefoilCompensationCoefficient,
        QuadrafoilCompensationCoefficient,
        AlphaOrder,
        AlphaOrderCoefficient,
        FrequencyAmplitudes,
        GenerateRetryTimes)
    {
        internal override void UpdateConfigurations()
        {
            var frequencyFileName = FunctionMonotonicTypeEnum switch
            {
                FunctionMonotonicTypeEnum.Increasing => $"{LowFrequency:0.###}Mhz_{HighFrequency:0.###}Mhz",
                FunctionMonotonicTypeEnum.Deceasing => $"{HighFrequency:0.###}Mhz_{LowFrequency:0.###}Mhz",
                FunctionMonotonicTypeEnum.Flatness => $"{CenterFrequency:0.###}Mhz_{CenterFrequency:0.###}Mhz",
                _ => ThrowHelper.ThrowArgumentOutOfRangeException<string>(nameof(FunctionMonotonicTypeEnum))
            };

            foreach (var item in Configurations)
            {
                item.FileNameWithoutExtension = $"chirp" +
                                                $"_{SoundPacketLength:0.###}mm" +
                                                $"_{frequencyFileName}" +
                                                $"${NumberOfSamples + ZeroSampleCount}${ZeroSampleCount}$600$03${item.OffsetFrequency:0.###}${item.OffsetFrequencyPeriodMultiple:0.###}$";
            }
        }

        internal override void Validate()
        {
            Guard.IsGreaterThan(SoundPacketLength, 0, nameof(SoundPacketLength));
            Guard.IsGreaterThan(AODSoundSpeed, 0, nameof(AODSoundSpeed));

            base.Validate();
        }
    }

    /// <inheritdoc cref="AbstractGenerateAODWaveformParam"/>
    /// <remarks>
    /// PrescanAOD波形生成参数
    /// </remarks>
    public sealed record GeneratePrescanAODWaveformParam(
        double BandWidth,
        double CenterFrequency,
        double FlatnessTime,
        FunctionMonotonicTypeEnum FunctionMonotonicTypeEnum,
        double SampleRate,
        double Amplitude,
        string DirectoryPath,
        IReadOnlyList<GenerateAODWaveformConfiguration> Configurations,
        int ZeroSampleCount = 0,
        int EndpointSampleCount = 0,
        double SincCoefficient = 0,
        double AstigmatismCompensationCoefficient = 0,
        double SphericalAberrationCompensationCoefficient = 0,
        double SecondaryAstigmatismCompensationCoefficient = 0,
        double ComaCompensationCoefficient = 0,
        double TrefoilCompensationCoefficient = 0,
        double QuadrafoilCompensationCoefficient = 0,
        double AlphaOrder = 0,
        double AlphaOrderCoefficient = 0,
        Point[]? FrequencyAmplitudes = null,
        int GenerateRetryTimes = 1000) : AbstractGenerateAODWaveformParam(
        BandWidth,
        CenterFrequency,
        FlatnessTime,
        FunctionMonotonicTypeEnum,
        SampleRate,
        Amplitude,
        DirectoryPath,
        Configurations,
        ZeroSampleCount,
        EndpointSampleCount,
        SincCoefficient,
        AstigmatismCompensationCoefficient,
        SphericalAberrationCompensationCoefficient,
        SecondaryAstigmatismCompensationCoefficient,
        ComaCompensationCoefficient,
        TrefoilCompensationCoefficient,
        QuadrafoilCompensationCoefficient,
        AlphaOrder,
        AlphaOrderCoefficient,
        FrequencyAmplitudes,
        GenerateRetryTimes)
    {
        internal override void UpdateConfigurations()
        {
            var frequencyFileName = FunctionMonotonicTypeEnum switch
            {
                FunctionMonotonicTypeEnum.Increasing => $"{LowFrequency:0.###}Mhz_{HighFrequency:0.###}Mhz",
                FunctionMonotonicTypeEnum.Deceasing => $"{HighFrequency:0.###}Mhz_{LowFrequency:0.###}Mhz",
                FunctionMonotonicTypeEnum.Flatness => $"{CenterFrequency:0.###}Mhz_{CenterFrequency:0.###}Mhz",
                _ => ThrowHelper.ThrowArgumentOutOfRangeException<string>(nameof(FunctionMonotonicTypeEnum))
            };

            foreach (var item in Configurations)
            {
                item.FileNameWithoutExtension = $"prescan" +
                                                $"_{FlatnessTime:0.###}ns" +
                                                $"_{frequencyFileName}" +
                                                $"${NumberOfSamples + ZeroSampleCount}${ZeroSampleCount}$600$02${item.OffsetFrequency:0.###}${item.OffsetFrequencyPeriodMultiple:0.###}$";
            }
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
    /// <param name="Configurations">AOD波形的频率偏移配置集合</param>
    /// <param name="ZeroSampleCount">前面添加多少补零采样点个数, 相当于添加延迟(sa)</param>
    /// <param name="EndpointSampleCount">端点头尾添加多少采样点个数, 缓冲(XTC响应不够)(sa)</param>
    /// <param name="SincCoefficient">sin(cx)/cx</param>
    /// <param name="AstigmatismCompensationCoefficient">二次补偿系数t^2 散光</param>
    /// <param name="SphericalAberrationCompensationCoefficient">三次补偿系数t^3 球差</param>
    /// <param name="SecondaryAstigmatismCompensationCoefficient">四次补偿系数t^4 二阶散光</param>
    /// <param name="ComaCompensationCoefficient">sin(2πt/T)</param>
    /// <param name="TrefoilCompensationCoefficient">sin(6πt/T)</param>
    /// <param name="QuadrafoilCompensationCoefficient">sin(8πt/T)</param>
    /// <param name="AlphaOrder">α次补偿</param>
    /// <param name="AlphaOrderCoefficient">α次补偿系数t^α</param>
    /// <param name="FrequencyAmplitudes">AOD波形频率生成补偿系数</param>
    /// <param name="GenerateRetryTimes">生成AOD波形文件重试次数</param>
    public abstract record AbstractGenerateAODWaveformParam(
        double BandWidth,
        double CenterFrequency,
        double FlatnessTime,
        FunctionMonotonicTypeEnum FunctionMonotonicTypeEnum,
        double SampleRate,
        double Amplitude,
        string DirectoryPath,
        IReadOnlyList<GenerateAODWaveformConfiguration> Configurations,
        int ZeroSampleCount,
        int EndpointSampleCount,
        double SincCoefficient,
        double AstigmatismCompensationCoefficient,
        double SphericalAberrationCompensationCoefficient,
        double SecondaryAstigmatismCompensationCoefficient,
        double ComaCompensationCoefficient,
        double TrefoilCompensationCoefficient,
        double QuadrafoilCompensationCoefficient,
        double AlphaOrder,
        double AlphaOrderCoefficient,
        Point[]? FrequencyAmplitudes,
        int GenerateRetryTimes)
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

        internal abstract void UpdateConfigurations();

        internal virtual void Validate()
        {
            Guard.IsGreaterThanOrEqualTo(BandWidth, 0, nameof(BandWidth));
            Guard.IsGreaterThan(CenterFrequency, 0, nameof(CenterFrequency));
            Guard.IsGreaterThan(FlatnessTime, 0, nameof(FlatnessTime));

            Guard.IsGreaterThan(SampleRate, 0, nameof(SampleRate));
            Guard.IsBetweenOrEqualTo(Amplitude, 0, 1, nameof(Amplitude));
            Guard.IsNotNullOrWhiteSpace(DirectoryPath, nameof(DirectoryPath));

            foreach (var item in Configurations) item.Validate();

            Guard.IsGreaterThanOrEqualTo(ZeroSampleCount, 0, nameof(ZeroSampleCount));
            Guard.IsGreaterThanOrEqualTo(EndpointSampleCount, 0, nameof(EndpointSampleCount));
            Guard.IsGreaterThan(GenerateRetryTimes, 0, nameof(GenerateRetryTimes));

            if (FunctionMonotonicTypeEnum is FunctionMonotonicTypeEnum.Flatness && BandWidth != 0) ThrowHelper.ThrowArgumentException(nameof(BandWidth), "if monotonic is flatness then band Width is must 0");
            if (BandWidth == 0 && FunctionMonotonicTypeEnum is not FunctionMonotonicTypeEnum.Flatness) ThrowHelper.ThrowArgumentException(nameof(FunctionMonotonicTypeEnum), "if band Width is must 0 then monotonic is flatness and band");
        }
    }

    /// <summary>
    /// 生成AOD波形文件结果
    /// </summary>
    /// <param name="FlatnessLinearFrequencySignals">AOD波形线性频率信号</param>
    /// <param name="FlatnessTotalFrequencySignals">AOD波形非线性频率信号</param>
    /// <param name="FlatnessAstigmatismCompensationSignals">AOD波形总频率信号</param>
    /// <param name="FlatnessSphericalAberrationCompensationSignals">AOD波形线性补偿信号</param>
    /// <param name="FlatnessSecondaryAstigmatismCompensationSignals">AOD波形散光补偿信号</param>
    /// <param name="FlatnessComaCompensationSignals">AOD波形球差补偿信号</param>
    /// <param name="FlatnessTrefoilCompensationSignals">AOD波形二阶散光补偿信号</param>
    /// <param name="FlatnessQuadrafoilCompensationSignals">AOD波形coma补偿信号</param>
    /// <param name="FlatnessAlphaOrderCompensationSignals">AOD波形trefoil补偿信号</param>
    /// <param name="Items">AOD波形文件集合</param>
    public sealed record GenerateAODWaveformResult(
        IReadOnlyList<Point> FlatnessLinearFrequencySignals,
        IReadOnlyList<Point> FlatnessTotalFrequencySignals,
        IReadOnlyList<Point> FlatnessAstigmatismCompensationSignals,
        IReadOnlyList<Point> FlatnessSphericalAberrationCompensationSignals,
        IReadOnlyList<Point> FlatnessSecondaryAstigmatismCompensationSignals,
        IReadOnlyList<Point> FlatnessComaCompensationSignals,
        IReadOnlyList<Point> FlatnessTrefoilCompensationSignals,
        IReadOnlyList<Point> FlatnessQuadrafoilCompensationSignals,
        IReadOnlyList<Point> FlatnessAlphaOrderCompensationSignals,
        IReadOnlyList<GenerateAODWaveformResultItem> Items);

    /// <summary>
    /// 生成AOD波形文件
    /// </summary>
    /// <param name="FilePath">AOD波形文件路径</param>
    /// <param name="Signals">AOD波形quadrafoil补偿信号</param>
    /// <param name="SignalsFourier">AOD波形α次方补偿信号</param>
    /// <param name="FrequencyAmplitudes">AOD波形非线性补偿信号</param>
    public sealed record GenerateAODWaveformResultItem(
        string FilePath,
        IReadOnlyList<Point> Signals,
        IReadOnlyList<Point> SignalsFourier,
        IReadOnlyList<Point> FrequencyAmplitudes);

    #endregion 参数

    /// <summary>
    /// 生成ChirpAOD波形文件
    /// </summary>
    /// <param name="generateChirpAODWaveformParam">ChirpAOD波形生成参数</param>
    /// <returns>(是否成功, 异常信息, 结果)</returns>
    public static (bool IsSuccess, Exception? Exception, GenerateAODWaveformResult GenerateAODWaveformResult) GenerateChirpAODWaveformFile(GenerateChirpAODWaveformParam generateChirpAODWaveformParam) => GenerateAodWaveFile(generateChirpAODWaveformParam);

    /// <summary>
    /// 生成PrescanAOD波形文件
    /// </summary>
    /// <param name="generatePrescanAODWaveformParam">PrescanAOD波形生成参数</param>
    /// <returns>(是否成功, 异常信息, 结果)</returns>
    public static (bool IsSuccess, Exception? Exception, GenerateAODWaveformResult GenerateAODWaveformResult) GeneratePrescanAODWaveformFile(GeneratePrescanAODWaveformParam generatePrescanAODWaveformParam) => GenerateAodWaveFile(generatePrescanAODWaveformParam);

    /// <summary>
    /// 生成AOD波形文件
    /// </summary>
    /// <param name="generateAODWaveformParam">PrescanAOD波形生成参数</param>
    /// <returns>(是否成功, 异常信息, 结果)</returns>
    public static (bool IsSuccess, Exception? Exception, GenerateAODWaveformResult GenerateAODWaveformResult) GenerateAodWaveFile(AbstractGenerateAODWaveformParam generateAODWaveformParam)
    {
        generateAODWaveformParam.Validate();
        generateAODWaveformParam.UpdateConfigurations();

        var (bandWidth,
            centerFrequency,
            _,
            functionMonotonicTypeEnum,
            sampleRate,
            amplitude,
            directoryPath,
            configurations,
            _,
            endpointSampleCount,
            sincCoefficient,
            astigmatismCompensationCoefficient,
            sphericalAberrationCompensationCoefficient,
            secondaryAstigmatismCompensationCoefficient,
            comaCompensationCoefficient,
            trefoilCompensationCoefficient,
            quadrafoilCompensationCoefficient,
            alphaOrder,
            alphaOrderCoefficient,
            frequencyAmplitudes,
            generateRetryTimes) = generateAODWaveformParam;

        var lowFrequency = generateAODWaveformParam.LowFrequency;
        var highFrequency = generateAODWaveformParam.HighFrequency;
        var numberOfSamples = generateAODWaveformParam.NumberOfSamples;

        #region 返回结果

        var dt = 1d / sampleRate; // 每个采样点的时间间隔 (us/sa): 1 / (Msa/s) = 10^-6s/sa = us/sa

        var allSampleIndices = GenerateUtils.LinearIndexRange(0, numberOfSamples - 1);
        var headerSampleIndices = GenerateUtils.LinearIndexRange(0, endpointSampleCount - 1);
        var flatnessSampleIndices = GenerateUtils.LinearIndexRange(endpointSampleCount, numberOfSamples - endpointSampleCount - 1);
        var footerSampleIndices = GenerateUtils.LinearIndexRange(numberOfSamples - endpointSampleCount, numberOfSamples - 1);

        var aodWaveformSignals = Vector<double>.Build.Dense(numberOfSamples);

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

                switch (functionMonotonicTypeEnum)
                {
                    case FunctionMonotonicTypeEnum.Increasing:
                    case FunctionMonotonicTypeEnum.Deceasing:
                        dLinearFrequencies = bandWidth / t.Maximum() * t;
                        dAstigmatismFrequencies = astigmatismCompensationCoefficient * t.PointwisePower(2);
                        dSphericalAberrationFrequencies = sphericalAberrationCompensationCoefficient * t.PointwisePower(3);
                        dSecondaryAstigmatismFrequencies = secondaryAstigmatismCompensationCoefficient * t.PointwisePower(4);
                        dComaFrequencies = comaCompensationCoefficient * (2d * Math.PI * t / t.Maximum()).PointwiseSin();
                        dTrefoilFrequencies = trefoilCompensationCoefficient * (6d * Math.PI * t / t.Maximum()).PointwiseSin();
                        dQuadrafoilFrequencies = quadrafoilCompensationCoefficient * (8d * Math.PI * t / t.Maximum()).PointwiseSin();
                        dAlphaOrderFrequencies = alphaOrderCoefficient * t.PointwisePower(alphaOrder);

                        break;

                    case FunctionMonotonicTypeEnum.Flatness:
                    default:
                        dLinearFrequencies = Vector<double>.Build.Dense(t.Count, 0);
                        dAstigmatismFrequencies = Vector<double>.Build.Dense(t.Count, 0);
                        dSphericalAberrationFrequencies = Vector<double>.Build.Dense(t.Count, 0);
                        dSecondaryAstigmatismFrequencies = Vector<double>.Build.Dense(t.Count, 0);
                        dComaFrequencies = Vector<double>.Build.Dense(t.Count, 0);
                        dTrefoilFrequencies = Vector<double>.Build.Dense(t.Count, 0);
                        dQuadrafoilFrequencies = Vector<double>.Build.Dense(t.Count, 0);
                        dAlphaOrderFrequencies = Vector<double>.Build.Dense(t.Count, 0);

                        break;
                }

                dHeaderFrequencies = functionMonotonicTypeEnum switch
                {
                    FunctionMonotonicTypeEnum.Increasing => Vector<double>.Build.Dense(headerSampleIndices.Length, lowFrequency),
                    FunctionMonotonicTypeEnum.Deceasing => Vector<double>.Build.Dense(headerSampleIndices.Length, highFrequency),
                    FunctionMonotonicTypeEnum.Flatness => Vector<double>.Build.Dense(headerSampleIndices.Length, centerFrequency),
                    _ => ThrowHelper.ThrowArgumentOutOfRangeException<Vector<double>>(nameof(functionMonotonicTypeEnum))
                };

                dFooterFrequencies = functionMonotonicTypeEnum switch
                {
                    FunctionMonotonicTypeEnum.Increasing => Vector<double>.Build.Dense(footerSampleIndices.Length, highFrequency),
                    FunctionMonotonicTypeEnum.Deceasing => Vector<double>.Build.Dense(footerSampleIndices.Length, lowFrequency),
                    FunctionMonotonicTypeEnum.Flatness => Vector<double>.Build.Dense(footerSampleIndices.Length, centerFrequency),
                    _ => ThrowHelper.ThrowArgumentOutOfRangeException<Vector<double>>(nameof(functionMonotonicTypeEnum))
                };

                dFlatnessFrequencies = functionMonotonicTypeEnum switch
                {
                    FunctionMonotonicTypeEnum.Increasing => lowFrequency + dLinearFrequencies + dAstigmatismFrequencies + dSphericalAberrationFrequencies + dSecondaryAstigmatismFrequencies + dComaFrequencies + dTrefoilFrequencies + dQuadrafoilFrequencies + dAlphaOrderFrequencies,
                    FunctionMonotonicTypeEnum.Deceasing => highFrequency - dLinearFrequencies - dAstigmatismFrequencies - dSphericalAberrationFrequencies - dSecondaryAstigmatismFrequencies - dComaFrequencies - dTrefoilFrequencies - dQuadrafoilFrequencies - dAlphaOrderFrequencies,
                    FunctionMonotonicTypeEnum.Flatness => Vector<double>.Build.Dense(flatnessSampleIndices.Length, centerFrequency),
                    _ => ThrowHelper.ThrowArgumentOutOfRangeException<Vector<double>>(nameof(functionMonotonicTypeEnum))
                };

                if (dFlatnessFrequencies.Exists(f => f <= 0 || f > generateAODWaveformParam.HighFrequency * 1.5d)) ThrowHelper.ThrowArgumentException(nameof(dFlatnessFrequencies), $"Synthesized frequencies must be in the range (0, {generateAODWaveformParam.HighFrequency * 1.5d:f3} MHz)");

                #endregion 频率

                FFT(0, 0);

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

                indices = fftSecondDerivative.FindAbsAbove(0.0001);
                headerIndices = GenerateUtils.LinearIndexRange(0, indices[0] - 1);
                flatnessIndices = GenerateUtils.LinearIndexRange(indices[0], indices[^1]);
                footerIndices = GenerateUtils.LinearIndexRange(indices[^1] + 1, fftSecondDerivative.Count - 1);

                var fftSecondDerivativeSign = Vector<double>.Build.SameAs(fftSecondDerivative);
                fftSecondDerivativeSign.SetByIndices(flatnessIndices, fftSecondDerivative.GetByIndices(flatnessIndices).PointwiseSign());
                fftSecondDerivativeSign.SetByIndices(headerIndices, Vector<double>.Build.Dense(headerIndices.Length, fftSecondDerivativeSign[flatnessIndices[0]]));
                fftSecondDerivativeSign.SetByIndices(footerIndices, Vector<double>.Build.Dense(footerIndices.Length, fftSecondDerivativeSign[flatnessIndices[^1]]));

                var fftInflectionPointIndices = fftSecondDerivativeSign.Differentiate().FindAll(d => d != 0); // 通过sign寻找拐点点, 极值点的索引都提前了1个点
                var fftInflectionPointFrequencies = fftFrequencies.GetByIndices([.. fftInflectionPointIndices.Select(d => d + 1)]);

                switch (functionMonotonicTypeEnum)
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
                        ThrowHelper.ThrowArgumentOutOfRangeException(nameof(functionMonotonicTypeEnum));
                        break;
                }

                #endregion 平坦部分的起始和终止频率

                #region 修正

                if (functionMonotonicTypeEnum == FunctionMonotonicTypeEnum.Flatness)
                {
                    if (Math.Abs(centerFrequency - generateAODWaveformParam.CenterFrequency) < sampleRate / flatnessSampleIndices.Length)
                    {
                        isSuccess = true;
                        break;
                    }

                    if (centerFrequency < generateAODWaveformParam.CenterFrequency)
                    {
                        centerFrequency += sampleRate / (2d * flatnessSampleIndices.Length);
                    }
                    else
                    {
                        centerFrequency -= sampleRate / (2d * flatnessSampleIndices.Length);
                    }
                }
                else if (functionMonotonicTypeEnum == FunctionMonotonicTypeEnum.Increasing)
                {
                    if (Math.Abs(minFlatnessFrequency - generateAODWaveformParam.LowFrequency) < sampleRate / flatnessSampleIndices.Length)
                    {
                        if (Math.Abs(maxFlatnessFrequency - generateAODWaveformParam.HighFrequency) < sampleRate / flatnessSampleIndices.Length)
                        {
                            isSuccess = true;
                            break;
                        }

                        if (maxFlatnessFrequency < generateAODWaveformParam.HighFrequency)
                            bandWidth += sampleRate / (2d * flatnessSampleIndices.Length);
                        else
                            bandWidth -= sampleRate / (2d * flatnessSampleIndices.Length);
                    }
                    else if (minFlatnessFrequency < generateAODWaveformParam.LowFrequency)
                        lowFrequency += sampleRate / (2d * flatnessSampleIndices.Length);
                    else
                        lowFrequency -= sampleRate / (2d * flatnessSampleIndices.Length);
                }
                else if (functionMonotonicTypeEnum == FunctionMonotonicTypeEnum.Deceasing)
                {
                    if (Math.Abs(maxFlatnessFrequency - generateAODWaveformParam.HighFrequency) < sampleRate / flatnessSampleIndices.Length)
                    {
                        if (Math.Abs(minFlatnessFrequency - generateAODWaveformParam.LowFrequency) < sampleRate / flatnessSampleIndices.Length)
                        {
                            isSuccess = true;
                            break;
                        }

                        if (minFlatnessFrequency < generateAODWaveformParam.LowFrequency)
                            bandWidth -= sampleRate / (2d * flatnessSampleIndices.Length);
                        else
                            bandWidth += sampleRate / (2d * flatnessSampleIndices.Length);
                    }
                    else if (maxFlatnessFrequency < generateAODWaveformParam.HighFrequency)
                        highFrequency += sampleRate / (2d * flatnessSampleIndices.Length);
                    else
                        highFrequency -= sampleRate / (2d * flatnessSampleIndices.Length);
                }
                else
                {
                    ThrowHelper.ThrowArgumentOutOfRangeException(nameof(functionMonotonicTypeEnum));
                }

                #endregion 修正

                if (++count > generateRetryTimes) ThrowHelper.ThrowInvalidOperationException("AOD waveforms could not be generated");
            }
            catch (Exception ex)
            {
                exception = ex;
                isSuccess = false;
                break;
            }
        }

        var itemList = new List<GenerateAODWaveformResultItem>();
        foreach (var item in configurations)
        {
            FFT(item.OffsetFrequency, item.OffsetFrequencyPeriodMultiple);
            var frequencyCompensationsResult = new List<Point>();

            if (isSuccess && functionMonotonicTypeEnum != FunctionMonotonicTypeEnum.Flatness && (frequencyAmplitudes?.Length > 0 || sincCoefficient != 0))
            {
                var fftFullFrequencies = fftResult.GetFullFrequencies(sampleRate);
                var indices = fftFullFrequencies.FindAll(d => minFlatnessFrequency <= Math.Abs(d) && Math.Abs(d) <= maxFlatnessFrequency);
                if (frequencyAmplitudes?.Length > 0)
                {
                    foreach (var index in indices)
                    {
                        var f = Math.Abs(fftFullFrequencies[index]);
                        var compensation = BinarySearch.TryValueIndexRange(
                            [.. frequencyAmplitudes.Select(tt => tt.X)],
                            f,
                            out var startColumnIndex,
                            out var endColumnIndex)
                            ? Interpolator.Linear(frequencyAmplitudes[startColumnIndex], frequencyAmplitudes[endColumnIndex], f)
                            : f <= frequencyAmplitudes.First().X
                                ? frequencyAmplitudes.First().Y
                                : frequencyAmplitudes.Last().Y;

                        fftResult[index] *= compensation;
                        if (fftFullFrequencies[index] >= 0) frequencyCompensationsResult.Add(new Point(f, compensation));
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
                        var x = sincCoefficient * (f - flatnessCenterFrequency) / flatnessBandwidth;
                        var compensation = x == 0
                            ? 1d
                            : 1d / (Math.Sin(x) / x + 1e-10);
                        compensations[i] = compensation;
                    }

                    compensations /= compensations.AbsoluteMaximum();

                    for (var i = 0; i < indices.Length; i++)
                    {
                        var index = indices[i];
                        var f = Math.Abs(fftFullFrequencies[index]);

                        var compensation = compensations[i];
                        fftResult[index] *= compensation;

                        if (fftFullFrequencies[index] >= 0) frequencyCompensationsResult.Add(new Point(f, compensation));
                    }
                }

                var recoveredSignal = fftResult.InverseFastFourierTransform().Real();
                if (recoveredSignal.AbsoluteMaximum() > amplitude)
                    recoveredSignal = recoveredSignal / recoveredSignal.AbsoluteMaximum() * amplitude;
                aodWaveformSignals.SetSubVectorRange(flatnessSampleIndices[0], flatnessSampleIndices[^1], recoveredSignal);

                fftResult = recoveredSignal.ToComplex().FastFourierTransform();
                (fftFrequencies, fftMagnitudes) = fftResult.GetPositiveFrequencies(sampleRate);
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
             *          一、公式: =(HEX2DEC(A1)-IF(HEX2DEC(A1)>=32768,65536,0))/POWER(2,15)
             *          二、不要[下拉填充点]直接推拽下拉太慢, 快速公式下拉填充: 直接双击[下拉填充点]一行直接生成
             */

            // 将结果转换为16位整数并保存到文件
            var aodWaveformSignalResult = aodWaveformSignals
                .Select(t => ConvertUtils.ToInt16NotOverflowException(Math.Round(Math.Pow(2, 15) * t, MidpointRounding.AwayFromZero)))
                .Select(Convert.ToInt64)
                .Select(t => t < 0 ? t + (long)Math.Pow(2, 32) : t)
                .ToArray();
            var hexStrings = aodWaveformSignalResult
                .Select(t => t.ToString("X4")) // 转换为16进制补码字符串, 至少4位不足右边补0
                .Select(t => t[^4..]) // 字符串的最后 4 位
                .ToArray();

            var aodWaveformFilePath = isSuccess
                ? Path.Combine(directoryPath, item.DirectoryName, $"{item.FileNameWithoutExtension}.txt")
                : Path.Combine(directoryPath, item.DirectoryName, $"Error_{item.FileNameWithoutExtension}.txt");
            var aodWaveformParmJsonFilePath = $"{aodWaveformFilePath}.json";

            aodWaveformFilePath = FileHelper.GetEnsureLongPathSupport(aodWaveformFilePath);
            aodWaveformParmJsonFilePath = FileHelper.GetEnsureLongPathSupport(aodWaveformParmJsonFilePath);

            DirectoryHelper.CreateFileDirectoryIfNotExists(aodWaveformFilePath);
            FileHelper.DeleteFileIfExists(aodWaveformFilePath);
            FileHelper.DeleteFileIfExists(aodWaveformParmJsonFilePath);
            File.WriteAllText(aodWaveformFilePath, string.Join(Environment.NewLine, hexStrings));
            FileHelper.SerializeOperate(generateAODWaveformParam, aodWaveformParmJsonFilePath);

            itemList.Add(new GenerateAODWaveformResultItem(
                aodWaveformFilePath,
                [..allSampleIndices.Select(t => new Point(t + 1, aodWaveformSignals[t]))],
                [..fftFrequencies.Zip(fftMagnitudes, (t1, t2) => new Point(t1, t2))],
                [..frequencyCompensationsResult]));
        }

        return (
            isSuccess,
            exception,
            new GenerateAODWaveformResult(
                [..flatnessSampleIndices.Select((t, index) => new Point(t + 1, dLinearFrequencies[index]))],
                [..flatnessSampleIndices.Select((t, index) => new Point(t + 1, dFlatnessFrequencies[index]))],
                [..flatnessSampleIndices.Select((t, index) => new Point(t + 1, dAstigmatismFrequencies[index]))],
                [..flatnessSampleIndices.Select((t, index) => new Point(t + 1, dSphericalAberrationFrequencies[index]))],
                [..flatnessSampleIndices.Select((t, index) => new Point(t + 1, dSecondaryAstigmatismFrequencies[index]))],
                [..flatnessSampleIndices.Select((t, index) => new Point(t + 1, dComaFrequencies[index]))],
                [..flatnessSampleIndices.Select((t, index) => new Point(t + 1, dTrefoilFrequencies[index]))],
                [..flatnessSampleIndices.Select((t, index) => new Point(t + 1, dQuadrafoilFrequencies[index]))],
                [..flatnessSampleIndices.Select((t, index) => new Point(t + 1, dAlphaOrderFrequencies[index]))],
                itemList
            )
        );

        void FFT(double offsetFrequency, double offsetFrequencyPeriodMultiple)
        {
            #region 相位

            var dHeaderPhases = 2d * Math.PI * dHeaderFrequencies * dt;
            var headerPhases = dHeaderPhases.IntegrateCumulative();
            var dFlatnessPhases = 2d * Math.PI * dFlatnessFrequencies * dt;
            var flatnessPhases = dFlatnessPhases.IntegrateCumulative();
            if (offsetFrequency != 0 && offsetFrequencyPeriodMultiple != 0) flatnessPhases += 2d * Math.PI * dFlatnessFrequencies * offsetFrequencyPeriodMultiple * 1d / offsetFrequency;
            var dFooterPhases = 2d * Math.PI * dFooterFrequencies * dt;
            var footerPhases = dFooterPhases.IntegrateCumulative();

            #endregion 相位

            #region 波形

            if (headerSampleIndices.Length > 0) aodWaveformSignals.SetSubVectorRange(headerSampleIndices[0], headerSampleIndices[^1], amplitude * headerPhases.PointwiseCos().PointwiseMultiply(Vector<double>.Build.DenseOfArray(headerSampleIndices) / headerSampleIndices.Length));
            aodWaveformSignals.SetSubVectorRange(flatnessSampleIndices[0], flatnessSampleIndices[^1], amplitude * flatnessPhases.PointwiseCos());
            if (footerSampleIndices.Length > 0) aodWaveformSignals.SetSubVectorRange(footerSampleIndices[0], footerSampleIndices[^1], amplitude * footerPhases.PointwiseCos().PointwiseMultiply(1d - (Vector<double>.Build.DenseOfArray(footerSampleIndices) - footerSampleIndices[0] + 1d) / footerSampleIndices.Length));

            #endregion 波形

            #region 傅里叶

            var flatnessAodWaveformSignals = aodWaveformSignals.SubVectorRange(flatnessSampleIndices[0], flatnessSampleIndices[^1]);
            fftResult = flatnessAodWaveformSignals.ToComplex().FastFourierTransform();
            (fftFrequencies, fftMagnitudes) = fftResult.GetPositiveFrequencies(sampleRate);

            #endregion 傅里叶
        }
    }
}