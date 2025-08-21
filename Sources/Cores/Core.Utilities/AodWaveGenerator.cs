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

public static class AodWaveGenerator
{
    /// <summary>
    /// 生成Chirp AOD波形文件, [中心频率 - 带宽/2, 中心频率 + 带宽/2]
    /// </summary>
    /// <param name="bandWidth">带宽(MHz)</param>
    /// <param name="centerFrequency">中心频率(Mhz)</param>
    /// <param name="soundPacketLength">音包长度(mm)</param>
    /// <param name="functionMonotonicTypeEnum">递增, 递减, 平坦</param>
    /// <param name="sampleRate">采样率(Msa/s)</param>
    /// <param name="amplitude">幅值</param>
    /// <param name="aodWaveDirectory">生成的目录</param>
    /// <param name="zeroSampleCount">前面添加多少补零采样点个数, 相当于添加延迟(sa)</param>
    /// <param name="endpointSampleCount">端点头尾添加多少采样点个数, 缓冲(XTC响应不够)(sa)</param>
    /// <param name="offsetFrequency">偏移的频率</param>
    /// <param name="offsetFrequencyPeriodMultiple">偏移的频率的2π周期的倍率</param>
    /// <param name="sincCoefficient">sin(cx)/cx</param>
    /// <param name="astigmatismCompensationCoefficient">二次补偿系数t^2 散光</param>
    /// <param name="sphericalAberrationCompensationCoefficient">三次补偿系数t^3 球差</param>
    /// <param name="secondaryAstigmatismCompensationCoefficient">四次补偿系数t^4 二阶散光</param>
    /// <param name="comaCompensationCoefficient">sin(2πt/T)</param>
    /// <param name="trefoilCompensationCoefficient">sin(6πt/T)</param>
    /// <param name="quadrafoilCompensationCoefficient">sin(8πt/T)</param>
    /// <param name="alphaOrder">α次补偿</param>
    /// <param name="alphaOrderCoefficient">α次补偿系数t^α</param>
    /// <param name="frequencyAmplitudes">波形频率生成补偿系数</param>
    /// <param name="generateRetryTimes">生成Aod文件重试次数</param>
    /// <returns>
    /// <code>
    /// (是否成功,
    ///  波形文件路径,
    ///  波形线性频率信号,
    ///  波形非线性频率信号,
    ///  波形总频率信号,
    ///  波形线性补偿信号,
    ///  波形散光补偿信号,
    ///  波形球差补偿信号,
    ///  波形二阶散光补偿信号,
    ///  波形coma补偿信号,
    ///  波形trefoil补偿信号,
    ///  波形quadrafoil补偿信号,
    ///  波形α次方补偿信号,
    ///  波形非线性补偿信号,
    ///  波形总补偿信号,
    ///  波形信号,
    ///  波形信号傅里叶,
    ///  波形信号Sinc,
    ///  异常信息)
    /// </code>
    /// </returns>
    public static (
        bool IsSuccess,
        string AodWaveFilePath,
        Point[] AodWaveFlatnessLinearFrequencySignals,
        Point[] AodWaveFlatnessTotalFrequencySignals,
        Point[] AodWaveFlatnessAstigmatismCompensationSignals,
        Point[] AodWaveFlatnessSphericalAberrationCompensationSignals,
        Point[] AodWaveFlatnessSecondaryAstigmatismCompensationSignals,
        Point[] AodWaveFlatnessComaCompensationSignals,
        Point[] AodWaveFlatnessTrefoilCompensationSignals,
        Point[] AodWaveFlatnessQuadrafoilCompensationSignals,
        Point[] AodWaveFlatnessAlphaOrderCompensationSignals,
        Point[] AodWaveSignals,
        Point[] AodWaveSignalsFourier,
        Point[] AodWaveFrequencyAmplitudes,
        Exception? Exception) GenerateChirpAodWaveFile(
            double bandWidth,
            double centerFrequency,
            double soundPacketLength,
            FunctionMonotonicTypeEnum functionMonotonicTypeEnum,
            double sampleRate,
            double amplitude,
            string aodWaveDirectory,
            int zeroSampleCount = 0,
            int endpointSampleCount = 0,
            double offsetFrequency = 0,
            double offsetFrequencyPeriodMultiple = 0,
            double sincCoefficient = 0,
            double astigmatismCompensationCoefficient = 0d,
            double sphericalAberrationCompensationCoefficient = 0d,
            double secondaryAstigmatismCompensationCoefficient = 0d,
            double comaCompensationCoefficient = 0d,
            double trefoilCompensationCoefficient = 0d,
            double quadrafoilCompensationCoefficient = 0d,
            double alphaOrder = 0d,
            double alphaOrderCoefficient = 0d,
            Point[]? frequencyAmplitudes = null,
            int generateRetryTimes = 1000)
        => GenerateAodWaveFile(
            bandWidth,
            centerFrequency,
            functionMonotonicTypeEnum,
            sampleRate,
            amplitude,
            aodWaveDirectory,
            zeroSampleCount: zeroSampleCount,
            flatnessTimeNullable: null,
            soundPacketLengthNullable: soundPacketLength,
            endpointSampleCount: endpointSampleCount,
            offsetFrequency: offsetFrequency,
            offsetFrequencyPeriodMultiple: offsetFrequencyPeriodMultiple,
            sincCoefficient: sincCoefficient,
            astigmatismCompensationCoefficient: astigmatismCompensationCoefficient,
            sphericalAberrationCompensationCoefficient: sphericalAberrationCompensationCoefficient,
            secondaryAstigmatismCompensationCoefficient: secondaryAstigmatismCompensationCoefficient,
            comaCompensationCoefficient: comaCompensationCoefficient,
            trefoilCompensationCoefficient: trefoilCompensationCoefficient,
            quadrafoilCompensationCoefficient: quadrafoilCompensationCoefficient,
            alphaOrder: alphaOrder,
            alphaOrderCoefficient: alphaOrderCoefficient,
            frequencyAmplitudes: frequencyAmplitudes,
            generateRetryTimes: generateRetryTimes);

    /// <summary>
    /// 生成Prescan AOD波形文件, [中心频率 - 带宽/2, 中心频率 + 带宽/2]
    /// </summary>
    /// <param name="bandWidth">带宽(MHz)</param>
    /// <param name="centerFrequency">中心频率(Mhz)</param>
    /// <param name="flatnessTime">平坦时间(ns)</param>
    /// <param name="functionMonotonicTypeEnum">递增, 递减, 平坦</param>
    /// <param name="sampleRate">采样率(Msa/s)</param>
    /// <param name="amplitude">幅值</param>
    /// <param name="aodWaveDirectory">生成的目录</param>
    /// <param name="zeroSampleCount">前面添加多少补零采样点个数, 相当于添加延迟(sa)</param>
    /// <param name="endpointSampleCount">端点头尾添加多少采样点个数, 缓冲(XTC响应不够)(sa)</param>
    /// <param name="offsetFrequency">偏移的频率</param>
    /// <param name="offsetFrequencyPeriodMultiple">偏移的频率的2π周期的倍率</param>
    /// <param name="sincCoefficient">sin(cx)/cx</param>
    /// <param name="astigmatismCompensationCoefficient">二次补偿系数t^2 散光</param>
    /// <param name="sphericalAberrationCompensationCoefficient">三次补偿系数t^3 球差</param>
    /// <param name="secondaryAstigmatismCompensationCoefficient">四次补偿系数t^4 二阶散光</param>
    /// <param name="comaCompensationCoefficient">sin(2πt/T)</param>
    /// <param name="trefoilCompensationCoefficient">sin(6πt/T)</param>
    /// <param name="quadrafoilCompensationCoefficient">sin(8πt/T)</param>
    /// <param name="alphaOrder">α次补偿</param>
    /// <param name="alphaOrderCoefficient">α次补偿系数t^α</param>
    /// <param name="frequencyAmplitudes">波形频率生成补偿系数</param>
    /// <param name="generateRetryTimes">生成Aod文件重试次数</param>
    /// <returns>
    /// <code>
    /// (是否成功,
    ///  波形文件路径,
    ///  波形线性频率信号,
    ///  波形非线性频率信号,
    ///  波形总频率信号,
    ///  波形线性补偿信号,
    ///  波形散光补偿信号,
    ///  波形球差补偿信号,
    ///  波形二阶散光补偿信号,
    ///  波形coma补偿信号,
    ///  波形trefoil补偿信号,
    ///  波形quadrafoil补偿信号,
    ///  波形α次方补偿信号,
    ///  波形非线性补偿信号,
    ///  波形总补偿信号,
    ///  波形信号,
    ///  波形信号傅里叶,
    ///  波形信号Sinc,
    ///  异常信息)
    /// </code>
    /// </returns>
    public static (
        bool IsSuccess,
        string AodWaveFilePath,
        Point[] AodWaveFlatnessLinearFrequencySignals,
        Point[] AodWaveFlatnessTotalFrequencySignals,
        Point[] AodWaveFlatnessAstigmatismCompensationSignals,
        Point[] AodWaveFlatnessSphericalAberrationCompensationSignals,
        Point[] AodWaveFlatnessSecondaryAstigmatismCompensationSignals,
        Point[] AodWaveFlatnessComaCompensationSignals,
        Point[] AodWaveFlatnessTrefoilCompensationSignals,
        Point[] AodWaveFlatnessQuadrafoilCompensationSignals,
        Point[] AodWaveFlatnessAlphaOrderCompensationSignals,
        Point[] AodWaveSignals,
        Point[] AodWaveSignalsFourier,
        Point[] AodWaveFrequencyAmplitudes,
        Exception? Exception) GeneratePrescanAodWaveFile(
            double bandWidth,
            double centerFrequency,
            double flatnessTime,
            FunctionMonotonicTypeEnum functionMonotonicTypeEnum,
            double sampleRate,
            double amplitude,
            string aodWaveDirectory,
            int zeroSampleCount = 0,
            int endpointSampleCount = 0,
            double offsetFrequency = 0,
            double offsetFrequencyPeriodMultiple = 0,
            double sincCoefficient = 0,
            double astigmatismCompensationCoefficient = 0d,
            double sphericalAberrationCompensationCoefficient = 0d,
            double secondaryAstigmatismCompensationCoefficient = 0d,
            double comaCompensationCoefficient = 0d,
            double trefoilCompensationCoefficient = 0d,
            double quadrafoilCompensationCoefficient = 0d,
            double alphaOrder = 0d,
            double alphaOrderCoefficient = 0d,
            Point[]? frequencyAmplitudes = null,
            int generateRetryTimes = 1000)
        => GenerateAodWaveFile(
            bandWidth,
            centerFrequency,
            functionMonotonicTypeEnum,
            sampleRate,
            amplitude,
            aodWaveDirectory,
            zeroSampleCount: zeroSampleCount,
            flatnessTimeNullable: flatnessTime,
            soundPacketLengthNullable: null,
            endpointSampleCount: endpointSampleCount,
            offsetFrequency: offsetFrequency,
            offsetFrequencyPeriodMultiple: offsetFrequencyPeriodMultiple,
            sincCoefficient: sincCoefficient,
            astigmatismCompensationCoefficient: astigmatismCompensationCoefficient,
            sphericalAberrationCompensationCoefficient: sphericalAberrationCompensationCoefficient,
            secondaryAstigmatismCompensationCoefficient: secondaryAstigmatismCompensationCoefficient,
            comaCompensationCoefficient: comaCompensationCoefficient,
            trefoilCompensationCoefficient: trefoilCompensationCoefficient,
            quadrafoilCompensationCoefficient: quadrafoilCompensationCoefficient,
            alphaOrder: alphaOrder,
            alphaOrderCoefficient: alphaOrderCoefficient,
            frequencyAmplitudes: frequencyAmplitudes,
            generateRetryTimes: generateRetryTimes);

    /// <summary>
    /// 生成AOD波形文件, [中心频率 - 带宽/2, 中心频率 + 带宽/2]
    /// </summary>
    /// <param name="bandWidth">带宽(MHz)</param>
    /// <param name="centerFrequency">中心频率(Mhz)</param>
    /// <param name="functionMonotonicTypeEnum">递增, 递减, 平坦</param>
    /// <param name="sampleRate">采样率(Msa/s)</param>
    /// <param name="amplitude">幅值</param>
    /// <param name="aodWaveDirectory">生成的目录</param>
    /// <param name="zeroSampleCount">前面添加多少补零采样点个数, 相当于添加延迟(sa)</param>
    /// <param name="flatnessTimeNullable">平坦时间(ns)</param>
    /// <param name="soundPacketLengthNullable">音包长度(mm)</param>
    /// <param name="endpointSampleCount">端点头尾添加多少采样点个数, 缓冲(XTC响应不够)(sa)</param>
    /// <param name="offsetFrequency">偏移的频率</param>
    /// <param name="offsetFrequencyPeriodMultiple">偏移的频率的2π周期的倍率</param>
    /// <param name="sincCoefficient">sin(cx)/cx</param>
    /// <param name="astigmatismCompensationCoefficient">二次补偿系数t^2 散光</param>
    /// <param name="sphericalAberrationCompensationCoefficient">三次补偿系数t^3 球差</param>
    /// <param name="secondaryAstigmatismCompensationCoefficient">四次补偿系数t^4 二阶散光</param>
    /// <param name="comaCompensationCoefficient">sin(2πt/T)</param>
    /// <param name="trefoilCompensationCoefficient">sin(6πt/T)</param>
    /// <param name="quadrafoilCompensationCoefficient">sin(8πt/T)</param>
    /// <param name="alphaOrder">α次补偿</param>
    /// <param name="alphaOrderCoefficient">α次补偿系数t^α</param>
    /// <param name="frequencyAmplitudes">波形频率生成补偿系数</param>
    /// <param name="generateRetryTimes">生成Aod文件重试次数</param>
    /// <returns>
    /// <code>
    /// (是否成功,
    ///  波形文件路径,
    ///  波形线性频率信号,
    ///  波形非线性频率信号,
    ///  波形总频率信号,
    ///  波形线性补偿信号,
    ///  波形散光补偿信号,
    ///  波形球差补偿信号,
    ///  波形二阶散光补偿信号,
    ///  波形coma补偿信号,
    ///  波形trefoil补偿信号,
    ///  波形quadrafoil补偿信号,
    ///  波形α次方补偿信号,
    ///  波形非线性补偿信号,
    ///  波形总补偿信号,
    ///  波形信号,
    ///  波形信号傅里叶,
    ///  波形信号Sinc,
    ///  异常信息)
    /// </code>
    /// </returns>
    public static (
        bool IsSuccess,
        string AodWaveFilePath,
        Point[] AodWaveFlatnessLinearFrequencySignals,
        Point[] AodWaveFlatnessTotalFrequencySignals,
        Point[] AodWaveFlatnessAstigmatismCompensationSignals,
        Point[] AodWaveFlatnessSphericalAberrationCompensationSignals,
        Point[] AodWaveFlatnessSecondaryAstigmatismCompensationSignals,
        Point[] AodWaveFlatnessComaCompensationSignals,
        Point[] AodWaveFlatnessTrefoilCompensationSignals,
        Point[] AodWaveFlatnessQuadrafoilCompensationSignals,
        Point[] AodWaveFlatnessAlphaOrderCompensationSignals,
        Point[] AodWaveSignals,
        Point[] AodWaveSignalsFourier,
        Point[] AodWaveFrequencyAmplitudes,
        Exception? Exception) GenerateAodWaveFile(
            double bandWidth,
            double centerFrequency,
            FunctionMonotonicTypeEnum functionMonotonicTypeEnum,
            double sampleRate,
            double amplitude,
            string aodWaveDirectory,
            int zeroSampleCount = 0,
            double? flatnessTimeNullable = null,
            double? soundPacketLengthNullable = null,
            int endpointSampleCount = 0,
            double offsetFrequency = 0,
            double offsetFrequencyPeriodMultiple = 0,
            double sincCoefficient = 0,
            double astigmatismCompensationCoefficient = 0d,
            double sphericalAberrationCompensationCoefficient = 0d,
            double secondaryAstigmatismCompensationCoefficient = 0d,
            double comaCompensationCoefficient = 0d,
            double trefoilCompensationCoefficient = 0d,
            double quadrafoilCompensationCoefficient = 0d,
            double alphaOrder = 0d,
            double alphaOrderCoefficient = 0d,
            Point[]? frequencyAmplitudes = null,
            int generateRetryTimes = 1000)
    {
        // 5.742 = 音速(mm/us), 固体声速更快:
        const double chirpAodSoundSpeed = 5.742d;

        #region 参数判断

        Guard.IsGreaterThanOrEqualTo(bandWidth, 0d, nameof(bandWidth));
        Guard.IsGreaterThan(centerFrequency, 0d, nameof(centerFrequency));
        Guard.IsGreaterThan(sampleRate, 0d, nameof(sampleRate));
        Guard.IsGreaterThan(amplitude, 0d, nameof(amplitude));
        Guard.IsLessThanOrEqualTo(amplitude, 1d, nameof(amplitude));
        Guard.IsNotNullOrWhiteSpace(aodWaveDirectory, nameof(aodWaveDirectory));
        Guard.IsGreaterThanOrEqualTo(zeroSampleCount, 0, nameof(zeroSampleCount));
        Guard.IsGreaterThanOrEqualTo(endpointSampleCount, 0, nameof(endpointSampleCount));
        Guard.IsGreaterThan(generateRetryTimes, 0, nameof(generateRetryTimes));

        if (functionMonotonicTypeEnum is FunctionMonotonicTypeEnum.Flatness && bandWidth != 0) ThrowHelper.ThrowArgumentException(nameof(bandWidth), "if monotonic is flatness then band Width is must 0");
        if (bandWidth == 0 && functionMonotonicTypeEnum is not FunctionMonotonicTypeEnum.Flatness) ThrowHelper.ThrowArgumentException(nameof(functionMonotonicTypeEnum), "if band Width is must 0 then monotonic is flatness and band");

        var flatnessTime = flatnessTimeNullable switch
        {
            not null when flatnessTimeNullable > 0d && soundPacketLengthNullable is null => flatnessTimeNullable.Value,
            null when soundPacketLengthNullable > 0d => Math.Round(soundPacketLengthNullable.Value / chirpAodSoundSpeed * 1000d, MidpointRounding.AwayFromZero), // (ns): mm/(mm/us) * 1000 = us * 1000 = ns
            _ => ThrowHelper.ThrowArgumentOutOfRangeException<double>("if prescan : flatness > 0; if chirp: sound packet length > 0")
        };

        #endregion 参数判断

        var lowFrequency = functionMonotonicTypeEnum switch // 低频
        {
            FunctionMonotonicTypeEnum.Increasing or FunctionMonotonicTypeEnum.Deceasing => centerFrequency - bandWidth / 2d,
            FunctionMonotonicTypeEnum.Flatness => centerFrequency,
            _ => ThrowHelper.ThrowArgumentOutOfRangeException<double>(nameof(functionMonotonicTypeEnum))
        };
        var highFrequency = functionMonotonicTypeEnum switch // 高频
        {
            FunctionMonotonicTypeEnum.Increasing or FunctionMonotonicTypeEnum.Deceasing => centerFrequency + bandWidth / 2d,
            FunctionMonotonicTypeEnum.Flatness => centerFrequency,
            _ => ThrowHelper.ThrowArgumentOutOfRangeException<double>(nameof(functionMonotonicTypeEnum))
        };

        var readonlyBandWidth = bandWidth;
        var readonlyCenterFrequency = centerFrequency; // 中心频率
        var readonlyLowFrequency = lowFrequency;
        var readonlyHighFrequency = highFrequency;

        var numberOfSamples = (int)Math.Round(flatnessTime * sampleRate / 1000 + 2 * endpointSampleCount, MidpointRounding.AwayFromZero); // 总采样点的个数: ns * (Msa/s) / 1000 = ns * (Gsa/s) = (10^-9s)*(10^9sa/s) = sa

        #region 返回结果

        #region 文件

        var frequencyFileName = functionMonotonicTypeEnum switch
        {
            FunctionMonotonicTypeEnum.Increasing => $"{readonlyLowFrequency:0.###}Mhz_{readonlyHighFrequency:0.###}Mhz",
            FunctionMonotonicTypeEnum.Deceasing => $"{readonlyHighFrequency:0.###}Mhz_{readonlyLowFrequency:0.###}Mhz",
            FunctionMonotonicTypeEnum.Flatness => $"{readonlyCenterFrequency:0.###}Mhz_{readonlyCenterFrequency:0.###}Mhz",
            _ => ThrowHelper.ThrowArgumentOutOfRangeException<string>(nameof(functionMonotonicTypeEnum))
        };

        // $总byte长度$补零个数$包分割长度$下发寄存器号(02prescan, 03chirp)$偏移的频率$偏移的频率的2π周期的倍率$
        var aodWaveFileName = soundPacketLengthNullable is not null
            ? $"chirp" +
              $"_{soundPacketLengthNullable.Value:0.###}mm" +
              $"_{readonlyBandWidth:0.###}BWMhz" +
              $"_{frequencyFileName}" +
              $"_{flatnessTime:0.###}ns" +
              $"_{amplitude:0.###}AMP" +
              $"_{astigmatismCompensationCoefficient:0.###############}astigmatism" +
              $"_{sphericalAberrationCompensationCoefficient:0.###############}sphericalAberration" +
              $"_{secondaryAstigmatismCompensationCoefficient:0.###############}secondaryAstigmatism" +
              $"_{comaCompensationCoefficient:0.###############}coma" +
              $"_{trefoilCompensationCoefficient:0.###############}trefoil" +
              $"_{quadrafoilCompensationCoefficient:0.###############}quadrafoil" +
              $"${numberOfSamples + zeroSampleCount}${zeroSampleCount}$600$03${offsetFrequency:0.###}${offsetFrequencyPeriodMultiple:0.###}$.txt"
            : $"prescan" +
              $"_{readonlyBandWidth:0.###}BWMhz" +
              $"_{frequencyFileName}" +
              $"_{flatnessTime:0.###}ns" +
              $"_{amplitude:0.###}AMP" +
              $"_{astigmatismCompensationCoefficient:0.###############}astigmatism" +
              $"_{sphericalAberrationCompensationCoefficient:0.###############}sphericalAberration" +
              $"_{secondaryAstigmatismCompensationCoefficient:0.###############}secondaryAstigmatism" +
              $"_{comaCompensationCoefficient:0.###############}coma" +
              $"_{trefoilCompensationCoefficient:0.###############}trefoil" +
              $"_{quadrafoilCompensationCoefficient:0.###############}quadrafoil" +
              $"${numberOfSamples + zeroSampleCount}${zeroSampleCount}$600$02${offsetFrequency:0.###}${offsetFrequencyPeriodMultiple:0.###}$.txt";

        #endregion 文件

        var dt = 1d / sampleRate; // 每个采样点的时间间隔 (us/sa): 1 / (Msa/s) = 10^-6s/sa = us/sa

        var allSampleIndices = GenerateUtils.LinearIndexRange(0, numberOfSamples - 1);
        var headerSampleIndices = GenerateUtils.LinearIndexRange(0, endpointSampleCount - 1);
        var flatnessSampleIndices = GenerateUtils.LinearIndexRange(endpointSampleCount, numberOfSamples - endpointSampleCount - 1);
        var footerSampleIndices = GenerateUtils.LinearIndexRange(numberOfSamples - endpointSampleCount, numberOfSamples - 1);

        var aodWaveSignals = Vector<double>.Build.Dense(numberOfSamples);

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

        var minFlatnessFrequency = 0D;
        var maxFlatnessFrequency = 0D;

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
                        dComaFrequencies = comaCompensationCoefficient * (2 * Math.PI * t / t.Maximum()).PointwiseSin();
                        dTrefoilFrequencies = trefoilCompensationCoefficient * (6 * Math.PI * t / t.Maximum()).PointwiseSin();
                        dQuadrafoilFrequencies = quadrafoilCompensationCoefficient * (8 * Math.PI * t / t.Maximum()).PointwiseSin();
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

                if (dFlatnessFrequencies.Exists(f => f <= 0 || f > readonlyHighFrequency * 1.5)) ThrowHelper.ThrowArgumentException(nameof(dFlatnessFrequencies), $"Synthesized frequencies must be in the range (0, {readonlyHighFrequency * 1.5:f3} MHz)");

                #endregion 频率

                Generate(0, 0);

                #region 平坦部分的起始和终止频率

                var fftDerivative = fftMagnitudes.Differentiate() / fftFrequencies.Differentiate();
                var fftDerivativeFrequencies = fftFrequencies.SubVectorRange(0, fftFrequencies.Count - 2);

                // 寻找极值点
                const double threshold = 0.0001;
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
                    if (Math.Abs(centerFrequency - readonlyCenterFrequency) < sampleRate / flatnessSampleIndices.Length)
                    {
                        isSuccess = true;
                        break;
                    }

                    if (centerFrequency < readonlyCenterFrequency)
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
                    if (Math.Abs(minFlatnessFrequency - readonlyLowFrequency) < sampleRate / flatnessSampleIndices.Length)
                    {
                        if (Math.Abs(maxFlatnessFrequency - readonlyHighFrequency) < sampleRate / flatnessSampleIndices.Length)
                        {
                            isSuccess = true;
                            break;
                        }

                        if (maxFlatnessFrequency < readonlyHighFrequency)
                            bandWidth += sampleRate / (2d * flatnessSampleIndices.Length);
                        else
                            bandWidth -= sampleRate / (2d * flatnessSampleIndices.Length);
                    }
                    else if (minFlatnessFrequency < readonlyLowFrequency)
                        lowFrequency += sampleRate / (2d * flatnessSampleIndices.Length);
                    else
                        lowFrequency -= sampleRate / (2d * flatnessSampleIndices.Length);
                }
                else if (functionMonotonicTypeEnum == FunctionMonotonicTypeEnum.Deceasing)
                {
                    if (Math.Abs(maxFlatnessFrequency - readonlyHighFrequency) < sampleRate / flatnessSampleIndices.Length)
                    {
                        if (Math.Abs(minFlatnessFrequency - readonlyLowFrequency) < sampleRate / flatnessSampleIndices.Length)
                        {
                            isSuccess = true;
                            break;
                        }

                        if (minFlatnessFrequency < readonlyLowFrequency)
                            bandWidth -= sampleRate / (2d * flatnessSampleIndices.Length);
                        else
                            bandWidth += sampleRate / (2d * flatnessSampleIndices.Length);
                    }
                    else if (maxFlatnessFrequency < readonlyHighFrequency)
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

        Generate(offsetFrequency, offsetFrequencyPeriodMultiple);

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
                        ? 1
                        : 1 / (Math.Sin(x) / x + 1e-10);
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
            aodWaveSignals.SetSubVectorRange(flatnessSampleIndices[0], flatnessSampleIndices[^1], recoveredSignal);

            fftResult = recoveredSignal.ToComplex().FastFourierTransform();
            (fftFrequencies, fftMagnitudes) = fftResult.GetPositiveFrequencies(sampleRate);
        }

        /*
         * double[-1,1]归一化数据需要转换为16-bit或32-bit整数格式进行传输[DSP、FPGA、DAC数字信号转换为模拟信号, 目前这个是16-bit PCM(脉冲编码调制)格式
         * 1. aodWaveSignal ∈ [-1, 1] 归一化
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
        var aodWaveSignalResult = aodWaveSignals
            .Select(t => ConvertUtils.ToInt16NotOverflowException(Math.Round(Math.Pow(2, 15) * t, MidpointRounding.AwayFromZero)))
            .Select(Convert.ToInt64)
            .Select(t => t < 0 ? t + (long)Math.Pow(2, 32) : t)
            .ToArray();
        var hexStrings = aodWaveSignalResult
            .Select(t => t.ToString("X4")) // 转换为16进制补码字符串, 至少4位不足右边补0
            .Select(t => t[^4..]) // 字符串的最后 4 位
            .ToArray();

        var aodWaveFilePath = isSuccess
            ? Path.Combine(aodWaveDirectory, aodWaveFileName)
            : Path.Combine(aodWaveDirectory, $"Error_{aodWaveFileName}");
        aodWaveFilePath = FileHelper.GetEnsureLongPathSupport(aodWaveFilePath);

        DirectoryHelper.CreateFileDirectoryIfNotExists(aodWaveFilePath);
        FileHelper.DeleteFileIfExists(aodWaveFilePath);
        File.WriteAllText(aodWaveFilePath, string.Join(Environment.NewLine, hexStrings));

        return (
            isSuccess,
            aodWaveFilePath,
            flatnessSampleIndices.Select((t, index) => new Point(t + 1, dLinearFrequencies[index])).ToArray(),
            flatnessSampleIndices.Select((t, index) => new Point(t + 1, dFlatnessFrequencies[index])).ToArray(),
            flatnessSampleIndices.Select((t, index) => new Point(t + 1, dAstigmatismFrequencies[index])).ToArray(),
            flatnessSampleIndices.Select((t, index) => new Point(t + 1, dSphericalAberrationFrequencies[index])).ToArray(),
            flatnessSampleIndices.Select((t, index) => new Point(t + 1, dSecondaryAstigmatismFrequencies[index])).ToArray(),
            flatnessSampleIndices.Select((t, index) => new Point(t + 1, dComaFrequencies[index])).ToArray(),
            flatnessSampleIndices.Select((t, index) => new Point(t + 1, dTrefoilFrequencies[index])).ToArray(),
            flatnessSampleIndices.Select((t, index) => new Point(t + 1, dQuadrafoilFrequencies[index])).ToArray(),
            flatnessSampleIndices.Select((t, index) => new Point(t + 1, dAlphaOrderFrequencies[index])).ToArray(),
            allSampleIndices.Select(t => new Point(t + 1, aodWaveSignals[t])).ToArray(),
            fftFrequencies.Zip(fftMagnitudes, (f, m) => new Point(f, m)).ToArray(),
            frequencyCompensationsResult.ToArray(),
            exception);

        void Generate(double currentOffsetFrequency, double currentOffsetFrequencyPeriodMultiple)
        {
            #region 相位

            var dHeaderPhases = 2 * Math.PI * dHeaderFrequencies * dt;
            var headerPhases = dHeaderPhases.IntegrateCumulative();
            var dFlatnessPhases = 2 * Math.PI * dFlatnessFrequencies * dt;
            var flatnessPhases = dFlatnessPhases.IntegrateCumulative();
            if (currentOffsetFrequency != 0) flatnessPhases += 2 * Math.PI * dFlatnessFrequencies * currentOffsetFrequencyPeriodMultiple * 1 / currentOffsetFrequency;
            var dFooterPhases = 2 * Math.PI * dFooterFrequencies * dt;
            var footerPhases = dFooterPhases.IntegrateCumulative();

            #endregion 相位

            #region 波形

            if (headerSampleIndices.Length > 0) aodWaveSignals.SetSubVectorRange(headerSampleIndices[0], headerSampleIndices[^1], amplitude * headerPhases.PointwiseCos().PointwiseMultiply(Vector<double>.Build.DenseOfArray(headerSampleIndices) / headerSampleIndices.Length));
            aodWaveSignals.SetSubVectorRange(flatnessSampleIndices[0], flatnessSampleIndices[^1], amplitude * flatnessPhases.PointwiseCos());
            if (footerSampleIndices.Length > 0) aodWaveSignals.SetSubVectorRange(footerSampleIndices[0], footerSampleIndices[^1], amplitude * footerPhases.PointwiseCos().PointwiseMultiply(1 - (Vector<double>.Build.DenseOfArray(footerSampleIndices) - footerSampleIndices[0] + 1) / footerSampleIndices.Length));

            #endregion 波形

            #region 傅里叶

            var flatnessAodWaveSignals = aodWaveSignals.SubVectorRange(flatnessSampleIndices[0], flatnessSampleIndices[^1]);
            fftResult = flatnessAodWaveSignals.ToComplex().FastFourierTransform();
            (fftFrequencies, fftMagnitudes) = fftResult.GetPositiveFrequencies(sampleRate);

            #endregion 傅里叶
        }
    }
}