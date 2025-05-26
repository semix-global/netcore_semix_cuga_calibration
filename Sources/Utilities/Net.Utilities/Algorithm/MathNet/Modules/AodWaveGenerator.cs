using CommunityToolkit.Diagnostics;
using MathNet.Numerics;
using MathNet.Numerics.LinearAlgebra;
using Net.Utilities.Algorithm.MathNet.Helper;
using Net.Utilities.Enums.Maths;
using Net.Utilities.Helper.File;
using Net.Utilities.Models;

namespace Net.Utilities.Algorithm.MathNet.Modules;

public static class AodWaveGenerator
{
    /// <summary>
    /// 生成Chirp AOD波形文件, [中心频率 - 带宽/2, 中心频率 + 带宽/2]
    /// </summary>
    /// <param name="bandWidth">带宽(MHz)</param>
    /// <param name="centerFrequency">中心频率(Mhz)</param>
    /// <param name="soundPacketLength">音包长度(mm)</param>
    /// <param name="monotonicTypeEnum">递增, 递减, 平坦</param>
    /// <param name="sampleRate">采样率(Msa/s)</param>
    /// <param name="amplitude">幅值</param>
    /// <param name="aodWaveDirectory">生成的目录</param>
    /// <param name="zeroSampleCount">前面添加多少补零采样点个数, 相当于添加延迟(sa)</param>
    /// <param name="endpointSampleCount">端点头尾添加多少采样点个数, 缓冲(XTC响应不够)(sa)</param>
    /// <param name="sincCoefficient">sin(cx)/cx</param>
    /// <param name="astigmatismCompensationCoefficient">二次补偿系数t^2 散光</param>
    /// <param name="sphericalAberrationCompensationCoefficient">三次补偿系数t^3 球差</param>
    /// <param name="secondaryAstigmatismCompensationCoefficient">四次补偿系数t^4 二阶散光</param>
    /// <param name="comaCompensationCoefficient">sin(2πt/T)</param>
    /// <param name="trefoilCompensationCoefficient">sin(6πt/T)</param>
    /// <param name="quadrafoilCompensationCoefficient">sin(8πt/T)</param>
    /// <param name="alphaOrder">α次补偿</param>
    /// <param name="alphaOrderCoefficient">α次补偿系数t^α</param>
    /// <param name="frequencyCompensations">波形频率生成补偿系数</param>
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
        Point[] AodWaveFlatnessNonLinearFrequencySignals,
        Point[] AodWaveFlatnessTotalFrequencySignals,
        Point[] AodWaveFlatnessLinearCompensationSignals,
        Point[] AodWaveFlatnessAstigmatismCompensationSignals,
        Point[] AodWaveFlatnessSphericalAberrationCompensationSignals,
        Point[] AodWaveFlatnessSecondaryAstigmatismCompensationSignals,
        Point[] AodWaveFlatnessComaCompensationSignals,
        Point[] AodWaveFlatnessTrefoilCompensationSignals,
        Point[] AodWaveFlatnessQuadrafoilCompensationSignals,
        Point[] AodWaveFlatnessAlphaOrderCompensationSignals,
        Point[] AodWaveFlatnessNonlinearCompensationSignals,
        Point[] AodWaveFlatnessTotalCompensationSignals,
        Point[] AodWaveSignals,
        Point[] AodWaveSignalsFourier,
        Point[] AodWaveSignalsSinc,
        Exception? Exception) GenerateChirpAodWaveFile(
            double bandWidth,
            double centerFrequency,
            double soundPacketLength,
            MonotonicTypeEnum monotonicTypeEnum,
            double sampleRate,
            double amplitude,
            string aodWaveDirectory,
            int zeroSampleCount = 0,
            int endpointSampleCount = 0,
            double sincCoefficient = 0,
            double astigmatismCompensationCoefficient = 0d,
            double sphericalAberrationCompensationCoefficient = 0d,
            double secondaryAstigmatismCompensationCoefficient = 0d,
            double comaCompensationCoefficient = 0d,
            double trefoilCompensationCoefficient = 0d,
            double quadrafoilCompensationCoefficient = 0d,
            double alphaOrder = 0d,
            double alphaOrderCoefficient = 0d,
            Point[]? frequencyCompensations = null,
            int generateRetryTimes = 1000)
        => GenerateAodWaveFile(
            bandWidth,
            centerFrequency,
            monotonicTypeEnum,
            sampleRate,
            amplitude,
            aodWaveDirectory,
            zeroSampleCount: zeroSampleCount,
            flatnessTimeNullable: null,
            soundPacketLengthNullable: soundPacketLength,
            endpointSampleCount: endpointSampleCount,
            sincCoefficient: sincCoefficient,
            astigmatismCompensationCoefficient: astigmatismCompensationCoefficient,
            sphericalAberrationCompensationCoefficient: sphericalAberrationCompensationCoefficient,
            secondaryAstigmatismCompensationCoefficient: secondaryAstigmatismCompensationCoefficient,
            comaCompensationCoefficient: comaCompensationCoefficient,
            trefoilCompensationCoefficient: trefoilCompensationCoefficient,
            quadrafoilCompensationCoefficient: quadrafoilCompensationCoefficient,
            alphaOrder: alphaOrder,
            alphaOrderCoefficient: alphaOrderCoefficient,
            frequencyCompensations: frequencyCompensations,
            generateRetryTimes: generateRetryTimes);

    /// <summary>
    /// 生成Prescan AOD波形文件, [中心频率 - 带宽/2, 中心频率 + 带宽/2]
    /// </summary>
    /// <param name="bandWidth">带宽(MHz)</param>
    /// <param name="centerFrequency">中心频率(Mhz)</param>
    /// <param name="flatnessTime">平坦时间(ns)</param>
    /// <param name="monotonicTypeEnum">递增, 递减, 平坦</param>
    /// <param name="sampleRate">采样率(Msa/s)</param>
    /// <param name="amplitude">幅值</param>
    /// <param name="aodWaveDirectory">生成的目录</param>
    /// <param name="zeroSampleCount">前面添加多少补零采样点个数, 相当于添加延迟(sa)</param>
    /// <param name="endpointSampleCount">端点头尾添加多少采样点个数, 缓冲(XTC响应不够)(sa)</param>
    /// <param name="sincCoefficient">sin(cx)/cx</param>
    /// <param name="astigmatismCompensationCoefficient">二次补偿系数t^2 散光</param>
    /// <param name="sphericalAberrationCompensationCoefficient">三次补偿系数t^3 球差</param>
    /// <param name="secondaryAstigmatismCompensationCoefficient">四次补偿系数t^4 二阶散光</param>
    /// <param name="comaCompensationCoefficient">sin(2πt/T)</param>
    /// <param name="trefoilCompensationCoefficient">sin(6πt/T)</param>
    /// <param name="quadrafoilCompensationCoefficient">sin(8πt/T)</param>
    /// <param name="alphaOrder">α次补偿</param>
    /// <param name="alphaOrderCoefficient">α次补偿系数t^α</param>
    /// <param name="frequencyCompensations">波形频率生成补偿系数</param>
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
        Point[] AodWaveFlatnessNonLinearFrequencySignals,
        Point[] AodWaveFlatnessTotalFrequencySignals,
        Point[] AodWaveFlatnessLinearCompensationSignals,
        Point[] AodWaveFlatnessAstigmatismCompensationSignals,
        Point[] AodWaveFlatnessSphericalAberrationCompensationSignals,
        Point[] AodWaveFlatnessSecondaryAstigmatismCompensationSignals,
        Point[] AodWaveFlatnessComaCompensationSignals,
        Point[] AodWaveFlatnessTrefoilCompensationSignals,
        Point[] AodWaveFlatnessQuadrafoilCompensationSignals,
        Point[] AodWaveFlatnessAlphaOrderCompensationSignals,
        Point[] AodWaveFlatnessNonlinearCompensationSignals,
        Point[] AodWaveFlatnessTotalCompensationSignals,
        Point[] AodWaveSignals,
        Point[] AodWaveSignalsFourier,
        Point[] AodWaveSignalsSinc,
        Exception? Exception) GeneratePrescanAodWaveFile(
            double bandWidth,
            double centerFrequency,
            double flatnessTime,
            MonotonicTypeEnum monotonicTypeEnum,
            double sampleRate,
            double amplitude,
            string aodWaveDirectory,
            int zeroSampleCount = 0,
            int endpointSampleCount = 0,
            double sincCoefficient = 0,
            double astigmatismCompensationCoefficient = 0d,
            double sphericalAberrationCompensationCoefficient = 0d,
            double secondaryAstigmatismCompensationCoefficient = 0d,
            double comaCompensationCoefficient = 0d,
            double trefoilCompensationCoefficient = 0d,
            double quadrafoilCompensationCoefficient = 0d,
            double alphaOrder = 0d,
            double alphaOrderCoefficient = 0d,
            Point[]? frequencyCompensations = null,
            int generateRetryTimes = 1000)
        => GenerateAodWaveFile(
            bandWidth,
            centerFrequency,
            monotonicTypeEnum,
            sampleRate,
            amplitude,
            aodWaveDirectory,
            zeroSampleCount: zeroSampleCount,
            flatnessTimeNullable: flatnessTime,
            soundPacketLengthNullable: null,
            endpointSampleCount: endpointSampleCount,
            sincCoefficient: sincCoefficient,
            astigmatismCompensationCoefficient: astigmatismCompensationCoefficient,
            sphericalAberrationCompensationCoefficient: sphericalAberrationCompensationCoefficient,
            secondaryAstigmatismCompensationCoefficient: secondaryAstigmatismCompensationCoefficient,
            comaCompensationCoefficient: comaCompensationCoefficient,
            trefoilCompensationCoefficient: trefoilCompensationCoefficient,
            quadrafoilCompensationCoefficient: quadrafoilCompensationCoefficient,
            alphaOrder: alphaOrder,
            alphaOrderCoefficient: alphaOrderCoefficient,
            frequencyCompensations: frequencyCompensations,
            generateRetryTimes: generateRetryTimes);

    /// <summary>
    /// 生成AOD波形文件, [中心频率 - 带宽/2, 中心频率 + 带宽/2]
    /// </summary>
    /// <param name="bandWidth">带宽(MHz)</param>
    /// <param name="centerFrequency">中心频率(Mhz)</param>
    /// <param name="monotonicTypeEnum">递增, 递减, 平坦</param>
    /// <param name="sampleRate">采样率(Msa/s)</param>
    /// <param name="amplitude">幅值</param>
    /// <param name="aodWaveDirectory">生成的目录</param>
    /// <param name="zeroSampleCount">前面添加多少补零采样点个数, 相当于添加延迟(sa)</param>
    /// <param name="flatnessTimeNullable">平坦时间(ns)</param>
    /// <param name="soundPacketLengthNullable">音包长度(mm)</param>
    /// <param name="endpointSampleCount">端点头尾添加多少采样点个数, 缓冲(XTC响应不够)(sa)</param>
    /// <param name="sincCoefficient">sin(cx)/cx</param>
    /// <param name="astigmatismCompensationCoefficient">二次补偿系数t^2 散光</param>
    /// <param name="sphericalAberrationCompensationCoefficient">三次补偿系数t^3 球差</param>
    /// <param name="secondaryAstigmatismCompensationCoefficient">四次补偿系数t^4 二阶散光</param>
    /// <param name="comaCompensationCoefficient">sin(2πt/T)</param>
    /// <param name="trefoilCompensationCoefficient">sin(6πt/T)</param>
    /// <param name="quadrafoilCompensationCoefficient">sin(8πt/T)</param>
    /// <param name="alphaOrder">α次补偿</param>
    /// <param name="alphaOrderCoefficient">α次补偿系数t^α</param>
    /// <param name="frequencyCompensations">波形频率生成补偿系数</param>
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
    private static (
        bool IsSuccess,
        string AodWaveFilePath,
        Point[] AodWaveFlatnessLinearFrequencySignals,
        Point[] AodWaveFlatnessNonLinearFrequencySignals,
        Point[] AodWaveFlatnessTotalFrequencySignals,
        Point[] AodWaveFlatnessLinearCompensationSignals,
        Point[] AodWaveFlatnessAstigmatismCompensationSignals,
        Point[] AodWaveFlatnessSphericalAberrationCompensationSignals,
        Point[] AodWaveFlatnessSecondaryAstigmatismCompensationSignals,
        Point[] AodWaveFlatnessComaCompensationSignals,
        Point[] AodWaveFlatnessTrefoilCompensationSignals,
        Point[] AodWaveFlatnessQuadrafoilCompensationSignals,
        Point[] AodWaveFlatnessAlphaOrderCompensationSignals,
        Point[] AodWaveFlatnessNonlinearCompensationSignals,
        Point[] AodWaveFlatnessTotalCompensationSignals,
        Point[] AodWaveSignals,
        Point[] AodWaveSignalsFourier,
        Point[] AodWaveSignalsSinc,
        Exception? Exception) GenerateAodWaveFile(
            double bandWidth,
            double centerFrequency,
            MonotonicTypeEnum monotonicTypeEnum,
            double sampleRate,
            double amplitude,
            string aodWaveDirectory,
            int zeroSampleCount = 0,
            double? flatnessTimeNullable = null,
            double? soundPacketLengthNullable = null,
            int endpointSampleCount = 0,
            double sincCoefficient = 0,
            double astigmatismCompensationCoefficient = 0d,
            double sphericalAberrationCompensationCoefficient = 0d,
            double secondaryAstigmatismCompensationCoefficient = 0d,
            double comaCompensationCoefficient = 0d,
            double trefoilCompensationCoefficient = 0d,
            double quadrafoilCompensationCoefficient = 0d,
            double alphaOrder = 0d,
            double alphaOrderCoefficient = 0d,
            Point[]? frequencyCompensations = null,
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

        Guard.IsGreaterThanOrEqualTo(sincCoefficient, 0d, nameof(sincCoefficient));

        Guard.IsGreaterThanOrEqualTo(astigmatismCompensationCoefficient, -1d, nameof(astigmatismCompensationCoefficient));
        Guard.IsLessThanOrEqualTo(astigmatismCompensationCoefficient, 1d, nameof(astigmatismCompensationCoefficient));
        Guard.IsGreaterThanOrEqualTo(sphericalAberrationCompensationCoefficient, -1d, nameof(sphericalAberrationCompensationCoefficient));
        Guard.IsLessThanOrEqualTo(sphericalAberrationCompensationCoefficient, 1d, nameof(sphericalAberrationCompensationCoefficient));
        Guard.IsGreaterThanOrEqualTo(secondaryAstigmatismCompensationCoefficient, -1d, nameof(secondaryAstigmatismCompensationCoefficient));
        Guard.IsLessThanOrEqualTo(secondaryAstigmatismCompensationCoefficient, 1d, nameof(secondaryAstigmatismCompensationCoefficient));
        Guard.IsGreaterThanOrEqualTo(comaCompensationCoefficient, -100d, nameof(comaCompensationCoefficient));
        Guard.IsLessThanOrEqualTo(comaCompensationCoefficient, 100d, nameof(comaCompensationCoefficient));
        Guard.IsGreaterThanOrEqualTo(trefoilCompensationCoefficient, -100d, nameof(trefoilCompensationCoefficient));
        Guard.IsLessThanOrEqualTo(trefoilCompensationCoefficient, 100d, nameof(trefoilCompensationCoefficient));
        Guard.IsGreaterThanOrEqualTo(quadrafoilCompensationCoefficient, -100d, nameof(quadrafoilCompensationCoefficient));
        Guard.IsLessThanOrEqualTo(quadrafoilCompensationCoefficient, 100d, nameof(quadrafoilCompensationCoefficient));

        Guard.IsGreaterThan(generateRetryTimes, 0, nameof(generateRetryTimes));

        if (monotonicTypeEnum is MonotonicTypeEnum.Flatness && bandWidth != 0) ThrowHelper.ThrowArgumentException(nameof(bandWidth), "if monotonic is flatness then band Width is must 0");
        if (bandWidth == 0 && monotonicTypeEnum is not MonotonicTypeEnum.Flatness) ThrowHelper.ThrowArgumentException(nameof(monotonicTypeEnum), "if band Width is must 0 then monotonic is flatness and band");

        var flatnessTime = flatnessTimeNullable switch
        {
            not null when flatnessTimeNullable > 0d && soundPacketLengthNullable is null => flatnessTimeNullable.Value,
            // mm/(mm/us)*1000 = ns(10^-9s)
            null when soundPacketLengthNullable > 0d => Math.Round(soundPacketLengthNullable.Value / chirpAodSoundSpeed * 1000d, MidpointRounding.AwayFromZero),
            _ => ThrowHelper.ThrowArgumentOutOfRangeException<double>("if prescan : flatness > 0; if chirp: sound packet length > 0")
        }; // 平坦时间ns

        #endregion 参数判断

        var readonlyCenterFrequency = centerFrequency; // 中心频率
        var readonlyLowFrequency = monotonicTypeEnum switch // 低频
        {
            MonotonicTypeEnum.Increasing or MonotonicTypeEnum.Deceasing => centerFrequency - bandWidth / 2d,
            MonotonicTypeEnum.Flatness => readonlyCenterFrequency,
            _ => ThrowHelper.ThrowArgumentOutOfRangeException<double>(nameof(monotonicTypeEnum))
        };
        var readonlyHighFrequency = monotonicTypeEnum switch // 高频
        {
            MonotonicTypeEnum.Increasing or MonotonicTypeEnum.Deceasing => centerFrequency + bandWidth / 2d,
            MonotonicTypeEnum.Flatness => readonlyCenterFrequency,
            _ => ThrowHelper.ThrowArgumentOutOfRangeException<double>(nameof(monotonicTypeEnum))
        };

        var lowFrequency = readonlyLowFrequency;
        var highFrequency = readonlyHighFrequency;
        var bandwidthTemp = bandWidth;

        // ns*(Msa/s)/1000 = (10^-9s)*(10^6sa/s)/(10^-3) = (10^-3sa)/(10^-3) = sa
        var totalSampleCount = (int)Math.Round(flatnessTime * sampleRate / 1000 + 2 * endpointSampleCount, MidpointRounding.AwayFromZero); // 总采样点的个数

        // 总采样点的索引Array
        var totalSampleIndices = Generate.LinearRangeInt32(0, 1, totalSampleCount - 1); // 以1为起始，1为步长，截至的数值(<= n): 等差数列 1, 2, 3, ..., n
        var headerSampleIndices = Generate.LinearRangeInt32(0, 1, endpointSampleCount - 1);
        var flatnessSampleIndices = Generate.LinearRangeInt32(endpointSampleCount, 1, totalSampleCount - endpointSampleCount - 1);
        var footerSampleIndices = Generate.LinearRangeInt32(totalSampleCount - endpointSampleCount, 1, totalSampleCount - 1);

        #region 返回结果

        var frequencyFileName = monotonicTypeEnum switch
        {
            MonotonicTypeEnum.Increasing => $"{readonlyLowFrequency:0.###}Mhz_{readonlyHighFrequency:0.###}Mhz",
            MonotonicTypeEnum.Deceasing => $"{readonlyHighFrequency:0.###}Mhz_{readonlyLowFrequency:0.###}Mhz",
            MonotonicTypeEnum.Flatness => $"{readonlyCenterFrequency:0.###}Mhz_{readonlyCenterFrequency:0.###}Mhz",
            _ => ThrowHelper.ThrowArgumentOutOfRangeException<string>(nameof(monotonicTypeEnum))
        };
        // $总byte长度$补零个数$不知道含义$下发寄存器号（02prescan，03chirp）
        var aodWaveFilePath = soundPacketLengthNullable is not null
            ? Path.Combine(aodWaveDirectory, $"chirp" +
                                             $"_{soundPacketLengthNullable.Value:0.###}mm" +
                                             $"_{bandWidth:0.###}BWMhz" +
                                             $"_{frequencyFileName}" +
                                             $"_{flatnessTime:0.###}ns" +
                                             $"_{amplitude:0.###}AMP" +
                                             $"_{astigmatismCompensationCoefficient:0.###############}astigmatism" +
                                             $"_{sphericalAberrationCompensationCoefficient:0.###############}sphericalAberration" +
                                             $"_{secondaryAstigmatismCompensationCoefficient:0.###############}secondaryAstigmatism" +
                                             $"_{comaCompensationCoefficient:0.###############}coma" +
                                             $"_{trefoilCompensationCoefficient:0.###############}trefoil" +
                                             $"_{quadrafoilCompensationCoefficient:0.###############}quadrafoil" +
                                             $"_{totalSampleCount}Count" +
                                             $"${totalSampleCount}${zeroSampleCount}$600$03$.txt")
            : Path.Combine(aodWaveDirectory, $"prescan" +
                                             $"_{bandWidth:0.###}BWMhz" +
                                             $"_{frequencyFileName}" +
                                             $"_{flatnessTime:0.###}ns" +
                                             $"_{amplitude:0.###}AMP" +
                                             $"_{astigmatismCompensationCoefficient:0.###############}astigmatism" +
                                             $"_{sphericalAberrationCompensationCoefficient:0.###############}sphericalAberration" +
                                             $"_{secondaryAstigmatismCompensationCoefficient:0.###############}secondaryAstigmatism" +
                                             $"_{comaCompensationCoefficient:0.###############}coma" +
                                             $"_{trefoilCompensationCoefficient:0.###############}trefoil" +
                                             $"_{quadrafoilCompensationCoefficient:0.###############}quadrafoil" +
                                             $"_{totalSampleCount}Count" +
                                             $"${totalSampleCount}${zeroSampleCount}$600$02$.txt");

        aodWaveFilePath = FileHelper.GetEnsureLongPathSupport(aodWaveFilePath);

        var aodWaveSignals = new double[totalSampleCount];
        double[] aodWaveFlatnessLinearFrequencySignals = [];
        double[] aodWaveFlatnessNonLinearFrequencySignals = [];
        double[] aodWaveFlatnessTotalFrequencySignals = [];
        double[] aodWaveFlatnessLinearCompensationSignals = [];
        double[] aodWaveFlatnessAstigmatismCompensationSignals = [];
        double[] aodWaveFlatnessSphericalAberrationCompensationSignals = [];
        double[] aodWaveFlatnessSecondaryAstigmatismCompensationSignals = [];
        double[] aodWaveFlatnessComaCompensationSignals = [];
        double[] aodWaveFlatnessTrefoilCompensationSignals = [];
        double[] aodWaveFlatnessQuadrafoilCompensationSignals = [];
        double[] aodWaveFlatnessAlphaOrderCompensationSignals = [];
        double[] aodWaveFlatnessNonlinearCompensationSignals = [];
        double[] aodWaveFlatnessTotalCompensationSignals = [];
        Point[] aodWaveSignalsFourier = [];
        double[] aodWaveSignalsSinc = [];
        bool isSuccess;
        Exception? exception = null;

        #endregion 返回结果

        var count = 1;
        while (true)
        {
            try
            {
                var ramp = bandwidthTemp / flatnessSampleIndices.Length; // MHz/ns 斜率

                #region Flatness

                var flatnessSampleX = flatnessSampleIndices
                    .Select(t => (
                        X: (t + 1) - (flatnessSampleIndices[0] + 1), // 起点补偿
                        XOfCenter: (t + 1) - ((flatnessSampleIndices[0] + 1) + (flatnessSampleIndices[^1] + 1)) / 2d)) // 中心补偿
                    .ToArray();

                aodWaveFlatnessLinearCompensationSignals =
                [
                    .. flatnessSampleX.Select(t =>
                        monotonicTypeEnum switch
                        {
                            MonotonicTypeEnum.Increasing => ramp / 2d * t.X,
                            MonotonicTypeEnum.Deceasing => -ramp / 2d * t.X,
                            MonotonicTypeEnum.Flatness => 0,
                            _ => ThrowHelper.ThrowArgumentOutOfRangeException<double>(nameof(monotonicTypeEnum))
                        })
                ];
                aodWaveFlatnessAstigmatismCompensationSignals =
                [
                    .. flatnessSampleX.Select(t =>
                        monotonicTypeEnum switch
                        {
                            MonotonicTypeEnum.Increasing => astigmatismCompensationCoefficient * ramp / 2d * Math.Pow(t.XOfCenter, 2),
                            MonotonicTypeEnum.Deceasing => -astigmatismCompensationCoefficient * ramp / 2d * Math.Pow(t.XOfCenter, 2),
                            MonotonicTypeEnum.Flatness => 0,
                            _ => ThrowHelper.ThrowArgumentOutOfRangeException<double>(nameof(monotonicTypeEnum))
                        })
                ];
                aodWaveFlatnessSphericalAberrationCompensationSignals =
                [
                    .. flatnessSampleX.Select(t =>
                        monotonicTypeEnum switch
                        {
                            MonotonicTypeEnum.Increasing => sphericalAberrationCompensationCoefficient * ramp / 2d * Math.Pow(t.XOfCenter, 3),
                            MonotonicTypeEnum.Deceasing => -sphericalAberrationCompensationCoefficient * ramp / 2d * Math.Pow(t.XOfCenter, 3),
                            MonotonicTypeEnum.Flatness => 0,
                            _ => ThrowHelper.ThrowArgumentOutOfRangeException<double>(nameof(monotonicTypeEnum))
                        })
                ];
                aodWaveFlatnessSecondaryAstigmatismCompensationSignals =
                [
                    .. flatnessSampleX.Select(t =>
                        monotonicTypeEnum switch
                        {
                            MonotonicTypeEnum.Increasing => secondaryAstigmatismCompensationCoefficient * ramp / 2d * Math.Pow(t.XOfCenter, 4),
                            MonotonicTypeEnum.Deceasing => -secondaryAstigmatismCompensationCoefficient * ramp / 2d * Math.Pow(t.XOfCenter, 4),
                            MonotonicTypeEnum.Flatness => 0,
                            _ => ThrowHelper.ThrowArgumentOutOfRangeException<double>(nameof(monotonicTypeEnum))
                        })
                ];
                aodWaveFlatnessComaCompensationSignals =
                [
                    .. flatnessSampleX.Select(t =>
                        monotonicTypeEnum switch
                        {
                            MonotonicTypeEnum.Increasing => comaCompensationCoefficient * Math.Sin(2 * Math.PI * t.X / flatnessSampleIndices.Length),
                            MonotonicTypeEnum.Deceasing => -comaCompensationCoefficient * Math.Sin(2 * Math.PI * t.X / flatnessSampleIndices.Length),
                            MonotonicTypeEnum.Flatness => 0,
                            _ => ThrowHelper.ThrowArgumentOutOfRangeException<double>(nameof(monotonicTypeEnum))
                        })
                ];
                aodWaveFlatnessTrefoilCompensationSignals =
                [
                    .. flatnessSampleX.Select(t =>
                        monotonicTypeEnum switch
                        {
                            MonotonicTypeEnum.Increasing => trefoilCompensationCoefficient * Math.Sin(6 * Math.PI * t.X / flatnessSampleIndices.Length),
                            MonotonicTypeEnum.Deceasing => -trefoilCompensationCoefficient * Math.Sin(6 * Math.PI * t.X / flatnessSampleIndices.Length),
                            MonotonicTypeEnum.Flatness => 0,
                            _ => ThrowHelper.ThrowArgumentOutOfRangeException<double>(nameof(monotonicTypeEnum))
                        })
                ];
                aodWaveFlatnessQuadrafoilCompensationSignals =
                [
                    .. flatnessSampleX.Select(t =>
                        monotonicTypeEnum switch
                        {
                            MonotonicTypeEnum.Increasing => quadrafoilCompensationCoefficient * Math.Sin(8 * Math.PI * t.X / flatnessSampleIndices.Length),
                            MonotonicTypeEnum.Deceasing => -quadrafoilCompensationCoefficient * Math.Sin(8 * Math.PI * t.X / flatnessSampleIndices.Length),
                            MonotonicTypeEnum.Flatness => 0,
                            _ => ThrowHelper.ThrowArgumentOutOfRangeException<double>(nameof(monotonicTypeEnum))
                        })
                ];
                aodWaveFlatnessAlphaOrderCompensationSignals =
                [
                    .. flatnessSampleX.Select(t =>
                        monotonicTypeEnum switch
                        {
                            MonotonicTypeEnum.Increasing => alphaOrderCoefficient * Math.Pow(t.X, alphaOrder),
                            MonotonicTypeEnum.Deceasing => -alphaOrderCoefficient * Math.Pow(t.X, alphaOrder),
                            MonotonicTypeEnum.Flatness => 0,
                            _ => ThrowHelper.ThrowArgumentOutOfRangeException<double>(nameof(monotonicTypeEnum))
                        })
                ];
                aodWaveFlatnessNonlinearCompensationSignals = flatnessSampleX.Select((_, index) =>
                    aodWaveFlatnessAstigmatismCompensationSignals[index] +
                    aodWaveFlatnessSphericalAberrationCompensationSignals[index] +
                    aodWaveFlatnessSecondaryAstigmatismCompensationSignals[index] +
                    aodWaveFlatnessComaCompensationSignals[index] +
                    aodWaveFlatnessTrefoilCompensationSignals[index] +
                    aodWaveFlatnessQuadrafoilCompensationSignals[index] +
                    aodWaveFlatnessAlphaOrderCompensationSignals[index]
                ).ToArray();
                aodWaveFlatnessTotalCompensationSignals = flatnessSampleX.Select((_, index) =>
                    aodWaveFlatnessLinearCompensationSignals[index] +
                    aodWaveFlatnessNonlinearCompensationSignals[index]
                ).ToArray();

                aodWaveFlatnessLinearFrequencySignals = flatnessSampleX.Select((_, index) =>
                    monotonicTypeEnum switch
                    {
                        MonotonicTypeEnum.Increasing => lowFrequency + aodWaveFlatnessLinearCompensationSignals[index],
                        MonotonicTypeEnum.Deceasing => highFrequency + aodWaveFlatnessLinearCompensationSignals[index],
                        MonotonicTypeEnum.Flatness => centerFrequency,
                        _ => ThrowHelper.ThrowArgumentOutOfRangeException<double>(nameof(monotonicTypeEnum))
                    }).ToArray();
                aodWaveFlatnessNonLinearFrequencySignals = flatnessSampleX.Select((_, index) =>
                    monotonicTypeEnum switch
                    {
                        MonotonicTypeEnum.Increasing => lowFrequency + aodWaveFlatnessNonlinearCompensationSignals[index],
                        MonotonicTypeEnum.Deceasing => highFrequency + aodWaveFlatnessNonlinearCompensationSignals[index],
                        MonotonicTypeEnum.Flatness => centerFrequency,
                        _ => ThrowHelper.ThrowArgumentOutOfRangeException<double>(nameof(monotonicTypeEnum))
                    }).ToArray();
                aodWaveFlatnessTotalFrequencySignals = flatnessSampleX.Select((_, index) =>
                    monotonicTypeEnum switch
                    {
                        MonotonicTypeEnum.Increasing => lowFrequency + aodWaveFlatnessTotalCompensationSignals[index],
                        MonotonicTypeEnum.Deceasing => highFrequency + aodWaveFlatnessTotalCompensationSignals[index],
                        MonotonicTypeEnum.Flatness => centerFrequency,
                        _ => ThrowHelper.ThrowArgumentOutOfRangeException<double>(nameof(monotonicTypeEnum))
                    }).ToArray();

                var flatnessSampleFrequencies = flatnessSampleX.Select(t =>
                    monotonicTypeEnum switch
                    {
                        MonotonicTypeEnum.Increasing => lowFrequency + (highFrequency - lowFrequency) * t.X / flatnessSampleIndices.Length,
                        MonotonicTypeEnum.Deceasing => highFrequency - (highFrequency - lowFrequency) * t.X / flatnessSampleIndices.Length,
                        MonotonicTypeEnum.Flatness => centerFrequency,
                        _ => ThrowHelper.ThrowArgumentOutOfRangeException<double>(nameof(monotonicTypeEnum))
                    }).ToArray();
                var eta = flatnessSampleFrequencies
                    .Select(t => monotonicTypeEnum != MonotonicTypeEnum.Flatness
                        ? sincCoefficient * (t - (highFrequency + lowFrequency) / 2) / (highFrequency - lowFrequency)
                        : 0d)
                    .Select(t => t == 0d ? 1d : Math.Sin(t) / t)
                    .Select(t => Math.Pow(t, 2))
                    .ToArray();
                var arf = eta
                    .Select(t => t == 0d ? 0.1 : t)
                    .Select(t => 1 / Math.Sqrt(t))
                    .ToArray();
                aodWaveSignalsSinc = arf.Select(t => t / arf.Max()).ToArray();

                /*
                 * 非固定频率 cos(2*pi*f*x) * 固定增益
                 * f = aodWaveTotalFrequencySignals[i], T = 2*pi/(2*pi*f) = 1/f
                 * x = (aodWaveTotalFrequencySignals[i] + 1) / sampleRate
                 * 单位: Mhz*sa/(Msa/s) = (10^6s^-1)*sa/(10^6sa/s) = (10^6s^-1)/(10^6s^-1)*sa/sa = 无量纲
                 */
                for (var i = 0; i < flatnessSampleIndices.Length; i++)
                {
                    var frequency = aodWaveFlatnessTotalFrequencySignals[i];
                    double? compensation = null;
                    if (frequencyCompensations is not null)
                    {
                        if (BinarySearch.TryValueIndexRange(
                                [.. frequencyCompensations.Select(tt => tt.X)],
                                flatnessSampleFrequencies[i],
                                out var startColumnIndex,
                                out var endColumnIndex))
                        {
                            compensation = Interpolator.Linear(frequencyCompensations[startColumnIndex], frequencyCompensations[endColumnIndex], flatnessSampleFrequencies[i]);
                        }
                    }

                    aodWaveSignals[flatnessSampleIndices[i]] = aodWaveSignalsSinc[i] * Math.Cos(2 * Math.PI * frequency * (flatnessSampleIndices[i] + 1) / sampleRate) * amplitude * (compensation ?? 1d);
                }

                #endregion Flatness

                /*
                 * 固定频率 cos(2*pi*f*x) * 递增增益(头部信号的增益, 在[0,1]之间严格递增序列) * 固定增益
                 * f = lowFrequency or highFrequency, T = 2*pi/(2*pi*f) = 1/f
                 * x = (headerSampleIndices[i] + 1) / sampleRate
                 * 单位: Mhz*sa/(Msa/s) = (10^6s^-1)*sa/(10^6sa/s) = (10^6s^-1)/(10^6s^-1)*sa/sa = 无量纲
                 */
                var headerAmplitudes = headerSampleIndices
                    .Select(temp => (double)(temp + 1) / headerSampleIndices.Length)
                    .ToArray();
                for (var i = 0; i < headerSampleIndices.Length; i++)
                    aodWaveSignals[headerSampleIndices[i]] = Math.Cos(2 * Math.PI * monotonicTypeEnum switch
                    {
                        MonotonicTypeEnum.Increasing => lowFrequency,
                        MonotonicTypeEnum.Deceasing => highFrequency,
                        MonotonicTypeEnum.Flatness => centerFrequency,
                        _ => ThrowHelper.ThrowArgumentOutOfRangeException<double>(nameof(monotonicTypeEnum))
                    } * (headerSampleIndices[i] + 1) / sampleRate) * headerAmplitudes[i] * amplitude; // 固定频率 cos(2*pi*T), 递增增益

                /*
                 * 固定频率 cos(2*pi*f*x) * 递减(尾部信号的增益, 在[0,1]之间严格递减小序列) * 固定增益
                 * f = lowFrequency or highFrequency, T = 2*pi/(2*pi*f) = 1/f
                 * x = (footerSampleIndices[i] + 1) / sampleRate
                 * 单位: Mhz*sa/(Msa/s) = (10^6s^-1)*sa/(10^6sa/s) = (10^6s^-1)/(10^6s^-1)*sa/sa = 无量纲
                 */
                var footerAmplitudes = headerSampleIndices
                    .Reverse()
                    .Select(temp => (double)(temp + 1) / headerSampleIndices.Length)
                    .ToArray();
                for (var i = 0; i < footerSampleIndices.Length; i++)
                    aodWaveSignals[footerSampleIndices[i]] = Math.Cos(2 * Math.PI * monotonicTypeEnum switch
                    {
                        MonotonicTypeEnum.Increasing => highFrequency,
                        MonotonicTypeEnum.Deceasing => lowFrequency,
                        MonotonicTypeEnum.Flatness => centerFrequency,
                        _ => ThrowHelper.ThrowArgumentOutOfRangeException<double>(nameof(monotonicTypeEnum))
                    } * (footerSampleIndices[i] + 1) / sampleRate) * footerAmplitudes[i] * amplitude; // 固定频率 cos(2*pi*T), 递减增益

                var freq = Generate.LinearSpaced(flatnessSampleIndices.Length, -sampleRate / 2d, sampleRate / 2d); // 起点: start 终点: stop 步进: step = (stop - start) / (length - 1), 包含起点终点
                var matlabFastFourierTransform = FourierTransform.MatlabFastFourierTransform(Vector<double>.Build.DenseOfEnumerable(flatnessSampleIndices.Select(temp => aodWaveSignals[temp]))).Map(c => c.Magnitude); // 平坦区域进行 Matlab FFT, 并得到 FFT的幅值
                /*
                 *  FFT with fftshift
                 *  |
                 *  |       *       *
                 *  |     *   *   *   *
                 *  |   *       *       *
                 *  -------------------------
                 *  -Fs/2        0        Fs/2
                 *
                 *  FFT without fftshift
                 *  |
                 *  |       *       *
                 *  |     *   *   *   *
                 *  |   *       *       *
                 *  -------------------------
                 *  0        Fs/2        Fs
                 */
                var fourierResult = FourierTransform.MatlabFastFourierTransformShift(matlabFastFourierTransform).ToArray(); // 将零频分量移动到数组中心，正负频率对称, 得到快速傅里叶变换结果
                aodWaveSignalsFourier = [.. freq.Select((t, i) => new Point(t, fourierResult[i]))];

                if (monotonicTypeEnum is MonotonicTypeEnum.Flatness)
                {
                    var max = aodWaveSignalsFourier.OrderByDescending(t => t.Y).Take(2).OrderByDescending(t => t.X).ToArray();

                    var frequency = Math.Abs(max[0].X);

                    if (Math.Abs(frequency - readonlyHighFrequency) < sampleRate / flatnessSampleIndices.Length)
                    {
                        isSuccess = true;
                        break;
                    }

                    if (frequency < readonlyCenterFrequency)
                    {
                        centerFrequency += sampleRate / (2d * totalSampleIndices.Length);
                    }
                    else
                    {
                        centerFrequency -= sampleRate / (2d * totalSampleIndices.Length);
                    }
                }
                else
                {
                    var lowFlatnessFrequencyIndex = (int)Math.Round(((lowFrequency + highFrequency) / 2d - (highFrequency - lowFrequency) / 4d)
                                                                    / (sampleRate / flatnessSampleIndices.Length)
                                                                    + flatnessSampleIndices.Length / 2d, MidpointRounding.AwayFromZero) - 1; // 平坦区域的低频
                    var highFlatnessFrequencyIndex = (int)Math.Round(((lowFrequency + highFrequency) / 2d + (highFrequency - lowFrequency) / 4d)
                                                                     / (sampleRate / flatnessSampleIndices.Length)
                                                                     + flatnessSampleIndices.Length / 2d, MidpointRounding.AwayFromZero) - 1; // 平坦区域的高频
#if NET
                    var mean = fourierResult[lowFlatnessFrequencyIndex..(highFlatnessFrequencyIndex + 1)].ToArray().Average();
#else
                    var mean = fourierResult.AsSpan()[lowFlatnessFrequencyIndex..(highFlatnessFrequencyIndex + 1)].ToArray().Average();
#endif
                    var x1 = Generate.LinearRangeInt32((int)Math.Round(flatnessSampleIndices.Length / 2d, MidpointRounding.AwayFromZero) - 1, 1, fourierResult.Length - 1)
                        .Where(temp => fourierResult[temp] - mean > 0)
                        .Select(temp => Math.Abs(fourierResult[temp] - mean) > Math.Abs(fourierResult[temp - 1] - mean)
                            ? temp - 1
                            : (int?)temp)
                        .FirstOrDefault();
                    if (x1 is null) ThrowHelper.ThrowInvalidOperationException("find flatness start(x1) failed");

                    var x2 = Generate.LinearRangeInt32(0, 1, fourierResult.Length - 1)
                        .Where(temp => fourierResult[fourierResult.Length - 1 - temp] - mean > 0)
                        .Select(temp => Math.Abs(fourierResult[fourierResult.Length - 1 - temp] - mean) > Math.Abs(fourierResult[fourierResult.Length - 1 - temp + 1] - mean)
                            ? fourierResult.Length - 1 - temp + 1
                            : (int?)(fourierResult.Length - 1 - temp))
                        .FirstOrDefault();
                    if (x2 is null) ThrowHelper.ThrowInvalidOperationException("find flatness end(x2) failed");

                    if (monotonicTypeEnum is MonotonicTypeEnum.Increasing)
                    {
                        if (Math.Abs(freq[x1.Value] - readonlyLowFrequency) < sampleRate / flatnessSampleIndices.Length)
                        {
                            if (Math.Abs(freq[x2.Value] - readonlyHighFrequency) < sampleRate / flatnessSampleIndices.Length)
                            {
                                isSuccess = true;
                                break;
                            }

                            if (freq[x1.Value] < readonlyHighFrequency)
                                bandwidthTemp -= sampleRate / (2d * totalSampleIndices.Length);
                            else
                                bandwidthTemp += sampleRate / (2d * totalSampleIndices.Length);
                        }
                        else if (freq[x2.Value] < readonlyLowFrequency)
                            lowFrequency += sampleRate / (2d * totalSampleIndices.Length);
                        else
                            lowFrequency -= sampleRate / (2d * totalSampleIndices.Length);
                    }
                    else
                    {
                        if (Math.Abs(freq[x2.Value] - readonlyHighFrequency) < sampleRate / flatnessSampleIndices.Length)
                        {
                            if (Math.Abs(freq[x1.Value] - readonlyLowFrequency) < sampleRate / flatnessSampleIndices.Length)
                            {
                                isSuccess = true;
                                break;
                            }

                            if (freq[x1.Value] < readonlyLowFrequency)
                                bandwidthTemp -= sampleRate / (2d * totalSampleIndices.Length);
                            else
                                bandwidthTemp += sampleRate / (2d * totalSampleIndices.Length);
                        }
                        else if (freq[x2.Value] < readonlyHighFrequency)
                            highFrequency += sampleRate / (2d * totalSampleIndices.Length);
                        else
                            highFrequency -= sampleRate / (2d * totalSampleIndices.Length);
                    }
                }

                if (++count > generateRetryTimes) ThrowHelper.ThrowInvalidOperationException("AOD waveforms could not be generated");
            }
            catch (Exception ex)
            {
                exception = ex;
                isSuccess = false;
                break;
            }
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
         *          一、公式: =(HEX2DEC(A1)-IF(HEX2DEC(A1)>=32768,65536,0))/POWER(2,16)
         *          二、不要[下拉填充点]直接推拽下拉太慢, 快速公式下拉填充: 直接双击[下拉填充点]一行直接生成
         */

        // 将结果转换为16位整数并保存到文件
        var aodWaveSignalResult = aodWaveSignals
            .Select(t => ConvertHelper.ToInt16NotOverflowException(Math.Round(Math.Pow(2, 15) * t, MidpointRounding.AwayFromZero)))
            .Select(Convert.ToInt64)
            .Select(t => t < 0 ? t + (long)Math.Pow(2, 32) : t)
            .ToArray();
        var hexStrings = aodWaveSignalResult
            .Select(t => t.ToString("X4")) // 转换为16进制补码字符串, 至少4位不足右边补0
            .Select(t => t[^4..]) // 字符串的最后 4 位
            .ToArray();

        DirectoryHelper.CreateFileDirectoryIfNotExists(aodWaveFilePath);
        FileHelper.DeleteFileIfExists(aodWaveFilePath);
        File.WriteAllText(aodWaveFilePath, string.Join(Environment.NewLine, hexStrings));

        return (
            isSuccess,
            aodWaveFilePath,
            flatnessSampleIndices.Select((t, index) => new Point(t + 1, aodWaveFlatnessLinearFrequencySignals[index])).ToArray(),
            flatnessSampleIndices.Select((t, index) => new Point(t + 1, aodWaveFlatnessNonLinearFrequencySignals[index])).ToArray(),
            flatnessSampleIndices.Select((t, index) => new Point(t + 1, aodWaveFlatnessTotalFrequencySignals[index])).ToArray(),
            flatnessSampleIndices.Select((t, index) => new Point(t + 1, aodWaveFlatnessLinearCompensationSignals[index])).ToArray(),
            flatnessSampleIndices.Select((t, index) => new Point(t + 1, aodWaveFlatnessAstigmatismCompensationSignals[index])).ToArray(),
            flatnessSampleIndices.Select((t, index) => new Point(t + 1, aodWaveFlatnessSphericalAberrationCompensationSignals[index])).ToArray(),
            flatnessSampleIndices.Select((t, index) => new Point(t + 1, aodWaveFlatnessSecondaryAstigmatismCompensationSignals[index])).ToArray(),
            flatnessSampleIndices.Select((t, index) => new Point(t + 1, aodWaveFlatnessComaCompensationSignals[index])).ToArray(),
            flatnessSampleIndices.Select((t, index) => new Point(t + 1, aodWaveFlatnessTrefoilCompensationSignals[index])).ToArray(),
            flatnessSampleIndices.Select((t, index) => new Point(t + 1, aodWaveFlatnessQuadrafoilCompensationSignals[index])).ToArray(),
            flatnessSampleIndices.Select((t, index) => new Point(t + 1, aodWaveFlatnessAlphaOrderCompensationSignals[index])).ToArray(),
            flatnessSampleIndices.Select((t, index) => new Point(t + 1, aodWaveFlatnessNonlinearCompensationSignals[index])).ToArray(),
            flatnessSampleIndices.Select((t, index) => new Point(t + 1, aodWaveFlatnessTotalCompensationSignals[index])).ToArray(),
            totalSampleIndices.Select(t => new Point(t + 1, aodWaveSignals[t])).ToArray(),
            aodWaveSignalsFourier.Where(t => t.X >= 0).ToArray(),
            flatnessSampleIndices.Select((t, index) => new Point(t + 1, aodWaveSignalsSinc[index])).ToArray(),
            exception);
    }
}