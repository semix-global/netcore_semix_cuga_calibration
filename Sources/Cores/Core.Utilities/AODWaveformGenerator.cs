using CommunityToolkit.Diagnostics;
using MathNet.Numerics.Interpolation;
using MathNet.Numerics.LinearAlgebra;
using Net.Utilities.Algorithms.Extensions;
using Net.Utilities.Algorithms.Modules;
using Net.Utilities.Helpers.Helpers;
using Net.Utilities.Helpers.Helpers.Files;
using Net.Utilities.Models;
using Net.Utilities.Models.Enums.Maths;
using Net.Utilities.Models.Geometries;
using Complex = System.Numerics.Complex;

namespace Core.Utilities;

public static class AODWaveformGenerator1
{
    /// <inheritdoc cref="GenerateAODWaveform{TResult,TParam}"/>
    /// <remarks>
    /// 生成PrescanAOD波形
    /// </remarks>
    public static (AODWaveformGenerator.PrescanAODWaveformResult AODWaveformResult, Exception? Exception) GeneratePrescanAODWaveform(AODWaveformGenerator.PrescanAODWaveformParam param, CancellationToken cancellationToken) =>
        GenerateAODWaveform<AODWaveformGenerator.PrescanAODWaveformResult, AODWaveformGenerator.PrescanAODWaveformParam>(param, cancellationToken);

    /// <inheritdoc cref="GenerateAODWaveform{TResult,TParam}"/>
    /// <remarks>
    /// 生成ChirpAOD波形
    /// </remarks>
    public static (AODWaveformGenerator.ChirpAODWaveformResult AODWaveformResult, Exception? Exception) GenerateChirpAODWaveform(AODWaveformGenerator.ChirpAODWaveformParam param, CancellationToken cancellationToken) =>
        GenerateAODWaveform<AODWaveformGenerator.ChirpAODWaveformResult, AODWaveformGenerator.ChirpAODWaveformParam>(param, cancellationToken);

    /// <summary>
    /// 生成AOD波形
    /// </summary>
    /// <param name="param">AOD波形生成参数</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>(结果, 异常信息)</returns>
    private static (TResult AODWaveformResult, Exception? Exception) GenerateAODWaveform<TResult, TParam>(TParam param, CancellationToken cancellationToken)
        where TResult : AODWaveformGenerator.AbstractAODWaveformResult<TParam>
        where TParam : AODWaveformGenerator.AbstractAODWaveformParam
    {
        try
        {
            ObjectHelper.InvokeMethod(param, "Validate");

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
            Vector<double> astigmatismFrequencies;
            Vector<double> sphericalAberrationFrequencies;
            Vector<double> secondaryAstigmatismFrequencies;
            Vector<double> comaFrequencies;
            Vector<double> trefoilFrequencies;
            Vector<double> quadrafoilFrequencies;
            Vector<double> alphaOrderFrequencies;

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
                    astigmatismFrequencies = param.AstigmatismCompensationCoefficient * t.PointwisePower(2d);
                    sphericalAberrationFrequencies = param.SphericalAberrationCompensationCoefficient * t.PointwisePower(3d);
                    secondaryAstigmatismFrequencies = param.SecondaryAstigmatismCompensationCoefficient * t.PointwisePower(4d);
                    comaFrequencies = Vector<double>.Build.Dense(t.Count, 0d);
                    trefoilFrequencies = Vector<double>.Build.Dense(t.Count, 0d);
                    quadrafoilFrequencies = Vector<double>.Build.Dense(t.Count, 0d);
                    alphaOrderFrequencies = Vector<double>.Build.Dense(t.Count, 0d);

                    break;

                case FunctionMonotonicTypeEnum.Flatness:
                default:
                    linearFrequencies = Vector<double>.Build.Dense(t.Count, 0d);
                    astigmatismFrequencies = Vector<double>.Build.Dense(t.Count, 0d);
                    sphericalAberrationFrequencies = Vector<double>.Build.Dense(t.Count, 0d);
                    secondaryAstigmatismFrequencies = Vector<double>.Build.Dense(t.Count, 0d);
                    comaFrequencies = Vector<double>.Build.Dense(t.Count, 0d);
                    trefoilFrequencies = Vector<double>.Build.Dense(t.Count, 0d);
                    quadrafoilFrequencies = Vector<double>.Build.Dense(t.Count, 0d);
                    alphaOrderFrequencies = Vector<double>.Build.Dense(t.Count, 0d);

                    break;
            }


            var flatnessFrequencies = centerFrequency + linearFrequencies + astigmatismFrequencies + sphericalAberrationFrequencies + secondaryAstigmatismFrequencies + comaFrequencies + trefoilFrequencies + quadrafoilFrequencies + alphaOrderFrequencies;

            if (flatnessFrequencies.Exists(f => f <= 0 || f > param.HighFrequency * 1.5d)) ThrowHelper.ThrowArgumentException(nameof(flatnessFrequencies), $"Synthesized frequencies must be in the range (0, {param.HighFrequency * 1.5d:f3} MHz)");

            #endregion 频率

            var result = GuardUtils.IsNotNullAndAssignableToType<TResult>(Activator.CreateInstance(typeof(TResult), param, true));
            ObjectHelper.InvokeMethod(result, "Initialize");

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

                ObjectHelper.SetPropertyValue(item, nameof(item.Signals), item.OffsetConfiguration.IsGenerateAODWaveformZero
                    ? (Point[])[.. allSampleIndices.Index().Select(tuple => new Point(tuple.Item, 0d))]
                    : [.. allSampleIndices.Index().Select(tuple => new Point(tuple.Item, aodWaveformSignals[tuple.Index]))]);
                ObjectHelper.SetPropertyValue(item, nameof(item.FFTSignals), item.OffsetConfiguration.IsGenerateAODWaveformZero
                    ? (Point[])[.. fftFrequencies.Zip(fftMagnitudes, (x, _) => new Point(x, 0))]
                    : [.. fftFrequencies.Zip(fftMagnitudes, (x, y) => new Point(x, y))]);
                ObjectHelper.SetPropertyValue(item, nameof(item.FrequencyCoefficients), item.OffsetConfiguration.IsGenerateAODWaveformZero
                    ? (Point[])[.. frequencyCoefficientList.Select(point => new Point(point.X, 0d))]
                    : [.. frequencyCoefficientList]);
                ObjectHelper.SetPropertyValue(item, nameof(item.FlatnessLinearFrequencySignals), item.OffsetConfiguration.IsGenerateAODWaveformZero
                    ? (Point[])[.. flatnessSampleIndices.Index().Select(tuple => new Point(tuple.Item, 0d))]
                    : [.. flatnessSampleIndices.Index().Select(tuple => new Point(tuple.Item, linearFrequencies[tuple.Index]))]);
                ObjectHelper.SetPropertyValue(item, nameof(item.FlatnessTotalFrequencySignals), item.OffsetConfiguration.IsGenerateAODWaveformZero
                    ? (Point[])[.. flatnessSampleIndices.Index().Select(tuple => new Point(tuple.Item, 0d))]
                    : [.. flatnessSampleIndices.Index().Select(tuple => new Point(tuple.Item, flatnessFrequencies[tuple.Index]))]);
                ObjectHelper.SetPropertyValue(item, nameof(item.FlatnessAstigmatismCompensationSignals), item.OffsetConfiguration.IsGenerateAODWaveformZero
                    ? (Point[])[.. flatnessSampleIndices.Index().Select(tuple => new Point(tuple.Item, 0d))]
                    : [.. flatnessSampleIndices.Index().Select(tuple => new Point(tuple.Item, astigmatismFrequencies[tuple.Index]))]);
                ObjectHelper.SetPropertyValue(item, nameof(item.FlatnessSphericalAberrationCompensationSignals), item.OffsetConfiguration.IsGenerateAODWaveformZero
                    ? (Point[])[.. flatnessSampleIndices.Index().Select(tuple => new Point(tuple.Item, 0d))]
                    : [.. flatnessSampleIndices.Index().Select(tuple => new Point(tuple.Item, sphericalAberrationFrequencies[tuple.Index]))]);
                ObjectHelper.SetPropertyValue(item, nameof(item.FlatnessSecondaryAstigmatismCompensationSignals), item.OffsetConfiguration.IsGenerateAODWaveformZero
                    ? (Point[])[.. flatnessSampleIndices.Index().Select(tuple => new Point(tuple.Item, 0d))]
                    : [.. flatnessSampleIndices.Index().Select(tuple => new Point(tuple.Item, secondaryAstigmatismFrequencies[tuple.Index]))]);
                ObjectHelper.SetPropertyValue(item, nameof(item.FlatnessComaCompensationSignals), item.OffsetConfiguration.IsGenerateAODWaveformZero
                    ? (Point[])[.. flatnessSampleIndices.Index().Select(tuple => new Point(tuple.Item, 0d))]
                    : [.. flatnessSampleIndices.Index().Select(tuple => new Point(tuple.Item, comaFrequencies[tuple.Index]))]);
                ObjectHelper.SetPropertyValue(item, nameof(item.FlatnessTrefoilCompensationSignals), item.OffsetConfiguration.IsGenerateAODWaveformZero
                    ? (Point[])[.. flatnessSampleIndices.Index().Select(tuple => new Point(tuple.Item, 0d))]
                    : [.. flatnessSampleIndices.Index().Select(tuple => new Point(tuple.Item, trefoilFrequencies[tuple.Index]))]);
                ObjectHelper.SetPropertyValue(item, nameof(item.FlatnessQuadrafoilCompensationSignals), item.OffsetConfiguration.IsGenerateAODWaveformZero
                    ? (Point[])[.. flatnessSampleIndices.Index().Select(tuple => new Point(tuple.Item, 0d))]
                    : [.. flatnessSampleIndices.Index().Select(tuple => new Point(tuple.Item, quadrafoilFrequencies[tuple.Index]))]);
                ObjectHelper.SetPropertyValue(item, nameof(item.FlatnessAlphaOrderCompensationSignals), item.OffsetConfiguration.IsGenerateAODWaveformZero
                    ? (Point[])[.. flatnessSampleIndices.Index().Select(tuple => new Point(tuple.Item, 0d))]
                    : [.. flatnessSampleIndices.Index().Select(tuple => new Point(tuple.Item, alphaOrderFrequencies[tuple.Index]))]);
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
                var flatnessPhases = 2d * Math.PI * (centerFrequency * (t - δt)
                                                     + 1d / 2d * k * (t - δt).PointwisePower(2)
                                                     + param.AstigmatismCompensationCoefficient * 1d / 3d * (t - δt).PointwisePower(3)
                                                     + param.SphericalAberrationCompensationCoefficient * 1d / 4d * (t - δt).PointwisePower(4)
                                                     + param.SecondaryAstigmatismCompensationCoefficient * 1d / 5d * (t - δt).PointwisePower(5)
                                                     + Vector<double>.Build.Dense(t.Count, 0d)
                                                     + Vector<double>.Build.Dense(t.Count, 0d)
                                                     + Vector<double>.Build.Dense(t.Count, 0d)
                                                     + Vector<double>.Build.Dense(t.Count, 0d));
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